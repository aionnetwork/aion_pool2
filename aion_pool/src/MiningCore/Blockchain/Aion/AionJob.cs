using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using System.Collections.Concurrent;
using MiningCore.Blockchain.Bitcoin;
using MiningCore.Blockchain.ZCash.DaemonResponses;
using MiningCore.Blockchain.Bitcoin.DaemonResponses;
using MiningCore.Configuration;
using MiningCore.Contracts;
using MiningCore.Crypto;
using MiningCore.Crypto.Hashing.Algorithms;
using MiningCore.Crypto.Hashing.Equihash;
using MiningCore.Extensions;
using MiningCore.Stratum;
using MiningCore.Time;
using MiningCore.Util;
using MiningCore.DaemonInterface;
using NBitcoin;
using NBitcoin.DataEncoders;
using NLog;

namespace MiningCore.Blockchain.Aion
{
    public class AionJob
    {
        public AionJob(string id, AionBlockTemplate blockTemplate, ILogger logger, DaemonClient daemon)
        {
            Id = id;
            BlockTemplate = blockTemplate;
            this.logger = logger;

            var target = blockTemplate.Target;
            if (target.StartsWith("0x"))
                target = target.Substring(2);

            this.daemonClient = daemon;
            Difficulty = getNetworkDifficulty();
            blockTarget = new uint256(target.HexToByteArray().ReverseArray());
        }

        private readonly Dictionary<StratumClient, ConcurrentDictionary<string, byte>> workerNonces =
            new Dictionary<StratumClient, ConcurrentDictionary<string, byte>>();

        public string Id { get; }
        public AionBlockTemplate BlockTemplate { get; }
        private readonly uint256 blockTarget;
        private readonly ILogger logger;
        private EquihashSolver equihash = EquihashSolver.Instance.Value;
        private IHashAlgorithm headerHasher = new Blake2b();
        public double Difficulty { get; protected set; }
        public DaemonClient daemonClient;

        private void RegisterNonce(StratumClient worker, string nonce)
        {
            var nonceLower = nonce.ToLower();

            if (!workerNonces.TryGetValue(worker, out var nonces))
            {
                nonces = new ConcurrentDictionary<string, byte>();
                nonces.TryAdd(nonceLower, 1);
                workerNonces[worker] = nonces;
            }

            else
            {
                if (nonces.TryGet(nonceLower) == 1)
                    throw new StratumException(StratumError.MinusOne, "duplicate share");

                nonces.TryAdd(nonceLower, 1);
            }
        }

        public async Task<(Share Share, string nonce, string solution, string headerHash, string ntime)> ProcessShare(
            StratumClient worker, string extraNonce2, string nTime, string solution)
        {
            Contract.RequiresNonNull(worker, nameof(worker));
            Contract.Requires<ArgumentException>(!string.IsNullOrEmpty(extraNonce2), $"{nameof(extraNonce2)} must not be empty");
            Contract.Requires<ArgumentException>(!string.IsNullOrEmpty(nTime), $"{nameof(nTime)} must not be empty");
            Contract.Requires<ArgumentException>(!string.IsNullOrEmpty(solution), $"{nameof(solution)} must not be empty");

            var context = worker.GetContextAs<AionWorkerContext>();

            // validate nTime
            if (nTime.Length != 16)
                throw new StratumException(StratumError.Other, "incorrect size of ntime");             

            // var nTimeInt = decimal.Parse(nTime.HexToByteArray().ReverseArray().ToHexString(), NumberStyles.HexNumber);

            var nonce = context.ExtraNonce1 + extraNonce2;

            // validate nonce
            if (nonce.Length != 64)
                throw new StratumException(StratumError.Other, "incorrect size of extraNonce2");

            // validate solution (2822 solution length (1408 * 2 for hex) + 3 byte buffer) OR (2816 without buffer)
            //if (solution.Length != 2816 || solution.Length != 2822)
            //    throw new StratumException(StratumError.Other, "incorrect size of solution, length: " + solution.Length);
            
            if (solution.Length == 2822) 
                solution = solution.Substring(6); // Remove 3 byte buffer
            else if(solution.Length != 2816)
                throw new StratumException(StratumError.Other, "incorrect size of solution");

            // duplicate check
            RegisterNonce(worker, nonce);

            return await ProcessShareInternal(worker, nonce, nTime, solution);
        }

         private async Task<(Share Share, string nonce, string solution, string headerHash, string nTime)> ProcessShareInternal(
             StratumClient worker, string nonce, string nTime, string solution)
        {
            var context = worker.GetContextAs<AionWorkerContext>();
            var solutionBytes = solution.HexToByteArray();
            
            // serialize block-header
            var headerBytes = SerializeHeader(nonce);

            // verify solution
            if (!equihash.Verify210(headerBytes, solutionBytes))
                throw new StratumException(StratumError.Other, "invalid solution");

            // hash block-header
            var headerSolutionBytes = headerBytes.Concat(solutionBytes).ToArray();
            var headerHash = headerHasher.Digest(headerSolutionBytes);
            var headerHashReversed = headerHash.ToReverseArray();
            var headerBigInt = headerHashReversed.ToBigInteger();
            var target = new BigInteger(blockTarget.ToBytes());
            
            var isBlockCandidate = target > headerBigInt;

            // calc share-diff
            var stratumDifficulty = context.Difficulty > Difficulty ? Difficulty : context.Difficulty; 
            var shareDiff = stratumDifficulty;           
            var ratio = shareDiff / stratumDifficulty;

            var sentTargetInt = new uint256(AionUtils.diffToTarget(context.Difficulty).HexToByteArray().ReverseArray());
            var sentTarget = new BigInteger(sentTargetInt.ToBytes());
            var isLowDiffShare = sentTarget <= headerBigInt;

            if (isLowDiffShare) 
            {
                // Check if matched a previous varDiff before retarget
                if (context.VarDiff?.LastUpdate != null && context.PreviousDifficulty.HasValue)
                {
                    var prevSentTargetInt = new uint256(AionUtils.diffToTarget(context.PreviousDifficulty.Value).HexToByteArray().ReverseArray());
                    var prevSentTargetBi = new BigInteger(prevSentTargetInt.ToBytes());
                    if (prevSentTargetBi <= headerBigInt)
                    {
                        stratumDifficulty = context.PreviousDifficulty.Value;
                    }
                }
                else
                {
                    throw new StratumException(StratumError.LowDifficultyShare, $"low difficulty share ({shareDiff})");   
                }                
            }

            var result = new Share
            {
                BlockHeight = (long) BlockTemplate.Height,
                IpAddress = worker.RemoteEndpoint?.Address?.ToString(),
                Miner = context.MinerName,
                Worker = context.WorkerName,
                UserAgent = context.UserAgent,
                NetworkDifficulty = Difficulty,
                Difficulty = stratumDifficulty,
                IsBlockCandidate = isBlockCandidate,
                TransactionConfirmationData = headerHash.ToHexString(),
            };

            if (isBlockCandidate)
            {
                result.BlockReward = AionUtils.calculateReward((long) BlockTemplate.Height);
                result.BlockHash = headerHashReversed.ToHexString();
            }

            return (result, nonce, solution, BlockTemplate.HeaderHash, nTime);
        }

        private byte[] SerializeHeader(string nonce)
        {
            var blockHeader = BlockTemplate.HeaderHash + nonce;

            return blockHeader.HexToByteArray();
        }

        private double getNetworkDifficulty() {
            var response = daemonClient.ExecuteCmdAnyAsync<string>(AionCommands.GetDifficulty).Result;
            return (double) Convert.ToInt32(response.Response, 16);
        }
    }
}