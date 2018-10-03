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
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using System.Numerics;
using Autofac;
using AutoMapper;
using MiningCore.Blockchain.Aion;
using MiningCore.Blockchain.Ethereum.Configuration;
using MiningCore.Configuration;
using MiningCore.Extensions;
using MiningCore.JsonRpc;
using MiningCore.Messaging;
using MiningCore.Mining;
using MiningCore.Notifications;
using MiningCore.Persistence;
using MiningCore.Persistence.Repositories;
using MiningCore.Stratum;
using MiningCore.Time;
using MiningCore.Util;
using Newtonsoft.Json;
using NBitcoin;
using MiningCore.Contracts;
using System.Collections;

namespace MiningCore.Blockchain.Aion
{
    [CoinMetadata(CoinType.AION)]
    public class AionPool : PoolBase
    {
        public AionPool(IComponentContext ctx,
            JsonSerializerSettings serializerSettings,
            IConnectionFactory cf,
            IStatsRepository statsRepo,
            IMapper mapper,
            IMasterClock clock,
            IMessageBus messageBus,
            NotificationService notificationService) :
            base(ctx, serializerSettings, cf, statsRepo, mapper, clock, messageBus, notificationService)
        {
        }

        private object currentJobParams;
        private AionJobManager manager;

        private void OnSubscribe(StratumClient client, Timestamped<JsonRpcRequest> tsRequest)
        {
            var request = tsRequest.Value;
            var context = client.GetContextAs<AionWorkerContext>();

            if (request.Id == null)
            {
                client.RespondError(StratumError.Other, "missing request id", request.Id);
                return;
            }

            var requestParams = request.ParamsAs<string[]>();

            if (requestParams == null || requestParams.Length < 2)
            {
                client.RespondError(StratumError.MinusOne, "invalid request", request.Id);
                return;
            }

            manager.PrepareWorker(client);
            var data = new object[]
                {
                    new object[]
                    {
                        AionStratumMethods.MiningNotify,
                        client.ConnectionId,
                        AionConstants.AionStratumVersion
                    },
                    context.ExtraNonce1
                }
                .ToArray();

            client.Respond(data, request.Id);

            // setup worker context
            context.IsSubscribed = true;
            context.UserAgent = requestParams[0].Trim();
        }

        private async void OnAuthorize(StratumClient client, Timestamped<JsonRpcRequest> tsRequest)
        {
            var request = tsRequest.Value;
            var context = client.GetContextAs<AionWorkerContext>();

            if (request.Id == null)
            {
                client.RespondError(StratumError.Other, "missing request id", request.Id);
                return;
            }

            var requestParams = request.ParamsAs<string[]>();
            var workerValue = requestParams?.Length > 0 ? requestParams[0] : null;
            var password = requestParams?.Length > 1 ? requestParams[1] : null;
            var passParts = password?.Split(PasswordControlVarsSeparator);

            // extract worker/miner
            var workerParts = workerValue?.Split('.');
            var minerName = workerParts?.Length > 0 ? workerParts[0].Trim() : null;
            var workerName = workerParts?.Length > 1 ? workerParts[1].Trim() : null;

            // assumes that workerName is an address
            context.IsAuthorized = !string.IsNullOrEmpty(minerName) && await manager.ValidateAddressAsync(minerName);
            context.MinerName = minerName;
            context.WorkerName = workerName;

            // respond
            client.Respond(context.IsAuthorized, request.Id);

            // extract control vars from password
            var staticDiff = GetStaticDiffFromPassparts(passParts);
            if (staticDiff.HasValue &&
                (context.VarDiff != null && staticDiff.Value >= context.VarDiff.Config.MinDiff ||
                context.VarDiff == null && staticDiff.Value > context.Difficulty))
            {
                context.VarDiff = null; // disable vardiff
                context.SetDifficulty(staticDiff.Value);
            }

            EnsureInitialWorkSent(client);

            // log association
            logger.Info(() => $"[{LogCat}] [{client.ConnectionId}] = {workerValue} = {client.RemoteEndpoint.Address}");
        }

