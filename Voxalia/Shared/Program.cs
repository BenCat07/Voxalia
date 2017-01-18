//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for contents of the license.
// If neither of these are not available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Voxalia.Shared.Files;
using Voxalia.Shared.Collision;
using System.Threading;
using System.Globalization;

namespace Voxalia.Shared
{
    /// <summary>
    /// Main game program initializer.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// The name of the game.
        /// </summary>
        public const string GameName = "Voxalia";

        /// <summary>
        /// The version of the game.
        /// </summary>
        public const string GameVersion = "0.1.1";

        /// <summary>
        /// The description of the game version.
        /// </summary>
        public const string GameVersionDescription = "Pre-Alpha";

        /// <summary>
        /// The web address for the primary global server that handles logging in.
        /// </summary>
        public const string GlobalServerAddress = "https://frenetic.xyz/";

        /// <summary>
        /// This method should be called FIRST!
        /// </summary>
        public static void PreInit()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
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
