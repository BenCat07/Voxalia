//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System.IO;

namespace Voxalia.Shared.Files
{
    /// <summary>
    /// Wraps a System.IO.MemoryStream.
    /// </summary>
    public class DataStream : MemoryStream
    {
        /// <summary>
        /// Constructs a data stream with bytes pre-loaded.
        /// </summary>
        /// <param name="bytes">The bytes to pre-load.</param>
        public DataStream(byte[] bytes)
            : base(bytes)
        {
        }
        
        /// <summary>
        /// Constructs an empty data stream.
        /// </summary>
        public DataStream()
            : base()
        {
        }

        public DataStream(int capacity)
            : base(capacity)
        {
        }
    }
}