        private async Task OnSubmitAsync(StratumClient client, Timestamped<JsonRpcRequest> tsRequest)
        {
            var request = tsRequest.Value;
            var context = client.GetContextAs<AionWorkerContext>();

            try
            {
                if (request.Id == null)
                    throw new StratumException(StratumError.MinusOne, "missing request id");

                // check age of submission (aged submissions are usually caused by high server load)
                var requestAge = clock.Now - tsRequest.Timestamp.UtcDateTime;

                if (requestAge > maxShareAge)
                {
                    logger.Debug(() => $"[{LogCat}] [{client.ConnectionId}] Dropping stale share submission request (not client's fault)");
                    return;
                }

                // validate worker
                if (!context.IsAuthorized)
                    throw new StratumException(StratumError.UnauthorizedWorker, "Unauthorized worker");
                else if (!context.IsSubscribed)
                    throw new StratumException(StratumError.NotSubscribed, "Not subscribed");

                // check request
                var submitRequest = request.ParamsAs<string[]>();

                if (submitRequest.Length != 5 || submitRequest.Any(string.IsNullOrEmpty))
                    throw new StratumException(StratumError.MinusOne, "malformed PoW result");

                // recognize activity
                context.LastActivity = clock.Now;

                var poolEndpoint = poolConfig.Ports[client.PoolEndpoint.Port];
                try
                {
                    var share = await manager.SubmitShareAsync(client, submitRequest, context.Difficulty, poolEndpoint.Difficulty);
                    // success
                    client.Respond(true, request.Id);
                    messageBus.SendMessage(new ClientShare(client, share));

                    EnsureInitialWorkSent(client);

                    logger.Debug(() => $"[{LogCat}] [{client.ConnectionId}] Share accepted: D={Math.Round(share.Difficulty, 3)}");

                    // update pool stats
                    if (share.IsBlockCandidate)
                        poolStats.LastPoolBlockTime = clock.Now;
                    // included by Andre-aion
                    poolStats.NetworkDifficulty = context.Difficulty;
                        
                }
                catch (MiningCore.Stratum.StratumException ex)
                {
                    logger.Info(() => $"[{LogCat}] [{client.ConnectionId}] Exception occured: {ex.Message}");
                    throw ex;
                }

                // update client stats
                context.Stats.ValidShares++;
                UpdateVarDiff(client);

            } catch (StratumException ex) {
                client.RespondError(ex.Code, ex.Message, request.Id, false);

                // update client stats
                context.Stats.InvalidShares++;
                logger.Debug(() => $"[{LogCat}] [{client.ConnectionId}] Share rejected: {ex.Code}");

                // banning
                if(context.Stats.InvalidShares > ((poolConfig.Banning.CheckThreshold / 2) - 10) && 
                    context.Stats.InvalidShares < ((poolConfig.Banning.CheckThreshold / 2) + 10)) {
                    
                    if(!context.IsInitialWorkSent && context.IsAuthorized) {
                        EnsureInitialWorkSent(client);
                    } else if(context.IsAuthorized) {
                        OnNewJob(currentJobParams);
                    } else {
                        DisconnectClient(client);
                    }
                } else {
                    ConsiderBan(client, context, poolConfig.Banning);
                }
            }
        }

        private void EnsureInitialWorkSent(StratumClient client)
        {
            var context = client.GetContextAs<AionWorkerContext>();

            lock (context)
            {
                if (context.IsAuthorized && !context.IsInitialWorkSent)
                {
                    context.IsInitialWorkSent = true;
                    string newTarget = AionUtils.diffToTarget(context.Difficulty);
                    ArrayList arrayTarget = new ArrayList();
                    arrayTarget.Add(newTarget);
                
                    // send intial update
                    client.Notify(AionStratumMethods.MiningNotify, currentJobParams);
                    client.Notify(AionStratumMethods.SetTarget, arrayTarget);
                }
            }
        }

