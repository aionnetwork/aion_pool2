﻿using System;
using System.Numerics;

namespace MiningCore.Blockchain.Aion
{
    public class AionBlockTemplate
    {
	/// <summary>
        /// The block number
        /// </summary>
        public ulong Height { get; set; }

        /// <summary>
        /// current block header pow-hash (32 Bytes)
        /// </summary>
        public string HeaderHash { get; set; }
       
        /// <summary>
        /// the boundary condition ("target"), 2^256 / difficulty. (32 Bytes)
        /// </summary>
        public string Target { get; set; }

        /// <summary>
        /// hash of the parent block.
        /// </summary>
        public string PreviousBlockHash { get; set; }


        /// <summary>
        /// partial hash.
        /// </summary>
        public string PartialHash { get; set; }
    }
}
