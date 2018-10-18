using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using AutoMapper;
using MailKit.Search;
using MimeKit.Cryptography;
using MiningCore.Blockchain.Aion;
using MiningCore.DaemonInterface;
using Newtonsoft.Json;
using NLog;
using MiningCore.Blockchain.Aion;
using MiningCore.Configuration;
using MiningCore.JsonRpc;
using MiningCore.Persistence;
using MiningCore.Persistence.Repositories;
using MiningCore.Time;
using MiningCore.Contracts;
using MiningCore.Extensions;
using MiningCore.Persistence.Postgres.Entities;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Linq;
using Polly;
using MiningCore.Blockchain.Aion.DaemonResponses;
using MiningCore.Configuration;


namespace MiningCore.Mining
{
    
    public class PoolNetworkPercRecorder
    {
        public PoolNetworkPercRecorder(IComponentContext ctx,
            IMasterClock clock,
            IConnectionFactory cf,
            IMapper mapper,
            IStatsRepository statsRepo)
        {
            Contract.RequiresNonNull(ctx, nameof(ctx));
            Contract.RequiresNonNull(clock, nameof(clock));
            Contract.RequiresNonNull(cf, nameof(cf));
            Contract.RequiresNonNull(mapper, nameof(mapper));
            Contract.RequiresNonNull(statsRepo, nameof(statsRepo));

            this.ctx = ctx;
            this.clock = clock;
            this.cf = cf;
            this.mapper = mapper;
            this.statsRepo = statsRepo;
            

            BuildFaultHandlingPolicy();
        }

        public PoolNetworkPercRecorder()
        {
            
        }
                  
        private readonly IMasterClock clock;
        private readonly IStatsRepository statsRepo;
        private readonly IConnectionFactory cf;
        private readonly IMapper mapper;
        private readonly IComponentContext ctx;
        private readonly AutoResetEvent stopEvent = new AutoResetEvent(false);
        private readonly ConcurrentDictionary<string, IMiningPool> pools = new ConcurrentDictionary<string, IMiningPool>();
        private ClusterConfig clusterConfig;
        private Thread thread1;
        private const int RetryCount = 4;
        private Policy readFaultPolicy;
        private long delayInSeconds = 40;

        private static double PoolNetworkPercentage = 0;

        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();
        
        #region API-Surface

        public void Configure(ClusterConfig clusterConfig)
        {
            this.clusterConfig = clusterConfig;
        }

        public void AttachPool(IMiningPool pool)
        {
            pools[pool.Config.Id] = pool;
        }
        
        // send to user interface
        public double ToUIPoolNetworkPercentage()
        {
            return PoolNetworkPercentage;
        }
        
        
        private void setDelayInSeconds()
        {
            try
            {
                delayInSeconds = clusterConfig.Pools[0].HashratePercentageCalcInterval;
            }
            catch (Exception e)
            {
                delayInSeconds = 7200;
                Console.WriteLine($"There is no HashratePercentage Calc Interval");
            }
        }

        public void Start()
        {
            logger.Info(() => "Online");
            
            setDelayInSeconds();

            thread1 = new Thread(() =>
            {
                // warm-up delay
                Thread.Sleep(TimeSpan.FromSeconds(10));

                var interval = TimeSpan.FromSeconds(delayInSeconds);

                while (true)
                {
                    try
                    {
                        RecordPoolHashratePercentage();
                    }

                    catch (Exception ex) 
                    {
                        logger.Error(ex);
                    }

                    var waitResult = stopEvent.WaitOne(interval);

                    // check if stop was signalled
                    if (waitResult)
                        break;
                }
            });

            thread1.Name = "PoolHashRatePercentageRecorder";
            thread1.Start();
        }
        
        public void Stop()
        {
            logger.Info(() => "Stopping ..");

            stopEvent.Set();
            thread1.Join();

            logger.Info(() => "Stopped");
        }

        #endregion // API-Surface
        
        // BEGIN MINER HASHRATE PERCENTAGE CALCULATION
            
        private void RecordPoolHashratePercentage()
        {
            Console.WriteLine($"Hashrate percentage calculation call at {DateTime.Now}");

            var poolIds = pools.Keys;
            var start = clock.Now;
            var stats = new PoolNetworkPercentageStats{};

           
            foreach (var poolId in poolIds)
            {
                logger.Info(() => $"Updating hashrates for pool {poolId}");
                                   
            
                try
                {
                    string poolAddress = pools[poolId].Config.Address;
                    PoolNetworkPercentage = 100*GetPoolNetworkPercentage(poolAddress, pools[poolId].Config);
                    // make the call to write to the database
                    Persist(poolId, PoolNetworkPercentage, stats);
                }
                catch(Exception e)
                {
                    Console.WriteLine($"PLEASE ENSURE (IN YOUR 'aion-pool.config') THAT " +
                                      $"YOUR POOL ADDRESS IS CORRECT. YOUR MINER ADDRESS MAY " +
                                      $"NOT BE ON THE NETWORK");
                    Console.WriteLine($"Error calculating Pool({poolId}) Hashrate network percentage %");
                    Console.WriteLine($"Pool({poolId}) network percentage: {PoolNetworkPercentage}");
                    Console.WriteLine($"Exception: {e}");
                }
                
            }
        }
        
        
        // Write to database
        private void Persist(string poolId, double networkhashrate, PoolNetworkPercentageStats stats)
        {

            cf.RunTx((con, tx) =>
            {
                stats.PoolId = poolId;
                stats.NetworkPercentage = networkhashrate;
                statsRepo.InsertPoolNetworkPercentageStats(con, tx, stats);
            });

        }
        
        
        private void BuildFaultHandlingPolicy()
        {
            var retry = Policy
                .Handle<DbException>()
                .Or<SocketException>()
                .Or<TimeoutException>()
                .Retry(RetryCount, OnPolicyRetry);

            readFaultPolicy = retry;
        }

        private static void OnPolicyRetry(Exception ex, int retry, object context)
        {
            logger.Warn(() => $"Retry {retry} due to {ex.Source}: {ex.GetType().Name} ({ex.Message})");
        }

        private double GetPoolNetworkPercentage(string poolAddress, PoolConfig poolConfig)
        {
            JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings();
            DaemonClient daemon = new DaemonClient(jsonSerializerSettings);
            var daemonEndpoints = poolConfig.Daemons
                .Where(x => string.IsNullOrEmpty(x.Category))
                .ToArray();            
            daemon.Configure(daemonEndpoints);
            var response = daemon.ExecuteCmdAnyAsync<GetMinerHashRateResponse>(AionCommands.GetMinerStats, new [] { poolAddress }).Result;
            var networkPercentage = (double) Convert.ToDouble(response.Response.MinerHashrateShare);
            return networkPercentage;
        }

    }

   
}