        private void OnNewJob(object jobParams)
        {
            currentJobParams = jobParams;

            logger.Info(() => $"[{LogCat}] Broadcasting job");

            ForEachClient(client =>
            {
                var context = client.GetContextAs<AionWorkerContext>();

                if (context.IsSubscribed && context.IsAuthorized && context.IsInitialWorkSent)
                {
                    // check alive
                    var lastActivityAgo = clock.Now - context.LastActivity;

                    if (poolConfig.ClientConnectionTimeout > 0 &&
                        lastActivityAgo.TotalSeconds > poolConfig.ClientConnectionTimeout)
                    {
                        logger.Info(() => $"[{LogCat}] [{client.ConnectionId}] Booting zombie-worker (idle-timeout exceeded)");
                        DisconnectClient(client);
                        return;
                    }

                    // varDiff: if the client has a pending difficulty change, apply it now
                    if (context.ApplyPendingDifficulty())
                        client.Notify(AionStratumMethods.SetDifficulty, new object[] { context.Difficulty });

                    string newTarget = AionUtils.diffToTarget(context.Difficulty);
                    ArrayList arrayTarget = new ArrayList();
                    arrayTarget.Add(newTarget);

                    client.Notify(AionStratumMethods.MiningNotify, currentJobParams);
                    client.Notify(AionStratumMethods.SetTarget, arrayTarget);              
                }
            });
        }

        #region Overrides
        protected override async Task SetupJobManager(CancellationToken ct)
        {
            
            manager = ctx.Resolve<AionJobManager>();
            manager.Configure(poolConfig, clusterConfig);

            var isNodeRunning = false;
            while (!isNodeRunning) 
            {
                isNodeRunning = await manager.IsDaemonRunning();
                if (!isNodeRunning) 
                {
                    logger.Info(() => $"[{LogCat}] No daemon is running. Checking again in 1 minute");
                    Thread.Sleep(1000 * 60 * 1); // 1 Minute
                }
            }   

            ValidatePoolAddress();

            await manager.StartAsync(ct);

            if (poolConfig.EnableInternalStratum == true)
	        {
		        disposables.Add(manager.Jobs.Subscribe(OnNewJob));

		        // // we need work before opening the gates
		        await manager.Jobs.Take(1).ToTask(ct);
	        }
        }

        private async void ValidatePoolAddress()
        {
            var poolAddressValid = await manager.ValidateAddressAsync(poolConfig.Address);
            if (!poolAddressValid) 
            {
                logger.ThrowLogPoolStartupException("Invalid pool address. Please check your configuration file.");
            }
        }

        protected override void InitStats()
        {
            base.InitStats();

            this.blockchainStats = manager.BlockchainStats;

        }

        protected override WorkerContextBase CreateClientContext()
        {
            return new AionWorkerContext();
        }

        protected override async Task OnRequestAsync(StratumClient client,
            Timestamped<JsonRpcRequest> tsRequest)
        {
            var request = tsRequest.Value;

            switch(request.Method)
            {
                case AionStratumMethods.Subscribe:
                    OnSubscribe(client, tsRequest);
                    break;

                case AionStratumMethods.Authorize:
                    OnAuthorize(client, tsRequest);
                    break;

                case AionStratumMethods.SubmitShare:
                    await OnSubmitAsync(client, tsRequest);
                    break;
                default:
                    logger.Debug(() => $"[{LogCat}] [{client.ConnectionId}] Unsupported RPC request: {JsonConvert.SerializeObject(request, serializerSettings)}");

                    client.RespondError(StratumError.Other, $"Unsupported request {request.Method}", request.Id);
                    break;
            }
        }

        public override double HashrateFromShares(double shares, double interval)
        {
            this.poolStats.PoolHashrate = manager.PoolHashRate;
            return this.poolStats.PoolHashrate;
        }

        protected override void OnVarDiffUpdate(StratumClient client, double newDiff)
        {
            base.OnVarDiffUpdate(client, newDiff);

            // apply immediately and notify client
            var context = client.GetContextAs<AionWorkerContext>();

            if (context.HasPendingDifficulty)
            {
                context.ApplyPendingDifficulty();
                string newTarget = AionUtils.diffToTarget(newDiff);
                ArrayList targetArray = new ArrayList();
                targetArray.Add(newTarget);

                // send job
                client.Notify(AionStratumMethods.SetDifficulty, new object[] { context.Difficulty });
                client.Notify(AionStratumMethods.MiningNotify, currentJobParams);
                client.Notify(AionStratumMethods.SetTarget, targetArray);
            }
        }

        public override void Configure(PoolConfig poolConfig, ClusterConfig clusterConfig)
        {
            base.Configure(poolConfig, clusterConfig);

            // validate mandatory extra config
        }

        #endregion // Overrides
    }
}
