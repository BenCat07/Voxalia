//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
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
        /// <para>40 bears no visible advantage to bump up to currently, and would be a fair bit large at 64,000 blocks.</para>
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

        /// <summary>
        /// This value represents the number of chunks in a SLOD chunk set.
        /// Divide a chunk coordinate by this (integer rounded down) to get the SLOD coordinate.
        /// Chosen as 3 as 4 was too many chunks at once, and 2 is too few, based on
        /// pre-render timings.
        /// </summary>
        public const int CHUNKS_PER_SLOD = 3;

        /// <summary>
        /// This value represents the number of blocks wide a SLOD chunk set is.
        /// (90: 30 * 3).
        /// It is calculated as CHUNK_WIDTH (30) multiplied by CHUNKS_PER_SLOD (3).
        /// </summary>
        public const int CHUNK_SLOD_WIDTH = CHUNKS_PER_SLOD * CHUNK_WIDTH;

        /// <summary>
        /// How wide the TOPS data is. Chosen as (3 * CHUNK_WIDTH(30) * 2): 180.
        /// Meaning, 1 set on either side, plus one partially empty set in the center!
        /// </summary>
        public const int TOPS_DATA_WIDTH = 3 * CHUNK_WIDTH * 2;

        /// <summary>
        /// The size of a tops data array: (WIDTH * WIDTH).
        /// </summary>
        public const int TOPS_DATA_SIZE = TOPS_DATA_WIDTH * TOPS_DATA_WIDTH;

        /// <summary>
        /// The count of tops vertices.
        /// ((TOPS_DATA_WIDTH - 1) ^ 2) * 6.
        /// 6 is the number of vertices per valid points.
        /// All points are valid exact the positive most row and column.
        /// </summary>
        public const int TOPS_VERT_COUNT = ((TOPS_DATA_WIDTH - 1) * (TOPS_DATA_WIDTH - 1)) * 6;
    }
}
