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
        bool HasPermission(params string[] keyPath);
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
        public static bool HasPermissionByPathString(this IPermissible perm, string pstr)
        {
            return perm.HasPermission(pstr.SplitFast('.'));
        }
    }
}
