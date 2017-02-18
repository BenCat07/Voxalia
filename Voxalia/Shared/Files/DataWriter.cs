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
    public class DataWriter: BinaryWriter
    {
        public DataWriter(Stream stream)
            : base(stream, FileHandler.encoding)
        {
        }

        public void WriteByte(byte x)
        {
            base.Write(x);
        }

        public void WriteInt(int x)
        {
            base.Write(Utilities.IntToBytes(x), 0, 4);
        }

        public void WriteFloat(float x)
        {
            base.Write(Utilities.FloatToBytes(x), 0, 4);
        }

        public void WriteDouble(double x)
        {
            base.Write(Utilities.DoubleToBytes(x), 0, 8);
        }

        public void WriteLong(long x)
        {
            base.Write(Utilities.LongToBytes(x), 0, 8);
        }

        public void WriteBytes(byte[] bits)
        {
            base.Write(bits, 0, bits.Length);
        }

        public void WriteFullBytes(byte[] data)
        {
            WriteInt(data.Length);
            WriteBytes(data);
        }

        public void WriteFullString(string str)
        {
            byte[] data = FileHandler.encoding.GetBytes(str);
            WriteInt(data.Length);
            WriteBytes(data);
        }
    }
}
