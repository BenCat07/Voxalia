﻿using System.Collections.Generic;

namespace Voxalia.ClientGame.NetworkSystem
{
    public class NetStringManager
    {
        public List<string> Strings = new List<string>();

        public int IndexForString(string str)
        {
            int i = Strings.IndexOf(str);
            if (i < 0 || i >= Strings.Count)
            {
                Strings.Add(str);
                return Strings.Count - 1;
            }
            else
            {
                return i;
            }
        }

        public string StringForIndex(int ind)
        {
            if (ind < 0 || ind >= Strings.Count)
            {
                return "";
            }
            return Strings[ind];
        }
    }
}
