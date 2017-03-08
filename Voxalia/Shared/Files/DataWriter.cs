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
    public class DataWriter
    {
        public Stream Internal;

        public DataWriter(Stream stream)
        {
            Internal = stream;
        }
        
        public void WriteLocation(Location loc)
        {
            Internal.Write(loc.ToDoubleBytes(), 0, 24);
        }

        public void WriteByte(byte x)
        {
            Internal.WriteByte(x);
        }
        
        public void WriteUShort(ushort x)
        {
            Internal.Write(Utilities.UshortToBytes(x), 0, 2);
        }

        public void WriteInt(int x)
        {
            Internal.Write(Utilities.IntToBytes(x), 0, 4);
        }

        public void WriteFloat(float x)
        {
            Internal.Write(Utilities.FloatToBytes(x), 0, 4);
        }

        public void WriteDouble(double x)
        {
            Internal.Write(Utilities.DoubleToBytes(x), 0, 8);
        }

        public void WriteLong(long x)
        {
            Internal.Write(Utilities.LongToBytes(x), 0, 8);
        }

        public void WriteBytes(byte[] bits)
        {
            Internal.Write(bits, 0, bits.Length);
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
