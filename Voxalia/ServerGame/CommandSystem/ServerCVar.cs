//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using FreneticScript;
using FreneticScript.CommandSystem;
using FreneticGameCore.Files;
using Voxalia.Shared;
using Voxalia.ServerGame.ServerMainSystem;

namespace Voxalia.ServerGame.CommandSystem
{
    /// <summary>
    /// Handles the serverside CVar system.
    /// </summary>
    public class ServerCVar
    {
        /// <summary>
        /// The CVar System the client will use.
        /// </summary>
        public CVarSystem system;
        
        /// <summary>
        /// Prepares the CVar system, generating default CVars.
        /// </summary>
        public void Init(Server tserver, Outputter output)
        {
            system = new CVarSystem(output);
        }

        CVar Register(string name, string value, CVarFlag flags, string desc)
        {
            return system.Register(name, value, flags, desc);
        }
    }
}
