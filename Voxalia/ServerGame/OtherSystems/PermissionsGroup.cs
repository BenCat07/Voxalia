using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FreneticDataSyntax;
using FreneticGameCore.Files;
using Voxalia.ServerGame.ServerMainSystem;
using FreneticGameCore;

namespace Voxalia.ServerGame.OtherSystems
{
    /// <summary>
    /// The engine for permissions group handling.
    /// </summary>
    public class PermissionsGroupEngine
    {
        /// <summary>
        /// The backing server.
        /// </summary>
        public Server TheServer;

        /// <summary>
        /// All groups in this engine.
        /// </summary>
        public Dictionary<string, PermissionsGroup> Groups = new Dictionary<string, PermissionsGroup>(128);

        /// <summary>
        /// Gets the permissions group for a name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>The group.</returns>
        public PermissionsGroup GetGroup(string name)
        {
            name = FileHandler.CleanFileName(name);
            if (Groups.TryGetValue(name, out PermissionsGroup grp))
            {
                return grp;
            }
            try
            {
                if (TheServer.Files.Exists("groups/" + name + ".fds"))
                {
                    string dat = TheServer.Files.ReadText("groups/" + name + ".fds");
                    FDSSection sect = new FDSSection(dat);
                    grp = new PermissionsGroup() { Name = name, Root = sect };
                    FDSSection grpint = sect.GetSection("__group_internal__");
                    if (grpint != null)
                    {
                        string inherit = grpint.GetString("inherits");
                        if (inherit != null)
                        {
                            grp.InheritsFrom = GetGroup(inherit);
                        }
                        grp.Priority = grpint.GetDouble("priority", 0).Value;
                    }
                    Groups[name] = grp;
                    return grp;
                }
            }
            catch (Exception ex)
            {
                Utilities.CheckException(ex);
                SysConsole.Output("Handling permissions-group reading", ex);
            }
            return null;
        }
    }

    /// <summary>
    /// Represents a group permission data.
    /// Not meant to be used directly, but rather as an object granted to players.
    /// </summary>
    public class PermissionsGroup : IPermissible
    {
        /// <summary>
        /// Name of the group.
        /// </summary>
        public string Name;

        /// <summary>
        /// Permissions root section.
        /// </summary>
        public FDSSection Root;

        /// <summary>
        /// The group this group inherits from, if any.
        /// </summary>
        public PermissionsGroup InheritsFrom = null;

        /// <summary>
        /// The priority value of this group.
        /// </summary>
        public double Priority = 0;

        /// <summary>
        /// The internal code to check if the player has a permission.
        /// </summary>
        /// <param name="path">The details of the node path.</param>
        /// <returns>Whether the permission is marked.</returns>
        public bool? HasPermission(params string[] path)
        {
            bool? b;
            FDSSection sect = Root;
            int end = path.Length - 1;
            for (int i = 0; i < end; i++)
            {
                b = sect?.GetBool("*");
                if (b.HasValue)
                {
                    return b.Value;
                }
                sect = sect?.GetSection(path[i]);
            }
            b = sect?.GetBool(path[end]);
            if (b.HasValue)
            {
                return b.Value;
            }
            if (InheritsFrom != null)
            {
                return InheritsFrom.HasPermission(path);
            }
            return null;
        }

    }
}
