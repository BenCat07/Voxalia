//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System.Collections.Generic;
using System.Text;
using Voxalia.ServerGame.EntitySystem;
using Voxalia.Shared;
using Voxalia.ServerGame.PlayerCommandSystem.CommonCommands;
using Voxalia.ServerGame.PlayerCommandSystem.RegionCommands;
using FreneticScript;

namespace Voxalia.ServerGame.PlayerCommandSystem
{
    public class PlayerCommandEngine
    {
        Dictionary<string, AbstractPlayerCommand> Commands = new Dictionary<string, AbstractPlayerCommand>();

        public PlayerCommandEngine()
        {
            // Common
            Register(new DevelPlayerCommand());
            Register(new DropPlayerCommand());
            Register(new RemotePlayerCommand());
            Register(new SayPlayerCommand());
            Register(new StancePlayerCommand());
            Register(new ThrowPlayerCommand());
            Register(new WeaponreloadPlayerCommand());
            // Region
            Register(new BlockfloodPlayerCommand());
            Register(new BlockshapePlayerCommand());
            Register(new BlockshipPlayerCommand());
        }

        public void Register(AbstractPlayerCommand cmd)
        {
            Commands.Add(cmd.Name, cmd);
        }

        public void Execute(PlayerEntity entity, List<string> arguments, string commandname)
        {
            PlayerCommandEntry entry = new PlayerCommandEntry();
            entry.Player = entity;
            entry.InputArguments = arguments;
            entry.Command = GetCommand(commandname);
            if (entry.Command == null || !entry.Command.Silent)
            {
                StringBuilder args = new StringBuilder();
                for (int i = 0; i < arguments.Count; i++)
                {
                    args.Append(" \"").Append(arguments[i]).Append("\"");
                }
                SysConsole.Output(OutputType.INFO, "Client " + entity + " executing command '" + commandname + "' with arguments:" + args.ToString());
            }
            // TODO: Permission
            // TODO: Fire command event
            if (entry.Command == null)
            {
                entry.Player.SendMessage(TextChannel.COMMAND_RESPONSE, "Unknown command."); // TODO: Noise mode // TODO: Language
            }
            else
            {
                entry.Command.Execute(entry);
            }
        }

        public AbstractPlayerCommand GetCommand(string name)
        {
            AbstractPlayerCommand apc;
            Commands.TryGetValue(name.ToLowerFast(), out apc);
            return apc;
        }
    }
}
