//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for contents of the license.
// If neither of these are not available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

namespace Voxalia.ServerGame.WorldSystem
{
    /// <summary>
    /// Abstractly represents a biome from an unspecified generator.
    /// </summary>
    public abstract class Biome
    {
        /// <summary>
        /// Gets the name of the biome.
        /// </summary>
        /// <returns>The name of the biome.</returns>
        public abstract string GetName();
    }
}
