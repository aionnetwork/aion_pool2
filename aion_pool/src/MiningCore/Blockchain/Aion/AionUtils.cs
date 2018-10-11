using System.Numerics;
using System.Linq;
using System;
using MiningCore.DaemonInterface;
using MiningCore.Persistence.Postgres.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;

namespace MiningCore.Blockchain.Aion
{
    class AionUtils 
    {
        public static decimal calculateReward(long height) {
            var blockReward = 1497989283243310185;
            var magnitude = 1000000000000000000;
            var rampUpLowerBound = 0;
            var rampUpUpperBound = 259200;
            var rampUpStartValue = 748994641621655092;
            var rampUpEndValue = blockReward;

            var delta = rampUpUpperBound - rampUpLowerBound;
            var m = (rampUpEndValue - rampUpStartValue) / delta;

            if (height <= rampUpUpperBound) {
                return ((decimal) (m * height) + rampUpStartValue) / magnitude;
            } else {
                return (decimal) blockReward / magnitude;
            }
        }

        public static string diffToTarget(double diff)
        {
            BigInteger targetNew = (BigInteger.One << 256);
            targetNew =  targetNew / new BigInteger(diff);
            byte[] tmp = new byte[32];

            byte[] bytes = targetNew.ToByteArray().Reverse().ToArray();
            if (bytes.Length == 32) {
                return bytetoHex(bytes);
            } else {
                int start = bytes[0] == 0 ? 1 : 0;
                int count = bytes.Length - start;
                if (count > 32) {
                    //bug
                } else {
                    Array.Copy(bytes, start, tmp, tmp.Length - count, count);
                }
            }
            
            return bytetoHex(tmp);
        }

        protected static string bytetoHex(byte[] tmp)
        {
            char[] c = new char[tmp.Length * 2];

            byte b;

            for(int bx = 0, cx = 0; bx < tmp.Length; ++bx, ++cx) 
            {
                b = ((byte)(tmp[bx] >> 4));
                c[cx] = (char)(b > 9 ? b + 0x37 + 0x20 : b + 0x30);

                b = ((byte)(tmp[bx] & 0x0F));
                c[++cx]=(char)(b > 9 ? b + 0x37 + 0x20 : b + 0x30);
            }

            return new string(c);
        }
        
        public static double getNetworkDifficulty()
        {
            JsonSerializerSettings serializerSettings = null;
            DaemonClient daemonClient = new DaemonClient(serializerSettings);
            var response = daemonClient.ExecuteStringResponseCmdSingleAsync(AionCommands.GetDifficulty).Result;
            try
            {
                double output = (double)Convert.ToInt32(response, 16);
                return output;
            }
            catch
            {
                Console.WriteLine($"Network Difficulty Cast error: {response}");
            }

            return 0;
        }
       
        
    }
    
    
}