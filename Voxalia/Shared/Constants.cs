//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016-2017 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voxalia.Shared
{
    /// <summary>
    /// This class represents any shared global constants for all of the game.
    /// Every constant should be marked with what it represents and why it was chosen at its specific value.
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// This constant represents the width, length, and height of a standard sized chunk - each always has exactly this value as its length on each axis.
        /// <para>Some things that needed considering included: how many chunks would be loaded at once
        /// (which affects how rapidly we can upload chunk render calls to the GPU), and its inverse: how many blocks in a chunk
        /// (which affects how rapidly we can preparse a chunk).</para>
        /// <para>30 was chosen as the best value for this constant, meaning a chunk contains 27,000 blocks.</para>
        /// <para>10 was too small: We'd have thousands of chunks loaded at once, each containing a tiny number of blocks (1000 exactly),
        /// though it is literally exactly ten, a great number for many purposes of calculations related to measurements.</para>
        /// <para>20 was /just slightly/ too small: We'd have a lot of chunks loaded at once still, as well as have only 8,000 blocks per chunk,
        /// though twenty is still a neat number to work with - one fifth of one hundred, a pretty solid choice for many measurement calculations.</para>
        /// <para>25 was an interesting option: it'd smooth out the calculations to a fairly nice spot, and is one fourth of one hundred, so still a nice number.
        /// Unfortunately it is not a multiple of ten, which makes many other usages awkward.</para>
        /// <para>30 was "good enough" for the present situation, with many blocks (27,000), and neatly a multiple of ten.
        /// It is unfortunately not a divisor of 100. It is also not a particularly notable number in any way... but it works nicely.</para>
        /// <para>40 bares no visible advantage to bump up to currently, and would be a fair bit large at 64,000 blocks.</para>
        /// <para>50 and higher would just be way too big at 250,000 blocks.</para>
        /// </summary>
        public const int CHUNK_WIDTH = 30;

        /// <summary>
        /// This value is 27,000. It is exactly equal to CHUNK_WIDTH (30) cubed.
        /// It is not a chosen value but rather one calculated from the CHUNK_WIDTH constant.
        /// This represents how many blocks are in a standard sized chunk.
        /// </summary>
        public const int CHUNK_BLOCK_COUNT = CHUNK_WIDTH * CHUNK_WIDTH * CHUNK_WIDTH;

        /// <summary>
        /// This value represents how many bytes are included in a single block, and is the size of a BlockInternal structure object.
        /// <para>See <see cref="BlockInternal"/>'s details for more information on how this data works out and why this constant was chosen at 5.</para>
        /// </summary>
        public const int BYTES_PER_BLOCK = 5;
    }
}
