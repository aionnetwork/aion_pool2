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
using System.Collections.Generic;
using Newtonsoft.Json;


namespace MiningCore.Api.Responses
{

    public class CryoptoCompareResponse {
        public double BTC {get; set; }
        public double USD {get; set; }
    }

    public class CoinResponseWrapper
    {
        public CoinData Data { get; set; }
    }

    public class CoinData
    {
        public int Id { get; set; }
        public string Name { get; set; }

        [JsonProperty("symbol")]
        public string CoinType { get; set; }
        public Quotes Quotes { get; set; }
    }

    public class Quotes
    {
        public Quote USD { get; set; }
        public Quote BTC { get; set; }
    }

    public class Quote
    {
        public double Price { get; set; }

    }

    public class ListingData
    {
        [JsonProperty("data")]
        public Listing[] Listings;
    }

    public class Listing
    {
        public int Id { get; set; }
        [JsonProperty("symbol")]
        public string CoinType { get; set; }
    }
}
