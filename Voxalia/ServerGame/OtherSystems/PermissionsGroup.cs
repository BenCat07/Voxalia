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
                if (TheServer.Files.Exists("saves/groups/" + name + ".fds"))
                {
                    string dat = TheServer.Files.ReadText("saves/groups/" + name + ".fds");
                    FDSSection sect = new FDSSection(dat);
                    grp = new PermissionsGroup() { Name = name, Root = sect };
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
    public class PermissionsGroup
    {
        /// <summary>
        /// Name of the group.
        /// </summary>
        public string Name;

        /// <summary>
        /// Permissions root section.
        /// </summary>
        public FDSSection Root;
    }
}
