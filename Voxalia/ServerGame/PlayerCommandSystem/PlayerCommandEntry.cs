//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016-2017 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System.Collections.Generic;
using System.Text;
using Voxalia.ServerGame.EntitySystem;

namespace Voxalia.ServerGame.PlayerCommandSystem
{
    public class PlayerCommandEntry
    {
        public PlayerEntity Player;

        public AbstractPlayerCommand Command;

        public List<string> InputArguments;

        public string AllArguments()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < InputArguments.Count; i++)
            {
                sb.Append(InputArguments[i]);
                if (i + 1 < InputArguments.Count)
                {
                    sb.Append(' ');
                }
            }
            return sb.ToString();
        }
    }
}
