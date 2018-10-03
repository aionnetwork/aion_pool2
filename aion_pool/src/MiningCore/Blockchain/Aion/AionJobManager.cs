using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using MiningCore.Blockchain.Bitcoin;
using MiningCore.Blockchain.Bitcoin.DaemonResponses;
using MiningCore.Blockchain.ZCash.Configuration;
using MiningCore.Blockchain.ZCash.DaemonResponses;
using MiningCore.Configuration;
using MiningCore.Contracts;
using MiningCore.DaemonInterface;
using MiningCore.Extensions;
using MiningCore.Notifications;
using MiningCore.Stratum;
using MiningCore.Time;
using NBitcoin;
using NLog;
using NBitcoin.DataEncoders;
using Newtonsoft.Json;
using Block = MiningCore.Blockchain.Ethereum.DaemonResponses.Block;

namespace MiningCore.Blockchain.Aion
{
    public class AionJobManager : JobManagerBase<AionJob>
    {
        public AionJobManager(
            IComponentContext ctx,
            NotificationService notificationService,
            IMasterClock clock) :
            base(ctx)
        {
            Contract.RequiresNonNull(ctx, nameof(ctx));
            Contract.RequiresNonNull(notificationService, nameof(notificationService));
            Contract.RequiresNonNull(clock, nameof(clock));

            this.clock = clock;
            this.notificationService = notificationService;
        }

        public IObservable<object> Jobs { get; private set; }
        public BlockchainStats BlockchainStats { get; } = new BlockchainStats();
        public double PoolHashRate = 0;
        protected readonly Dictionary<string, AionJob> validJobs = new Dictionary<string, AionJob>();
        private DaemonEndpointConfig[] daemonEndpoints;
        private DaemonClient daemon;
        private readonly NotificationService notificationService;
        private readonly IMasterClock clock;
        private readonly AionExtraNonceProvider extraNonceProvider = new AionExtraNonceProvider();
        private const int MaxBlockBacklog = 3;
        private string poolAddress = "";

        protected object[] getBlockTemplateParams = new object[]
        {
            new
            {
                capabilities = new[] { "coinbasetxn", "workid", "coinbase/append" },
                rules = new[] { "segwit" }
            }
        };
        public override void Configure(PoolConfig poolConfig, ClusterConfig clusterConfig)
        {

            // extract standard daemon endpoints
            daemonEndpoints = poolConfig.Daemons
                .Where(x => string.IsNullOrEmpty(x.Category))
                .ToArray();

            base.Configure(poolConfig, clusterConfig);
            poolAddress = poolConfig.Address;
        }

        public void PrepareWorker(StratumClient client)
        {
            var context = client.GetContextAs<AionWorkerContext>();
            context.ExtraNonce1 = extraNonceProvider.Next();
        }

        public async Task<Share> SubmitShareAsync(StratumClient worker,
            string[] request, double stratumDifficulty, double stratumDifficultyBase)
        {
            Contract.RequiresNonNull(worker, nameof(worker));
            Contract.RequiresNonNull(request, nameof(request));

            logger.LogInvoke(LogCat, new[] { worker.ConnectionId });
            var context = worker.GetContextAs<AionWorkerContext>();

            var miner = request[0];
            var jobId = request[1];
            var time = request[2];
            var nonce = request[3];
            var soln = request[4];
            AionJob job;

            // stale?
            lock(jobLock)
            {
                if (!validJobs.TryGetValue(jobId, out job))
                    throw new StratumException(StratumError.MinusOne, "stale share");
            }

            // validate & process
            var (share, fullNonceHex, solution, headerHash, nTime) = await job.ProcessShare(worker, nonce, time, soln);
            
            // enrich share with common data
            share.PoolId = poolConfig.Id;
            share.Source = clusterConfig.ClusterName;
            share.Created = clock.Now;
            
            // if block candidate, submit & check if accepted by network
            if (share.IsBlockCandidate)
            {
                logger.Info(() => $"[{LogCat}] Submitting block {share.BlockHeight}");

                share.IsBlockCandidate = await SubmitBlockAsync(share, fullNonceHex, headerHash, solution, nTime);

                if (share.IsBlockCandidate)
                {
                    logger.Info(() => $"[{LogCat}] Daemon accepted block {share.BlockHeight} submitted by {context.MinerName}");
                } 
            }

            return share;
        }

        public async Task<bool> ValidateAddressAsync(string address)
        {
            Contract.Requires<ArgumentException>(!string.IsNullOrEmpty(address), $"{nameof(address)} must not be empty");

            var result = await daemon.ExecuteCmdAnyAsync<ValidateAddressResponse>(
                AionCommands.ValidateAddress, new[] { address });

            return result.Response != null && result.Response.IsValid;;
        }

        public async Task<bool> IsDaemonRunning() 
        {
            var result = await  daemon.ExecuteCmdAnyAsync<object>(AionCommands.Ping);

            return result.Response != null && result.Response.Equals("pong");
        }
    
        protected override void ConfigureDaemons()
        {
            var jsonSerializerSettings = ctx.Resolve<JsonSerializerSettings>();

            daemon = new DaemonClient(jsonSerializerSettings);
            daemon.Configure(daemonEndpoints);
        }

