namespace MiningCore.Blockchain.Aion.DaemonResponses 
{
    public class GetMinerHashRateResponse 
    {
        public string NetworkHashrate { get; set; }
        public string MinerHashrate { get; set; }
        public double MinerHashrateShare { get; set; }
    }
}