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
using Voxalia.ClientGame.ClientMainSystem;

namespace Voxalia.ClientGame.CommandSystem
{
    /// <summary>
    /// Handles the clientside CVar system.
    /// </summary>
    public class ClientCVar
    {
        /// <summary>
        /// The CVar System the client will use.
        /// </summary>
        public CVarSystem system;
        
        // Game CVars
        public CVar g_timescale, g_firstperson, g_weathermode;

        // Network CVars
        public CVar n_first, n_debugmovement, n_ourvehiclelerp, n_online;

        // Renderer CVars
        public CVar r_fullscreen, r_width, r_height, r_vsync, r_lighting, r_renderwireframe, r_brighttransp,  r_decals,
            r_fov, r_znear, r_renderdist, r_renderdist_2, r_renderdist_2h, r_renderdist_5, r_renderdist_5h, r_renderdist_6, r_renderdist_15,
            r_dof_strength, r_fog, r_maxfps, r_drawself, r_firemode, r_dynamicshadows, r_ssao, r_particles, r_drawchunks, r_drawents,
            r_lightmaxdistance, r_shadowquality, r_shadowblur, r_shadowpace, r_shadows, r_cloudshadows,
            r_good_graphics, r_skybox, r_lensflare, r_blocktexturelinear, r_blocktexturewidth, r_toonify, r_transplighting, r_transpshadows,
            r_3d_enable, r_fast, r_chunksatonce, r_chunkoverrender, r_transpll, r_noblockshapes, r_treeshadows,
            r_godrays, r_hdr, r_clouds, r_motionblur, r_plants, r_exposure, r_grayscale, r_vrscale,
            r_forward_normals, r_forward_lights, r_forward_shadows, r_compute, r_less_clouds, r_extraclouds,
            r_block_mipmaps, r_slodsmoothing, r_modeldistance, r_plantdistance, r_forwardreflections;

        // Audio CVars
        public CVar a_musicvolume, a_musicpitch, a_globalvolume, a_globalpitch, a_music, a_quietondeselect, a_echovolume, a_enforce, a_left, a_right;

        // UI CVars
        public CVar u_mouse_sensitivity, u_reticle, u_reticlescale, u_showhud,
            u_highlight_targetblock, u_highlight_placeblock, u_showping,
            u_debug, u_showmap, u_showrangefinder, u_showcompass,
            u_colortyping, u_rate;
        
