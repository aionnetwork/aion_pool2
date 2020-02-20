/*
Copyright 2017 Coin Foundry (coinfoundry.org)
Authors: Oliver Weichhold (oliver@weichhold.com)

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
associated documentation files (the "Software"), to deal in the Software without restriction,
including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial
portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT
LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Autofac;
using AutoMapper;
using MiningCore.Blockchain.Aion.DaemonRequests;
using MiningCore.Blockchain.Aion.DaemonResponses;
using MiningCore.Blockchain.Aion.Configuration;
using MiningCore.Configuration;
using MiningCore.DaemonInterface;
using MiningCore.Extensions;
using MiningCore.Notifications;
using MiningCore.Payments;
using MiningCore.Persistence;
using MiningCore.Persistence.Model;
using MiningCore.Persistence.Repositories;
using MiningCore.Time;
using MiningCore.Util;
using Newtonsoft.Json;
using Block = MiningCore.Persistence.Model.Block;
using Contract = MiningCore.Contracts.Contract;
using AionCommands = MiningCore.Blockchain.Aion.AionCommands;

namespace MiningCore.Blockchain.Aion
{
    [CoinMetadata(CoinType.AION)]
    public class AionPayoutHandler : PayoutHandlerBase,
        IPayoutHandler
    {
        public AionPayoutHandler(
            IComponentContext ctx,
            IConnectionFactory cf,
            IMapper mapper,
            IShareRepository shareRepo,
            IBlockRepository blockRepo,
            IBalanceRepository balanceRepo,
            IPaymentRepository paymentRepo,
            IMasterClock clock,
            NotificationService notificationService) :
            base(cf, mapper, shareRepo, blockRepo, balanceRepo, paymentRepo, clock, notificationService)
        {
            Contract.RequiresNonNull(ctx, nameof(ctx));
            Contract.RequiresNonNull(balanceRepo, nameof(balanceRepo));
            Contract.RequiresNonNull(paymentRepo, nameof(paymentRepo));

            this.ctx = ctx;
        }

        private readonly IComponentContext ctx;
        private DaemonClient daemon;

        private AionPoolPaymentExtraConfig extraConfig;

        protected override string LogCategory => "Aion Payout Handler";

        #region IPayoutHandler

        public async Task ConfigureAsync(ClusterConfig clusterConfig, PoolConfig poolConfig)
        {
            this.poolConfig = poolConfig;
            this.clusterConfig = clusterConfig;
            extraConfig = poolConfig.PaymentProcessing.Extra.SafeExtensionDataAs<AionPoolPaymentExtraConfig>();

            logger = LogUtil.GetPoolScopedLogger(typeof(AionPayoutHandler), poolConfig);

            // configure standard daemon
            var jsonSerializerSettings = ctx.Resolve<JsonSerializerSettings>();

            var daemonEndpoints = poolConfig.Daemons
                .Where(x => string.IsNullOrEmpty(x.Category))
                .ToArray();

            daemon = new DaemonClient(jsonSerializerSettings);
            daemon.Configure(daemonEndpoints);
        }

        public async Task<Block[]> ClassifyBlocksAsync(Block[] blocks)
        {
            Contract.RequiresNonNull(poolConfig, nameof(poolConfig));
            Contract.RequiresNonNull(blocks, nameof(blocks));

            var pageSize = 100;
            var pageCount = (int)Math.Ceiling(blocks.Length / (double)pageSize);
            var blockCache = new Dictionary<long, DaemonResponses.Block>();
            var result = new List<Block>();

            for (var i = 0; i < pageCount; i++)
            {
                // get a page full of blocks
                var page = blocks
                    .Skip(i * pageSize)
                    .Take(pageSize)
                    .ToArray();

                // get latest block
                var latestBlockResponses = await daemon.ExecuteCmdAllAsync<DaemonResponses.Block>(AionCommands.GetBlockByNumber, new[] { (object)"latest", true });
                if (!latestBlockResponses.Any(x => x.Error == null && x.Response?.Height != null))
                    break;
                var latestBlockHeight = latestBlockResponses.First(x => x.Error == null && x.Response?.Height != null).Response.Height.Value;

                // execute batch
                var blockInfos = await FetchBlocks(blockCache, page.Select(block => (long)block.BlockHeight).ToArray());

                for (var j = 0; j < blockInfos.Length; j++)
                {
                    var blockInfo = blockInfos[j];
                    var block = page[j];

                    // extract confirmation data from stored block
                    var mixHash = block.TransactionConfirmationData.Split(":").First();
                    var nonce = block.TransactionConfirmationData.Split(":").Last();

                    // update progress
                    block.ConfirmationProgress = Math.Min(1.0d, (double)(latestBlockHeight - block.BlockHeight) / extraConfig.MinimumConfirmations);
                    result.Add(block);

                    // is it block mined by us?
                    if (blockInfo.Miner == poolConfig.Address)
                    {
                        // mature?
                        if (latestBlockHeight - block.BlockHeight >= (ulong) extraConfig.MinimumConfirmations)
                        {
                            block.Status = BlockStatus.Confirmed;
                            block.ConfirmationProgress = 1;
                            block.Reward = AionUtils.calculateReward((long) block.BlockHeight);

                            if (extraConfig?.KeepTransactionFees == false && blockInfo.Transactions?.Length > 0)
                                block.Reward += await GetTxRewardAsync(blockInfo); // tx fees

                            logger.Info(() => $"[{LogCategory}] Unlocked block {block.BlockHeight} worth {FormatAmount(block.Reward)}");
                        }

                        continue;
                    }

                    if (block.Status == BlockStatus.Pending && block.ConfirmationProgress > 0.75)
                    {
                        // we've lost this one
                        block.Status = BlockStatus.Orphaned;
                        block.Reward = 0;
                    }
                }
            }

            return result.ToArray();
        }

        public Task CalculateBlockEffortAsync(Block block, double accumulatedBlockShareDiff)
        {
            block.Effort = accumulatedBlockShareDiff / block.NetworkDifficulty;

            return Task.FromResult(true);
        }

    public Task<decimal> UpdateBlockRewardBalancesAsync(IDbConnection con, IDbTransaction tx, Block block, PoolConfig pool)
        {
            var blockRewardRemaining = block.Reward;

            // Distribute funds to configured reward recipients
            foreach (var recipient in poolConfig.RewardRecipients.Where(x => x.Percentage > 0))
            {
                var amount = block.Reward * (recipient.Percentage / 100.0m);
                var address = recipient.Address;

                blockRewardRemaining -= amount;

                // skip transfers from pool wallet to pool wallet
                if (address != poolConfig.Address)
                {
                    logger.Info(() => $"Adding {FormatAmount(amount)} to balance of {address}");
                    balanceRepo.AddAmount(con, tx, poolConfig.Id, poolConfig.Coin.Type, address, amount, $"Reward for block {block.BlockHeight}");
                }
            }

            // Deduct static reserve for tx fees
            blockRewardRemaining -= (decimal) extraConfig.NrgFee;

            return Task.FromResult(blockRewardRemaining);
        }

        public async Task PayoutAsync(Balance[] balances)
        {
            // ensure we have peers
//             var infoResponse = await daemon.ExecuteCmdSingleAsync<object>(AionCommands.GetPeerCount);

//             //TODO @AP-137 fix the validation
// #if !DEBUG
//             if (infoResponse.Error != null || 
//                 (Convert.ToInt32(infoResponse.Response)) < extraConfig.MinimumPeerCount)
//             {
//                 logger.Warn(() => $"[{LogCategory}] Payout aborted. Not enough peers (" +
//                 extraConfig.MinimumPeerCount + " required)");
//                 return;
//             }
// #endif

//             var txHashes = new List<string>();

//             foreach (var balance in balances)
//             {
//                 try
//                 {
                    for (int a = 0; a < 100; a = a + 1) {
                        var txHash = await Payout(/*null*/);
                        logger.Info($"{a}:{txHash}");
                    }
            //         txHashes.Add(txHash);
            //     }

            //     catch (Exception ex)
            //     {
            //         logger.Error(ex);

            //         NotifyPayoutFailure(poolConfig.Id, new[] { balance }, ex.Message, null);
            //     }
            // }

            // if (txHashes.Any())
            //     NotifyPayoutSuccess(poolConfig.Id, balances, txHashes.ToArray(), null);
        }

        #endregion // IPayoutHandler

        private async Task<DaemonResponses.Block[]> FetchBlocks(Dictionary<long, DaemonResponses.Block> blockCache, params long[] blockHeights)
        {
            var cacheMisses = blockHeights.Where(x => !blockCache.ContainsKey(x)).ToArray();

            if (cacheMisses.Any())
            {
                var blockBatch = cacheMisses.Select(height => new DaemonCmd(AionCommands.GetBlockByNumber,
                    new[]
                    {
                        (object) height.ToStringHexWithPrefix(),
                        true
                    })).ToArray();

                var tmp = await daemon.ExecuteBatchAnyAsync(blockBatch);

                var transformed = tmp
                    .Where(x => x.Error == null && x.Response != null)
                    .Select(x => x.Response?.ToObject<DaemonResponses.Block>())
                    .Where(x => x != null)
                    .ToArray();

                foreach (var block in transformed)
                    blockCache[(long)block.Height.Value] = block;
            }

            return blockHeights.Select(x => blockCache[x]).ToArray();
        }

        private async Task<decimal> GetTxRewardAsync(DaemonResponses.Block blockInfo)
        {
            // fetch all tx receipts in a single RPC batch request
            var batch = blockInfo.Transactions.Select(tx => new DaemonCmd(AionCommands.GetTxReceipt, new[] { tx.Hash }))
                .ToArray();

            var results = await daemon.ExecuteBatchAnyAsync(batch);

            if (results.Any(x => x.Error != null))
                throw new Exception($"Error fetching tx receipts: {string.Join(", ", results.Where(x => x.Error != null).Select(y => y.Error.Message))}");

            // create lookup table
            var gasUsed = results.Select(x => x.Response.ToObject<TransactionReceipt>())
                .ToDictionary(x => x.TransactionHash, x => x.GasUsed);

            // accumulate
            var result = blockInfo.Transactions.Sum(x => (ulong)gasUsed[x.Hash] * ((decimal)x.GasPrice / AionConstants.Wei));

            return result;
        }

        private async Task<string> Payout(/*Balance balance*/)
        {

            // if (!String.IsNullOrEmpty("PLAT4life"/*extraConfig.AccountPassword*/))
            // {
                var unlockResponse = await daemon.ExecuteCmdSingleAsync<object>(AionCommands.UnlockAccount, new[]
                {
                    // poolConfig.Address,
                    "0xa02e4f1e4c192a0a992eb5d4d30e6359b800a3759e1d749c6c0b591ab8e47fc9",
                    // extraConfig.AccountPassword,
                    "PLAT4life",
                    null
                });

                if (unlockResponse.Error != null || unlockResponse.Response == null || (bool)unlockResponse.Response == false)
                    throw new Exception("Unable to unlock coinbase account for sending transaction");
            // } else 
            // {
            //     logger.Error(() => $"[{LogCategory}] The password is missing from the configuration, unable to send payments.");
            //     throw new Exception("Missing password");
            // }

            // send transaction
            // logger.Info(() => $"[{LogCategory}] Sending {FormatAmount(balance.Amount)} to {balance.Address}");
            logger.Info(() => $"send rewards");

            decimal amount = 1;
            var request = new SendTransactionRequest
            {
                // From = poolConfig.Address,
                From = "0xa02e4f1e4c192a0a992eb5d4d30e6359b800a3759e1d749c6c0b591ab8e47fc9",
                // To = balance.Address,
                To = "0xa02e4f1e4c192a0a992eb5d4d30e6359b800a3759e1d749c6c0b591ab8e47fc9",
                // Value = (BigInteger)Math.Floor(balance.Amount * AionConstants.Wei),
                Value = (BigInteger)Math.Floor(amount * AionConstants.Wei),
            };

            var response = await daemon.ExecuteCmdSingleAsync<string>(AionCommands.SendTx, new[] { request });

            if (response.Error != null)
                throw new Exception($"{AionCommands.SendTx} returned error: {response.Error.Message} code {response.Error.Code}");

            if (string.IsNullOrEmpty(response.Response) || AionConstants.ZeroHashPattern.IsMatch(response.Response))
                throw new Exception($"{AionCommands.SendTx} did not return a valid transaction hash");

            var txHash = response.Response;
            // logger.Info(() => $"[{LogCategory}] Payout transaction id: {txHash}");

            // // update db
            // PersistPayments(new[] { balance }, txHash);

            // done
            return txHash;
        }
    }
}
