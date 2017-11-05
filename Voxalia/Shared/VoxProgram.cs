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
using FreneticGameCore.Files;
using Voxalia.Shared.Collision;
using System.Threading;
using System.Globalization;
using System.Reflection;
using FreneticGameCore;

namespace Voxalia.Shared
{
    /// <summary>
    /// Main game program initializer.
    /// </summary>
    public class VoxProgram : Program
    {
        /// <summary>
        /// The name of the game.
        /// </summary>
        public const string VoxGameName = "Voxalia";

        /// <summary>
        /// The version of the game. Automatically read from file.
        /// </summary>
        public static readonly string VoxGameVersion = Assembly.GetCallingAssembly().GetName().Version.ToString();

        /// <summary>
        /// The description of the game version.
        /// </summary>
        public const string VoxGameVersionDescription = "Pre-Alpha";
        
        /// <summary>
        /// Configures the program correctly.
        /// </summary>
        public VoxProgram()
            : base(VoxGameName, VoxGameVersion, VoxGameVersionDescription)
        {
        }

        /// <summary>
        /// The web address for the primary global server that handles logging in.
        /// </summary>
        public const string GlobalServerAddress = "https://forum.freneticllc.com/Account/APIv1";

        /// <summary>
        /// This method should be called FIRST!
        /// </summary>
        public static void PreInitVox()
        {
            PreInit(new VoxProgram());
        }
        
        /// <summary>
        /// Initializes the entire game.
        /// </summary>
        public static void Init()
        {
            FileHandler files = new FileHandler();
            files.Init();
            MaterialHelpers.Populate(files); // TODO: Non-static material helper data?!
            BlockShapeRegistry.Init();
            FullChunkObject.RegisterMe();
        }
    }
}
