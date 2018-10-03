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
using MiningCore.Contracts;
using MiningCore.Native;

namespace MiningCore.Crypto.Hashing.Algorithms
{
    public unsafe class NeoScrypt : IHashAlgorithm
    {
        public NeoScrypt(uint profile)
        {
            this.profile = profile;
        }

        private readonly uint profile;

        public byte[] Digest(byte[] data, params object[] extra)
        {
            Contract.RequiresNonNull(data, nameof(data));
            Contract.Requires<ArgumentException>(data.Length == 80, $"{nameof(data)} length must be exactly 80 bytes");

            var result = new byte[32];

            fixed (byte* input = data)
            {
                fixed (byte* output = result)
                {
                    LibMultihash.neoscrypt(input, output, (uint)data.Length, profile);
                }
            }

            return result;
        }
    }
}
