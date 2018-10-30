﻿/*
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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Primitives;
using MiningCore.Api.Extensions;
using MiningCore.Api.Responses;
using MiningCore.Blockchain;
using MiningCore.Configuration;
using MiningCore.Extensions;
using MiningCore.Messaging;
using MiningCore.Mining;
using MiningCore.Notifications.Messages;
using MiningCore.Persistence;
using MiningCore.Persistence.Model;
using MiningCore.Persistence.Repositories;
using MiningCore.Time;
using MiningCore.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NLog;
using Contract = MiningCore.Contracts.Contract;

namespace MiningCore.Api
{
    public class ApiServer
    {
        public ApiServer(
            IMapper mapper,
            IConnectionFactory cf,
            IBlockRepository blocksRepo,
            IPaymentRepository paymentsRepo,
            IStatsRepository statsRepo,
            ICoinInfoRepository coinInfoRepo,
            IMasterClock clock,
            IMessageBus messageBus)
        {
            Contract.RequiresNonNull(cf, nameof(cf));
            Contract.RequiresNonNull(statsRepo, nameof(statsRepo));
            Contract.RequiresNonNull(blocksRepo, nameof(blocksRepo));
            Contract.RequiresNonNull(paymentsRepo, nameof(paymentsRepo));
            Contract.RequiresNonNull(coinInfoRepo, nameof(coinInfoRepo));
            Contract.RequiresNonNull(mapper, nameof(mapper));
            Contract.RequiresNonNull(clock, nameof(clock));
            Contract.RequiresNonNull(messageBus, nameof(messageBus));

            this.cf = cf;
            this.statsRepo = statsRepo;
            this.blocksRepo = blocksRepo;
            this.paymentsRepo = paymentsRepo;
            this.coinInfoRepo = coinInfoRepo;
            this.mapper = mapper;
            this.clock = clock;

            messageBus.Listen<BlockNotification>().Subscribe(OnBlockNotification);

            requestMap = new Dictionary<Regex, Func<HttpContext, Match, Task>>
            {
                { new Regex("^/api/pools$", RegexOptions.Compiled), GetPoolInfosAsync },
                { new Regex("^/api/pools/(?<poolId>[^/]+)/performance$", RegexOptions.Compiled), GetPoolPerformanceAsync },
                { new Regex("^/api/pools/(?<poolId>[^/]+)/miners$", RegexOptions.Compiled), PagePoolMinersAsync },
                { new Regex("^/api/pools/(?<poolId>[^/]+)/blocks$", RegexOptions.Compiled), PagePoolBlocksPagedAsync },
                { new Regex("^/api/pools/(?<poolId>[^/]+)/payments$", RegexOptions.Compiled), PagePoolPaymentsAsync },
                { new Regex("^/api/pools/(?<poolId>[^/]+)/stats$", RegexOptions.Compiled), GetMiningPoolStats },
                { new Regex("^/api/pools/(?<poolId>[^/]+)/stats/percentage$", RegexOptions.Compiled), GetMiningPoolPercentageStats },
                { new Regex("^/api/pools/(?<poolId>[^/]+)/stats/hashrate$", RegexOptions.Compiled), GetMiningPoolHashrateStats },
                { new Regex("^/api/pools/(?<poolId>[^/]+)/stats/miners$", RegexOptions.Compiled), GetMiningPoolMinersStats },
                
                { new Regex("^/api/pools/(?<poolId>[^/]+)$", RegexOptions.Compiled), GetPoolInfoAsync },
                { new Regex("^/api/pools/(?<poolId>[^/]+)/miners/(?<address>[^/]+)/payments$", RegexOptions.Compiled), PageMinerPaymentsAsync },
                { new Regex("^/api/pools/(?<poolId>[^/]+)/miners/(?<address>[^/]+)/balancechanges$", RegexOptions.Compiled), PageMinerBalanceChangesAsync },
                { new Regex("^/api/pools/(?<poolId>[^/]+)/miners/(?<address>[^/]+)/performance$", RegexOptions.Compiled), GetMinerPerformanceAsync },
                { new Regex("^/api/pools/(?<poolId>[^/]+)/miners/(?<address>[^/]+)$", RegexOptions.Compiled), GetMinerInfoAsync },
                { new Regex("^/api/coin/(?<coinType>[^/]+)$", RegexOptions.Compiled), GetCoinInfo }
            };

            requestMapAdmin = new Dictionary<Regex, Func<HttpContext, Match, Task>>
            {
                { new Regex("^/api/admin/forcegc$", RegexOptions.Compiled), HandleForceGcAsync },
                { new Regex("^/api/admin/stats/gc$", RegexOptions.Compiled), HandleGcStatsAsync },
            };
        }

        private readonly IConnectionFactory cf;
        private readonly IStatsRepository statsRepo;
        private readonly ICoinInfoRepository coinInfoRepo;
        private readonly IBlockRepository blocksRepo;
        private readonly IPaymentRepository paymentsRepo;
        private readonly IMapper mapper;
        private readonly IMasterClock clock;

        private ClusterConfig clusterConfig;
        private IWebHost webHost;
        private IWebHost webHostAdmin;
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();
        private static readonly Encoding encoding = new UTF8Encoding(false);

        private static readonly JsonSerializer serializer = new JsonSerializer
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc
        };

        private readonly Dictionary<Regex, Func<HttpContext, Match, Task>> requestMap;
        private readonly Dictionary<Regex, Func<HttpContext, Match, Task>> requestMapAdmin;

        private PoolConfig GetPool(HttpContext context, Match m)
        {
            var poolId = m.Groups["poolId"]?.Value;

            if (!string.IsNullOrEmpty(poolId))
            {
                var pool = clusterConfig.Pools.FirstOrDefault(x => x.Id == poolId && x.Enabled);

                if (pool != null)
                    return pool;
            }

            context.Response.StatusCode = 404;
            return null;
        }

        private async Task SendJsonAsync(HttpContext context, object response)
        {
            context.Response.ContentType = "application/json";

            // add CORS headers
            context.Response.Headers.Add("Access-Control-Allow-Origin", new StringValues("*"));
            context.Response.Headers.Add("Access-Control-Allow-Methods", new StringValues("GET, POST, DELETE, PUT, OPTIONS, HEAD"));

            using (var stream = context.Response.Body)
            {
                using (var writer = new StreamWriter(stream, encoding))
                {
                    serializer.Serialize(writer, response);

                    // append newline
                    await writer.WriteLineAsync();
                    await writer.FlushAsync();
                }
            }
        }

        private void OnBlockNotification(BlockNotification notification)
        {
        }

        #region API

        private async Task HandleRequest(HttpContext context)
        {
            var request = context.Request;

            try
            {
                logger.Debug(() => $"Processing request {request.GetEncodedPathAndQuery()}");

                foreach (var path in requestMap.Keys)
                {
                    var m = path.Match(request.Path);

                    if (m.Success)
                    {
                        var handler = requestMap[path];
                        await handler(context, m);
                        return;
                    }
                }

                context.Response.StatusCode = 404;
            }

            catch (Exception ex)
            {
                logger.Error(ex);
                throw;
            }
        }

        private WorkerPerformanceStatsContainer[] GetMinerPerformanceInternal(string mode, PoolConfig pool, string address)
        {
            Persistence.Model.Projections.WorkerPerformanceStatsContainer[] stats;
            var end = clock.Now;

            if (mode == "day" || mode != "month")
            {
                // set range
                if (end.Minute < 30)
                    end = end.AddHours(-1);

                end = end.AddMinutes(-end.Minute);
                end = end.AddSeconds(-end.Second);

                var start = end.AddDays(-1);

                stats = cf.Run(con => statsRepo.GetMinerPerformanceBetweenHourly(
                    con, pool.Id, address, start, end));
            }

            else
            {
                if (end.Hour < 12)
                    end = end.AddDays(-1);

                end = end.Date;

                // set range
                var start = end.AddMonths(-1);

                stats = cf.Run(con => statsRepo.GetMinerPerformanceBetweenDaily(
                    con, pool.Id, address, start, end));
            }

            // map
            var result = mapper.Map<WorkerPerformanceStatsContainer[]>(stats);
            return result;
        }

        private async Task GetPoolInfosAsync(HttpContext context, Match m)
        {
            var response = new GetPoolsResponse
            {
                Pools = clusterConfig.Pools.Where(x => x.Enabled).Select(config =>
                 {
                     // load stats
                     var stats = cf.Run(con => statsRepo.GetLastPoolStats(con, config.Id));

                     // map
                     var result = config.ToPoolInfo(mapper, stats);

                     // enrich
                     result.TotalPaid = cf.Run(con => statsRepo.GetTotalPoolPayments(con, config.Id));
#if DEBUG
                     var from = new DateTime(2018, 1, 6, 16, 0, 0);
#else
                    var from = clock.Now.AddDays(-1);
#endif
                     result.TopMiners = cf.Run(con => statsRepo.PagePoolMinersByHashrate(
                              con, config.Id, from, 0, 15)).Results
                          .Select(mapper.Map<MinerPerformanceStats>)
                          .ToArray();

                     return result;
                 }).ToArray()
            };

            await SendJsonAsync(context, response);
        }

        private async Task GetPoolInfoAsync(HttpContext context, Match m)
        {
            var pool = GetPool(context, m);
            if (pool == null)
                return;

            // load stats
            var stats = cf.Run(con => statsRepo.GetLastPoolStats(con, pool.Id));

            var response = new GetPoolResponse
            {
                Pool = pool.ToPoolInfo(mapper, stats)
            };

            // enrich
            response.Pool.TotalPaid = cf.Run(con => statsRepo.GetTotalPoolPayments(con, pool.Id));
#if DEBUG
            var from = new DateTime(2018, 1, 7, 16, 0, 0);
#else
            var from = clock.Now.AddDays(-1);
#endif

            response.Pool.TopMiners = cf.Run(con => statsRepo.PagePoolMinersByHashrate(
                    con, pool.Id, from, 0, 15)).Results
                .Select(mapper.Map<MinerPerformanceStats>)
                .ToArray();

            await SendJsonAsync(context, response);
        }

        
        private (DateTime, DateTime, StatsGranularity) parseStatsRequestParams(HttpContext context) {
            //range
            var end = context.GetQueryParameter<DateTime>("end", clock.Now);
            var start = context.GetQueryParameter<DateTime>("start", end.AddDays(-1));

            // stats granularity [minutely, hourly, daily]
            var granularity = (StatsGranularity) StatsGranularity.Parse(typeof(StatsGranularity), context.GetQueryParameter<string>("granularity", "minutely"), true);
            
            return (start, end, granularity);
        }

        private async Task GetMiningPoolMinersStats(HttpContext context, Match m)
        {
            var pool = GetPool(context, m);
            if (pool == null)
                return;
            var (start,end,granularity) = parseStatsRequestParams(context);
            PoolValueStat[] stats = cf.Run(con => statsRepo.GetPoolConnectedMiners(con, pool.Id, start, end, granularity));
            await SendJsonAsync(context, stats);
        }

        private async Task GetMiningPoolPercentageStats(HttpContext context, Match m)
        {
            var pool = GetPool(context, m);
            if (pool == null)
                return;
            var (start,end,granularity) = parseStatsRequestParams(context);
            PoolValueStat[] stats = cf.Run(con => statsRepo.GetPoolNetworkPercentage(con, pool.Id, start, end, granularity));
            await SendJsonAsync(context, stats);
        }

        private async Task GetMiningPoolHashrateStats(HttpContext context, Match m)
        {
            var pool = GetPool(context, m);
            if (pool == null)
                return;
            var (start,end,granularity) = parseStatsRequestParams(context); 
            PoolValueStat[] stats = cf.Run(con => statsRepo.GetPoolHashrate(con, pool.Id, start, end, granularity));
            await SendJsonAsync(context, stats);
        }
        
         private async Task GetMiningPoolStats(HttpContext context, Match m)
        {
            var pool = GetPool(context, m);
            if (pool == null)
                return;

            // set range
            var end = clock.Now;
            var start = end.AddDays(-1);

            var stats = cf.Run(con => statsRepo.GetPoolPerformanceBetweenMinutely(con, pool.Id, start, end));

            var response = new GetPoolStatsResponse
            {
                Stats = stats.Select(mapper.Map<AggregatedPoolStats>).ToArray()
            };

            await SendJsonAsync(context, response);
        }

        private async Task GetPoolPerformanceAsync(HttpContext context, Match m)
        {
            var pool = GetPool(context, m);
            if (pool == null)
                return;

            // set range
            var end = clock.Now;
            var start = end.AddDays(-1);

            var stats = cf.Run(con => statsRepo.GetPoolPerformanceBetweenHourly(
                con, pool.Id, start, end));

            var response = new GetPoolStatsResponse
            {
                Stats = stats.Select(mapper.Map<AggregatedPoolStats>).ToArray()
            };

            await SendJsonAsync(context, response);
        }

        private async Task PagePoolMinersAsync(HttpContext context, Match m)
        {
            var pool = GetPool(context, m);
            if (pool == null)
                return;

            // set range
            var end = clock.Now;
            var start = end.AddDays(-1);

            var page = context.GetQueryParameter<int>("page", 0);
            var pageSize = context.GetQueryParameter<int>("pageSize", 20);

            if (pageSize == 0)
            {
                context.Response.StatusCode = 500;
                return;
            }

            var result = cf.Run(con => statsRepo.PagePoolMinersByHashrate(
                    con, pool.Id, start, page, pageSize));

            await SendJsonAsync(context, new PagedResponse<MinerPerformanceStats>
            {
                Results = result.Results.Select(mapper.Map<MinerPerformanceStats>).ToArray(),
                Total = result.Total
            });
        }

        private async Task PagePoolBlocksPagedAsync(HttpContext context, Match m)
        {
            var pool = GetPool(context, m);
            if (pool == null)
                return;

            var page = context.GetQueryParameter<int>("page", 0);
            var pageSize = context.GetQueryParameter<int>("pageSize", 20);
            var blockStatusString = context.GetQueryParameter<string>("status", null);

            if (pageSize == 0)
            {
                context.Response.StatusCode = 500;
                return;
            }

            BlockStatus[] blockStatus;
            if (!String.IsNullOrEmpty(blockStatusString))
            {
                blockStatusString = blockStatusString.First().ToString().ToUpper() + blockStatusString.Substring(1);
                try
                {
                    blockStatus = new[] { (BlockStatus)Enum.Parse(typeof(BlockStatus), blockStatusString) };
                }
                catch (ArgumentException ex)
                {
                    logger.Info(ex, () => $"[{blockStatusString}] can not be cast to a valid block status");
                    context.Response.StatusCode = 400;
                    return;
                }
            }
            else
            {
                blockStatus = new[] { BlockStatus.Confirmed, BlockStatus.Pending, BlockStatus.Orphaned };
            }

            var blocks = cf.Run(con => blocksRepo.PageDistinctBlocks(con, pool.Id, blockStatus, page, pageSize))
                .Select(mapper.Map<Responses.Block>)
                .ToArray();

            // enrich blocks
            CoinMetaData.BlockInfoLinks.TryGetValue(pool.Coin.Type, out var blockInfobaseDict);

            foreach (var block in blocks)
            {
                // compute infoLink
                if (blockInfobaseDict != null)
                {
                    blockInfobaseDict.TryGetValue(!string.IsNullOrEmpty(block.Type) ? block.Type : string.Empty, out var blockInfobaseUrl);

                    if (!string.IsNullOrEmpty(blockInfobaseUrl))
                    {
                        if (blockInfobaseUrl.Contains(CoinMetaData.BlockHeightPH))
                            block.InfoLink = blockInfobaseUrl.Replace(CoinMetaData.BlockHeightPH, block.BlockHeight.ToString(CultureInfo.InvariantCulture));
                        else if (blockInfobaseUrl.Contains(CoinMetaData.BlockHashPH) && !string.IsNullOrEmpty(block.Hash))
                            block.InfoLink = blockInfobaseUrl.Replace(CoinMetaData.BlockHashPH, block.Hash);
                    }
                }
            }

            await SendJsonAsync(context, blocks);
        }

        private async Task PagePoolPaymentsAsync(HttpContext context, Match m)
        {
            var pool = GetPool(context, m);
            if (pool == null)
                return;

            var page = context.GetQueryParameter<int>("page", 0);
            var pageSize = context.GetQueryParameter<int>("pageSize", 20);

            if (pageSize == 0)
            {
                context.Response.StatusCode = 500;
                return;
            }

            var result = cf.Run(con => paymentsRepo.PagePayments(
                    con, pool.Id, null, page, pageSize));
            var payments = result.Results
                .Select(mapper.Map<Responses.Payment>)
                .ToArray();


            // enrich payments
            CoinMetaData.TxInfoLinks.TryGetValue(pool.Coin.Type, out var txInfobaseUrl);
            CoinMetaData.AddressInfoLinks.TryGetValue(pool.Coin.Type, out var addressInfobaseUrl);

            foreach (var payment in payments)
            {
                // compute transaction infoLink
                if (!string.IsNullOrEmpty(txInfobaseUrl))
                    payment.TransactionInfoLink = string.Format(txInfobaseUrl, payment.TransactionConfirmationData);

                // pool wallet link
                if (!string.IsNullOrEmpty(addressInfobaseUrl))
                    payment.AddressInfoLink = string.Format(addressInfobaseUrl, payment.Address);
            }

            await SendJsonAsync(context, new PagedResponse<Responses.Payment>
            {
                Total = result.Total,
                Results = payments
            });
        }

        private async Task GetMinerInfoAsync(HttpContext context, Match m)
        {
            var pool = GetPool(context, m);
            if (pool == null)
                return;

            var address = m.Groups["address"]?.Value;
            if (string.IsNullOrEmpty(address))
            {
                context.Response.StatusCode = 404;
                return;
            }

            var perfMode = context.GetQueryParameter<string>("perfMode", "day");

            var statsResult = cf.RunTx((con, tx) =>
                statsRepo.GetMinerStats(con, tx, pool.Id, address), true, IsolationLevel.Serializable);

            MinerStats stats = null;

            if (statsResult != null)
            {
                stats = mapper.Map<MinerStats>(statsResult);

                // optional fields
                if (statsResult.LastPayment != null)
                {
                    // Set timestamp of last payment
                    stats.LastPayment = statsResult.LastPayment.Created;

                    // Compute info link
                    if (CoinMetaData.TxInfoLinks.TryGetValue(pool.Coin.Type, out var baseUrl))
                        stats.LastPaymentLink = string.Format(baseUrl, statsResult.LastPayment.TransactionConfirmationData);
                }

                stats.PerformanceSamples = GetMinerPerformanceInternal(perfMode, pool, address);
            }

            await SendJsonAsync(context, stats);
        }

        private async Task PageMinerPaymentsAsync(HttpContext context, Match m)
        {
            var pool = GetPool(context, m);
            if (pool == null)
                return;

            var address = m.Groups["address"]?.Value;
            if (string.IsNullOrEmpty(address))
            {
                context.Response.StatusCode = 404;
                return;
            }

            var page = context.GetQueryParameter<int>("page", 0);
            var pageSize = context.GetQueryParameter<int>("pageSize", 20);

            if (pageSize == 0)
            {
                context.Response.StatusCode = 500;
                return;
            }


            var result = cf.Run(con => paymentsRepo.PagePayments(
               con, pool.Id, address, page, pageSize));
            var payments = result.Results
                .Select(mapper.Map<Responses.Payment>)
                .ToArray();

            // enrich payments
            CoinMetaData.TxInfoLinks.TryGetValue(pool.Coin.Type, out var txInfobaseUrl);
            CoinMetaData.AddressInfoLinks.TryGetValue(pool.Coin.Type, out var addressInfobaseUrl);

            foreach (var payment in payments)
            {
                // compute transaction infoLink
                if (!string.IsNullOrEmpty(txInfobaseUrl))
                    payment.TransactionInfoLink = string.Format(txInfobaseUrl, payment.TransactionConfirmationData);

                // pool wallet link
                if (!string.IsNullOrEmpty(addressInfobaseUrl))
                    payment.AddressInfoLink = string.Format(addressInfobaseUrl, payment.Address);
            }

            await SendJsonAsync(context, new PagedResponse<Responses.Payment>{
                Total = result.Total,
                Results = payments
            });
        }

        private async Task PageMinerBalanceChangesAsync(HttpContext context, Match m)
        {
            var pool = GetPool(context, m);
            if (pool == null)
                return;

            var address = m.Groups["address"]?.Value;
            if (string.IsNullOrEmpty(address))
            {
                context.Response.StatusCode = 404;
                return;
            }

            var page = context.GetQueryParameter<int>("page", 0);
            var pageSize = context.GetQueryParameter<int>("pageSize", 20);

            if (pageSize == 0)
            {
                context.Response.StatusCode = 500;
                return;
            }

            var balanceChanges = cf.Run(con => paymentsRepo.PageBalanceChanges(
                    con, pool.Id, address, page, pageSize))
                .Select(mapper.Map<Responses.BalanceChange>)
                .ToArray();

            await SendJsonAsync(context, balanceChanges);
        }

        private async Task GetMinerPerformanceAsync(HttpContext context, Match m)
        {
            var pool = GetPool(context, m);
            if (pool == null)
                return;

            var address = m.Groups["address"]?.Value;
            if (string.IsNullOrEmpty(address))
            {
                context.Response.StatusCode = 404;
                return;
            }

            var mode = context.GetQueryParameter<string>("mode", "day").ToLower(); // "day" or "month"
            var result = GetMinerPerformanceInternal(mode, pool, address);

            await SendJsonAsync(context, result);
        }

        private async Task HandleForceGcAsync(HttpContext context, Match m)
        {
            GC.Collect(2, GCCollectionMode.Forced);

            await SendJsonAsync(context, true);
        }

        private async Task HandleGcStatsAsync(HttpContext context, Match m)
        {
            // update other stats
            Program.gcStats.GcGen0 = GC.CollectionCount(0);
            Program.gcStats.GcGen1 = GC.CollectionCount(1);
            Program.gcStats.GcGen2 = GC.CollectionCount(2);
            Program.gcStats.MemAllocated = FormatUtil.FormatCapacity(GC.GetTotalMemory(false));

            await SendJsonAsync(context, Program.gcStats);
        }

        private void StartApi(ClusterConfig clusterConfig)
        {
            var address = clusterConfig.Api?.ListenAddress != null
                ? (clusterConfig.Api.ListenAddress != "*" ? IPAddress.Parse(clusterConfig.Api.ListenAddress) : IPAddress.Any)
                : IPAddress.Parse("127.0.0.1");

            var port = clusterConfig.Api?.Port ?? 4000;

            webHost = new WebHostBuilder()
                .Configure(app => { app.Run(HandleRequest); })
                .UseKestrel(options => { options.Listen(address, port); })
                .Build();

            webHost.Start();

            logger.Info(() => $"API Online @ {address}:{port}");
        }


        private async Task GetCoinInfo(HttpContext context, Match m)
        {
            var coinType = (CoinType)CoinType.Parse(typeof(CoinType), m.Groups["coinType"]?.Value, true);
            var coinInfo = cf.Run(con => coinInfoRepo.GetCoinInfo(con, coinType));
            if (coinInfo == null || DateTime.Now.Subtract(coinInfo.Updated).TotalMinutes > 5)
            {
                coinInfo = GetCoinInfoFromCryptoCompare(coinType);
                cf.RunTx((con, tx) => coinInfoRepo.AddCoinInfo(con, tx, coinInfo));
            }
            
            var response = new CoinInfoResponse
            {
                CoinType = coinInfo.CoinType.ToString(),
                Name = coinInfo.Name,
                PriceBTC = coinInfo.PriceBTC,
                PriceUSD = coinInfo.PriceUSD
            };

            await SendJsonAsync(context, response);
        }

        private CoinInfo GetCoinInfoFromCryptoCompare(CoinType coinType)
        {
            var url = "https://min-api.cryptocompare.com/data/price?fsym=" + coinType.ToString() + "&tsyms=BTC,USD";
            var json = new WebClient().DownloadString(url);
            var response = JsonConvert.DeserializeObject<CryoptoCompareResponse>(json);
            return new CoinInfo
            {
                CoinType = coinType,
                Name = coinType.ToString(),
                CoinMarketCapId = 0,
                PriceUSD = response.USD,
                PriceBTC = response.BTC,
                Updated = DateTime.Now
            };
        }

        #endregion // API

        #region Admin API

        private async Task HandleRequestAdmin(HttpContext context)
        {
            var request = context.Request;

            try
            {
                logger.Debug(() => $"Processing request {request.GetEncodedPathAndQuery()}");

                foreach (var path in requestMapAdmin.Keys)
                {
                    var m = path.Match(request.Path);

                    if (m.Success)
                    {
                        var handler = requestMapAdmin[path];
                        await handler(context, m);
                        return;
                    }
                }

                context.Response.StatusCode = 404;
            }

            catch (Exception ex)
            {
                logger.Error(ex);
                throw;
            }
        }

        private void StartAdminApi(ClusterConfig clusterConfig)
        {
            var address = clusterConfig.Api?.ListenAddress != null
                ? (clusterConfig.Api.ListenAddress != "*" ? IPAddress.Parse(clusterConfig.Api.ListenAddress) : IPAddress.Any)
                : IPAddress.Parse("127.0.0.1");

            var port = clusterConfig.Api?.AdminPort ?? 4001;

            webHostAdmin = new WebHostBuilder()
                .Configure(app => { app.Run(HandleRequestAdmin); })
                .UseKestrel(options => { options.Listen(address, port); })
                .Build();

            webHostAdmin.Start();

            logger.Info(() => $"Admin API Online @ {address}:{port}");
        }

        #endregion // Admin API

        #region API-Surface

        public void Start(ClusterConfig clusterConfig)
        {
            Contract.RequiresNonNull(clusterConfig, nameof(clusterConfig));
            this.clusterConfig = clusterConfig;

            logger.Info(() => $"Launching ...");
            StartApi(clusterConfig);
            StartAdminApi(clusterConfig);
        }

        #endregion // API-Surface

    }
}