        /// <summary>
        /// Prepares the CVar system, generating default CVars.
        /// </summary>
        public void Init(Client tclient, Outputter output)
        {
            // TODO: Organize this mess!
            system = new CVarSystem(output);
            
            // Game CVars
            g_timescale = Register("g_timescale", "1", CVarFlag.Numeric | CVarFlag.ServerControl, "The current game time scale value."); // TODO: Server controllable setting
            g_firstperson = Register("g_firstperson", "true", CVarFlag.Boolean, "Whether to be in FirstPerson view mode.");
            g_weathermode = Register("g_weathermode", "0", CVarFlag.Numeric | CVarFlag.ServerControl, "What weather mode is currently shown. 0 = none, 1 = rain, 2 = snow.");
            // Network CVars
            n_first = Register("n_first", "ipv4", CVarFlag.Textual, "Whether to prefer IPv4 or IPv6.");
            n_debugmovement = Register("n_debugmovement", "false", CVarFlag.Boolean, "Whether to debug movement networking.");
            n_ourvehiclelerp = Register("n_ourvehiclelerp", "0.1", CVarFlag.Numeric, "How strongly to lerp our own vehicle's movement.");
            n_online = Register("n_online", "true", CVarFlag.Boolean, "Whether to only connect to servers with a valid login key. Disable this to play singleplayer without internet.");
            // Renderer CVars
            r_fullscreen = Register("r_fullscreen", "false", CVarFlag.Boolean | CVarFlag.Delayed, "Whether to use fullscreen mode.");
            r_width = Register("r_width", "1280", CVarFlag.Numeric | CVarFlag.Delayed, "What width the window should be.");
            r_height = Register("r_height", "720", CVarFlag.Numeric | CVarFlag.Delayed, "What height the window should be.");
            r_vsync = Register("r_vsync", "false", CVarFlag.Boolean, "Whether to use vertical synchronization mode.");
            r_lighting = Register("r_lighting", "true", CVarFlag.Boolean, "Whether to enable 3D lighting (Otherwise, use FullBright).");
            r_brighttransp = Register("r_brighttransp", "true", CVarFlag.Boolean, "Whether transparent objects should be 'bright' or 'normal'.");
            r_decals = Register("r_decals", "true", CVarFlag.Boolean, "Whether to render decals at all.");
            r_renderwireframe = Register("r_renderwireframe", "false", CVarFlag.Boolean, "Whether to render a wireframe.");
            r_fov = Register("r_fov", "70", CVarFlag.Numeric, "What Field of Vision range value to use.");
            r_znear = Register("r_znear", "0.1", CVarFlag.Numeric, "How close the near plane should be to the camera.");
            r_renderdist = Register("r_renderdist", "3", CVarFlag.Numeric, "The maximum distance, in chunks, a chunk can be from you for it to render.");
            r_renderdist_2 = Register("r_renderdist_2", "1", CVarFlag.Numeric, "Base RenderDist + this = how far a LOD2 chunk can be from you.");
            r_renderdist_2h = Register("r_renderdist_2h", "1", CVarFlag.Numeric, "Base RenderDist + this = how far (Height-wise) a LOD2 chunk can be from you.");
            r_renderdist_5 = Register("r_renderdist_5", "2", CVarFlag.Numeric, "Base RenderDist + this = how far a LOD5 chunk can be from you.");
            r_renderdist_5h = Register("r_renderdist_5h", "2", CVarFlag.Numeric, "Base RenderDist + this = how far (Height-wise) a LOD5 chunk can be from you.");
            r_renderdist_6 = Register("r_renderdist_6", "6", CVarFlag.Numeric, "Base RenderDist + this = how far a LOD6 chunk can be from you.");
            r_renderdist_15 = Register("r_renderdist_15", "10", CVarFlag.Numeric, "Base RenderDist + this = how far a LOD15 chunk can be from you.");
            r_dof_strength = Register("r_dof_strength", "4", CVarFlag.Numeric, "How strong the Depth Of Field effect should be.");
            r_maxfps = Register("r_maxfps", "600", CVarFlag.Numeric | CVarFlag.Delayed, "What the FPS cap should be.");
            r_drawself = Register("r_drawself", "true", CVarFlag.Boolean, "Whether the player entity should be visible.");
            r_firemode = Register("r_firemode", "0", CVarFlag.Numeric, "What mode to display fire as (0 = simple, 1 = dense).");
            r_dynamicshadows = Register("r_dynamicshadows", "true", CVarFlag.Boolean, "Whether dynamic objects should ever cast shadows.");
            r_ssao = Register("r_ssao", "true", CVarFlag.Boolean, "Whether SSAO (Ambient Occlusion) should be used.");
            r_particles = Register("r_particles", "true", CVarFlag.Boolean, "Whether to render any particle effects.");
            r_drawchunks = Register("r_drawchunks", "true", CVarFlag.Boolean, "Whether to draw any chunks at all.");
            r_drawents = Register("r_drawents", "true", CVarFlag.Boolean, "Whether to draw any entities at all.");
            r_lightmaxdistance = Register("r_lightmaxdistance", "35", CVarFlag.Numeric, "How far away a light can be from the camera before it is disabled.");
            r_shadowquality = Register("r_shadowquality", "1024", CVarFlag.Numeric, "What texture size to use for shadow maps.");
            r_shadowblur = Register("r_shadowblur", "0.25", CVarFlag.Numeric, "What factor to use for shadow blurring. Smaller = blurrier.");
            r_shadowpace = Register("r_shadowpace", "1", CVarFlag.Numeric, "How rapidly to rerender shadows, in frames.");
            r_shadows = Register("r_shadows", "false", CVarFlag.Boolean, "Whether to render shadows at all.");
            r_cloudshadows = Register("r_cloudshadows", "false", CVarFlag.Boolean, "Whether to display shadows from clouds.");
            r_good_graphics = Register("r_good_graphics", "true", CVarFlag.Boolean | CVarFlag.Delayed, "Whether to use 'good' graphics."); // TODO: Callback to auto-set
            r_skybox = Register("r_skybox", "default", CVarFlag.ServerControl | CVarFlag.Textual, "What skybox to use."); // TODO: Server controllable setting
            r_blocktexturelinear = Register("r_blocktexturelinear", "true", CVarFlag.Boolean | CVarFlag.Delayed, "Whether block textures are to use a linear blur or nearest-pixel mode.");
            r_blocktexturewidth = Register("r_blocktexturewidth", "256", CVarFlag.Numeric | CVarFlag.Delayed, "What texture width (pixels) block textures should use.");
            r_toonify = Register("r_toonify", "false", CVarFlag.Boolean, "Whether to use a 'toonify' post-processing effect.");
            r_transplighting = Register("r_transplighting", "true", CVarFlag.Boolean, "Whether transparent objects should be lit properly (otherwise, fullbright).");
            r_transpshadows = Register("r_transpshadows", "false", CVarFlag.Boolean, "Whether transparent objects should be lit using HD shadows (Requires r_shadows true).");
            r_3d_enable = Register("r_3d_enable", "false", CVarFlag.Boolean, "Whether to use 3D side-by-side rendering mode.");
            r_fast = Register("r_fast", "true", CVarFlag.Boolean, "Whether to use 'fast' rendering mode.");
            r_chunksatonce = Register("r_chunksatonce", "5", CVarFlag.Numeric, "How many chunks can render at once.");
            r_chunkoverrender = Register("r_chunkoverrender", "true", CVarFlag.Boolean, "Whether to render chunks more often for quality's sake, at risk of performance.");
            r_transpll = Register("r_transpll", "false", CVarFlag.Boolean, "Whether to use GPU linked lists when rendering transparent objects.");
            r_noblockshapes = Register("r_noblockshapes", "false", CVarFlag.Boolean, "Whether block shapes are disabled or not.");
            r_treeshadows = Register("r_treeshadows", "true", CVarFlag.Boolean, "Whether trees cast shadows.");
            r_godrays = Register("r_godrays", "true", CVarFlag.Boolean, "Whether to render GodRays (rays of light from the sun.");
            r_hdr = Register("r_hdr", "true", CVarFlag.Boolean, "Whether to render with high dynamic range adjustments enabled.");
            r_clouds = Register("r_clouds", "true", CVarFlag.Boolean, "Whether to render clouds."); // TODO: Inform the server of this to reduce bandwidth.
            r_motionblur = Register("r_motionblur", "false", CVarFlag.Boolean, "Whether to blur the screen to better represent motion.");
            r_plants = Register("r_plants", "true", CVarFlag.Boolean, "Whether to render small plants around the view.");
            r_exposure = Register("r_exposure", "1.0", CVarFlag.Numeric, "What value to scale the lighting by.");
            r_grayscale = Register("r_grayscale", "false", CVarFlag.Boolean, "Whether to grayscale the view.");
            r_vrscale = Register("r_vrscale", "1", CVarFlag.Numeric, "What scale to put everything at in VR mode.");
            r_compute = Register("r_compute", "true", CVarFlag.Boolean, "Whether to render chunks using the Voxel Compute Shader engine.");
            r_fog = Register("r_fog", "true", CVarFlag.Boolean, "Whether render fog should be enabled at distance.");
            r_forward_normals = Register("r_forward_normals", "true", CVarFlag.Boolean | CVarFlag.Delayed, "Whether forward mode rendering should calculate normal-map effects.");
            r_forward_lights = Register("r_forward_lights", "true", CVarFlag.Boolean | CVarFlag.Delayed, "Whether forward mode rendering should calculate live lighting effects.");
            r_forward_shadows = Register("r_forward_shadows", "false", CVarFlag.Boolean | CVarFlag.Delayed, "Whether forward mode rendering should calculate dynamic shadow effects.");
            r_block_mipmaps = Register("r_block_mipmaps", "true", CVarFlag.Boolean | CVarFlag.Delayed, "Whether mipmaps should be enabled for block textures.");
            r_slodsmoothing = Register("r_slodsmoothing", "false", CVarFlag.Boolean, "Whether to smooth out SLOD chunks.");
            r_modeldistance = Register("r_modeldistance", "60", CVarFlag.Boolean, "How far a static model can be from the camera before it becomes a cardboard cutout.");
            r_plantdistance = Register("r_plantdistance", "50", CVarFlag.Numeric, "How far away plants should be rendered at, maximum.");
            r_less_clouds = Register("r_less_clouds", "true", CVarFlag.Boolean, "Whether to reduce cloud density.");
            r_extraclouds = Register("r_extraclouds", "false", CVarFlag.Boolean, "Whether to render an extra cloud layer (experimental).");
            r_forwardreflections = Register("r_forwardreflections", "true", CVarFlag.Boolean, "Whether to enable forward-mode reflections.");
            // Audio CVars
            a_musicvolume = Register("a_musicvolume", "0.5", CVarFlag.Numeric, "What volume the music should be.");
            a_musicpitch = Register("a_musicpitch", "1", CVarFlag.Numeric, "What pitch the music should be.");
            a_globalvolume = Register("a_globalvolume", "1", CVarFlag.Numeric, "What volume all sounds should be.");
            a_globalpitch = Register("a_globalpitch", "1", CVarFlag.Numeric, "What pitch all sounds should be.");
            a_music = Register("a_music", "music/epic/bcvoxalia_adventure", CVarFlag.Textual | CVarFlag.ServerControl, "What music should be played.");// TODO: Server controllable setting
            a_quietondeselect = Register("a_quietondeselect", "true", CVarFlag.Boolean, "Whether to quiet music when the window is deselected.");
            a_echovolume = Register("a_echovolume", "0", CVarFlag.Numeric, "What volume to echo microphone pickup at, for audio testing purposes. Specify 0 to not listen to the microphone at all.");
            a_enforce = Register("a_enforce", "true", CVarFlag.Boolean, "Whether to use the audio enforcement mode.");
            a_left = Register("a_left", "true", CVarFlag.Boolean, "Whether to play left-channel audio (when audio enforce is enabled).");
            a_right = Register("a_right", "true", CVarFlag.Boolean, "Whether to play right-channel audio (when audio enforce is enabled).");
            // UI CVars
            u_mouse_sensitivity = Register("u_mouse_sensitivity", "1", CVarFlag.Numeric, "How sensitive the mouse is.");
            u_reticle = Register("u_reticle", "1", CVarFlag.Textual, "What reticle to use.");
            u_reticlescale = Register("u_reticlescale", "32", CVarFlag.Numeric, "How big the reticle should be.");
            u_showhud = Register("u_showhud", "true", CVarFlag.Boolean, "Whether to render the HUD.");
            u_highlight_targetblock = Register("u_highlight_targetblock", "true", CVarFlag.Boolean, "Whether to highlight the targeted block.");
            u_highlight_placeblock = Register("u_highlight_placeblock", "true", CVarFlag.Boolean, "Whether to highlight the targeted placement block.");
            u_debug = Register("u_debug", "false", CVarFlag.Boolean, "Whether to display debug information on the HUD.");
            u_showmap = Register("u_showmap", "false", CVarFlag.Boolean | CVarFlag.ServerControl, "Whether to display a map on the HUD.");// TODO: Server controllable setting
            u_showrangefinder = Register("u_showrangefinder", "false", CVarFlag.Boolean | CVarFlag.ServerControl, "Whether to display a range finder on the HUD.");// TODO: Server controllable setting
            u_showping = Register("u_showping", "true", CVarFlag.Boolean, "Whether to display the current ping on the UI.");
            u_showcompass = Register("u_showcompass", "false", CVarFlag.Boolean | CVarFlag.ServerControl, "Whether to display a compass on the HUD.");// TODO: Server controllable setting
            u_colortyping = Register("u_colortyping", "false", CVarFlag.Boolean, "Whether to color the text currently being typed typed (chat, console, ...).");
            u_rate = Register("u_rate", "3", CVarFlag.Numeric, "How many frames between UI updates.");
        }

        CVar Register(string name, string value, CVarFlag flags, string desc = null)
        {
            return system.Register(name, value, flags, desc);
        }
    }
}
