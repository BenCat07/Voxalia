//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

namespace Voxalia.ServerGame.PluginSystem
{
    public interface ServerPlugin
    {
        /// <summary>
        /// Gets the name of the plugin.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets a list of authors of the plugin.
        /// </summary>
        string[] Authors { get; }

        /// <summary>
        /// Gets a list of plugins that this plugin depends on.
        /// </summary>
        string[] Dependencies { get; }
        
        /// <summary>
        /// Gets a quick, simple description of the plugin, used to help identify it.
        /// </summary>
        string ShortDescription { get; }

        /// <summary>
        /// Gets a URL that can be accessed for support information regarding the plugin.
        /// </summary>
        string HelpLink { get; }

        /// <summary>
        /// Gets the version of the plugin, in standard "x.y.z" version format.
        /// </summary>
        string Version { get; }
        
        /// <summary>
        /// Initially loads the plugin, implemented by the plugin, should be used to register events, commands, or whatever else is needed.
        /// Returns whether the plugin loaded succesfully and should be included.
        /// </summary>
        bool Load(PluginManager manager);

        /// <summary>
        /// Unloads the plugin, should remove any and all events, commands, etc. that are no longer going to be valid when the assembly is removed.
        /// </summary>
        void Unload();
    }
}
