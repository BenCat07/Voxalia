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
using System.Threading.Tasks;
using FreneticGameCore;

namespace Voxalia.ServerGame.OtherSystems
{
    /// <summary>
    /// Represents any object that may have permission or not to do something.
    /// </summary>
    public interface IPermissible
    {
        /// <summary>
        /// Returns whether the permissible has a key path.
        /// </summary>
        /// <param name="keyPath">The key path.</param>
        /// <returns>Whether it's permitted.</returns>
        bool? HasPermission(params string[] keyPath);
    }

    /// <summary>
    /// Helpers for permissibles.
    /// </summary>
    public static class PermissbleExtensions
    {
        /// <summary>
        /// Returns whether the permissible has a key path string.
        /// </summary>
        /// <param name="perm">The object.</param>
        /// <param name="pstr">The path string.</param>
        /// <returns>Whether it's permitted.</returns>
        public static bool? HasPermissionByPathString(this IPermissible perm, string pstr)
        {
            return perm.HasPermission(pstr.SplitFast('.'));
        }
    }
}