        protected override Task<bool> AreDaemonsHealthyAsync()
        {
            return Task.FromResult(true);
        }

        protected override Task<bool> AreDaemonsConnectedAsync()
        {
            return Task.FromResult(true);
        }

        protected override Task EnsureDaemonsSynchedAsync(CancellationToken ct)
        {
            return Task.FromResult(true);
        }

        protected override Task PostStartInitAsync(CancellationToken ct)
        {
            SetupJobUpdates();
            GetBlockTemplateAsync();

            return Task.FromResult(true);
        }

        protected virtual void SetupJobUpdates()
        {
	        if (poolConfig.EnableInternalStratum == false)
		        return;

            Jobs = Observable.Interval(TimeSpan.FromMilliseconds(poolConfig.BlockRefreshInterval))
                    .Select(_ => Observable.FromAsync(UpdateJobAsync))
                    .Concat()
                    .Do(isNew =>
                    {
                        if (isNew)
                            logger.Info(() => $"[{LogCat}] New block {currentJob.BlockTemplate.Height} detected");
                    })
                    .Where(isNew => isNew)
                    .Select(_ => GetJobParamsForStratum())
                    .Publish()
                    .RefCount();
        }

        protected async Task<bool> UpdateJobAsync()
        {
            logger.LogInvoke(LogCat);

            try
            {
                return UpdateJob(await GetBlockTemplateAsync());
            }

            catch(Exception ex)
            {
                logger.Error(ex, () => $"[{LogCat}] Error during {nameof(UpdateJobAsync)}");
            }

            return false;
        }

        protected bool UpdateJob(AionBlockTemplate blockTemplate)
        {
            logger.LogInvoke(LogCat);

            try
            {
                // may happen if daemon is currently not connected to peers
                if (blockTemplate == null || blockTemplate.HeaderHash?.Length == 0)
                    return false;
        
                var job = currentJob;
                var isNew = currentJob == null || job.BlockTemplate.HeaderHash != blockTemplate.HeaderHash;
                
                if (isNew)
                {
                    var jobId = NextJobId("x8");
                    job = new AionJob(jobId, blockTemplate, logger, daemon);
                    lock (jobLock)
                    {
                        // add jobs
                        validJobs[jobId] = job;

                        // remove old ones
                        var obsoleteKeys = validJobs.Keys
                            .Where(key => (long) validJobs[key].BlockTemplate.Height < (long) (job.BlockTemplate.Height - MaxBlockBacklog)).ToArray();

                        foreach (var key in obsoleteKeys)
                            validJobs.Remove(key);
                    }

                    currentJob = job;

                    // update stats
                    BlockchainStats.LastNetworkBlockTime = clock.Now;
                    BlockchainStats.BlockHeight = (long) currentJob.BlockTemplate.Height;
                    var (networkHashrate, poolHashRate) = getNetworkAndMinerHashRate();
                    BlockchainStats.NetworkHashrate = networkHashrate;
                    //////       ADDED LINE BELOW    
                    BlockchainStats.NetworkDifficulty = job.Difficulty;
                    PoolHashRate = poolHashRate;

                }  

                return isNew;
            }
           
            catch (Exception ex)
            {
                logger.Error(ex, () => $"[{LogCat}] Error during {nameof(UpdateJob)}");
            }

            return false;
        }

        private async Task<bool> SubmitBlockAsync(Share share, string fullNonceHex, string headerHash, string solution, string nTime)
        {
            // submit work
            var response = await daemon.ExecuteCmdAnyAsync<object>(AionCommands.SubmitWork, new[]
            {
                fullNonceHex,
                solution,
                headerHash
            });

            if (response.Error != null)
            {
                var error = response.Error?.Message ?? response?.Response?.ToString();

                logger.Warn(() => $"[{LogCat}] Block {share.BlockHeight} submission failed with: {error}");
                notificationService.NotifyAdmin("Block submission failed", $"Pool {poolConfig.Id} {(!string.IsNullOrEmpty(share.Source) ? $"[{share.Source.ToUpper()}] " : string.Empty)}failed to submit block {share.BlockHeight}: {error}");

                return false;
            }

            await Jobs.Take(1);
            return true;
          
        }

        private async Task<AionBlockTemplate> GetBlockTemplateAsync()
        {
            logger.LogInvoke(LogCat);
            var result = await daemon.ExecuteCmdAnyAsync<AionBlockTemplate>(AionCommands.GetWork);
            return result.Response;
        }

        private object[] GetJobParamsForStratum()
        {
            var job = currentJob;

            if(job != null)
            {
                return new object[]
                {
                    job.Id,
                    true, // clean job
                    job.BlockTemplate.Target,
                    job.BlockTemplate.HeaderHash
                };
            }

            return new object[0];
        }

        public (double, double) getNetworkAndMinerHashRate() 
        {
            var response = daemon.ExecuteCmdAnyAsync<DaemonResponses.GetMinerHashRateResponse>(AionCommands.GetMinerStats, new [] { poolAddress }).Result;
            var networkHashRate = (double) Convert.ToDouble(response.Response.NetworkHashrate);
            var minerHashRate = (double) Convert.ToDouble(response.Response.MinerHashrate);
            return (networkHashRate, minerHashRate);
        }
    }    
}       
