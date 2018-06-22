/*

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
using System.Data;
using System.Linq;
using AutoMapper;
using Dapper;
using MiningCore.Configuration;
using MiningCore.Extensions;
using MiningCore.Persistence.Model;
using MiningCore.Persistence.Repositories;
using MiningCore.Util;
using NLog;

namespace MiningCore.Persistence.Postgres.Repositories
{
    public class CoinInfoRepository : ICoinInfoRepository
    {
        public CoinInfoRepository(IMapper mapper)
        {
            this.mapper = mapper;
        }

        private readonly IMapper mapper;
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        public void AddCoinInfo(IDbConnection con, IDbTransaction tx, CoinInfo CoinInfo)
        {
            var existing = GetCoinInfo(con, CoinInfo.CoinType);
            if (existing == null)
            {
                var query = "INSERT INTO coin_info(cointype, name, coinmarketcapid, priceusd, pricebtc, updated) " +
                        "VALUES(@cointype, @name, @coinmarketcapid, @priceusd, @pricebtc, @updated)";

                var coinInfo = new Entities.CoinInfo
                {
                    CoinType = CoinInfo.CoinType.ToString(),
                    Name = CoinInfo.Name,
                    CoinMarketCapId = CoinInfo.CoinMarketCapId,
                    PriceUSD = CoinInfo.PriceUSD,
                    PriceBTC = CoinInfo.PriceBTC,
                    Updated = CoinInfo.Updated
                };

                con.Execute(query, coinInfo, tx);
            }
            else
            {
                var query = "update coin_info set priceusd = @priceusd, pricebtc = @pricebtc, updated = @updated " +
                        " WHERE cointype = @cointype ";
                con.Execute(query, new {
                    priceusd = CoinInfo.PriceUSD,
                    pricebtc = CoinInfo.PriceBTC,
                    updated = CoinInfo.Updated,
                    cointype = CoinInfo.CoinType.ToString()
                }, tx);
            }
        }

        public CoinInfo GetCoinInfo(IDbConnection con, CoinType coinType)
        {
            logger.LogInvoke();

            var query = "SELECT * FROM coin_info WHERE cointype = @coin ";
            var results = con.Query<Entities.CoinInfo>(query, new { coin = coinType.ToString() })
                .Select(mapper.Map<CoinInfo>)
                .ToArray();

            if (results == null || results.Length == 0)
                return null;
            return results[0];
        }
    }
}
