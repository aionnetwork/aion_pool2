﻿using System;
using System.Numerics;
using System.Text.RegularExpressions;

namespace MiningCore.Blockchain.Aion
{
    public class AionConstants
    {
        public static BigInteger BigMaxValue = BigInteger.Pow(2, 256);
        public const string AionStratumVersion = "AionStratum/1.0.0";
        public static decimal Wei = 1000000000000000000;
        public static readonly Regex ZeroHashPattern = new Regex("^0?x?0+$", RegexOptions.Compiled);
        
    }

	public static class AionCommands
    {
        public const string GetWork = "getblocktemplate";
        public const string SubmitWork = "submitblock";
        public const string Sign = "eth_sign";
        public const string GetNetVersion = "net_version";
        public const string GetClientVersion = "web3_clientVersion";
        public const string GetCoinbase = "eth_coinbase";
        public const string GetAccounts = "eth_accounts";
        public const string GetPeerCount = "net_peerCount";
        public const string ValidateAddress = "validateaddress";
        public const string GetSyncState = "eth_syncing";
        public const string GetBlockByNumber = "eth_getBlockByNumber";
        public const string GetBlockByHash = "eth_getBlockByHash";
        public const string GetTxReceipt = "eth_getTransactionReceipt";
        public const string SendTx = "eth_sendTransaction";
	    public const string UnlockAccount = "personal_unlockAccount";
        public const string GetDifficulty = "getdifficulty";
        public const string GetMinerStats = "getMinerStats";
        public const string Ping = "ping";
        
    }

}
