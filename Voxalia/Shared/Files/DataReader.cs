//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016-2017 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System.IO;

namespace Voxalia.Shared.Files
{
    public class DataReader: BinaryReader
    {
        public DataReader(Stream stream)
            : base(stream, FileHandler.encoding)
        {
        }

        public int ReadInt()
        {
            return Utilities.BytesToInt(base.ReadBytes(4));
        }

        public long ReadLong()
        {
            return Utilities.BytesToLong(base.ReadBytes(8));
        }

        public float ReadFloat()
        {
            return Utilities.BytesToFloat(base.ReadBytes(4));
        }

        public string ReadString(int length)
        {
            return FileHandler.encoding.GetString(base.ReadBytes(length));
        }

        public byte[] ReadFullBytes()
        {
            int len = ReadInt();
            return ReadBytes(len);
        }

        public string ReadFullString()
        {
            int len = ReadInt();
            return ReadString(len);
        }
    }
}
