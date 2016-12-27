//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for contents of the license.
// If neither of these are not available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using Voxalia.Shared;
using Voxalia.ClientGame.UISystem;
using Voxalia.ClientGame.GraphicsSystems;
using Voxalia.ClientGame.GraphicsSystems.LightingSystem;
using Voxalia.ClientGame.OtherSystems;
using Voxalia.ClientGame.JointSystem;
using Voxalia.Shared.Collision;
using System.Diagnostics;
using FreneticScript;
using Voxalia.ClientGame.WorldSystem;
using Voxalia.ClientGame.EntitySystem;
using Voxalia.Shared.Files;

namespace Voxalia.ClientGame.ClientMainSystem
{
    public partial class Client
    {
        /// <summary>
        /// Current graphics-delta value.
        /// </summary>
        public double gDelta = 0;

        /// <summary>
        /// A stack of reusable VBOs, for chunks.
        /// TODO: Is tracking this actually helpful?
        /// TODO: Is a ConcurrentStack the best mode here? Perhaps a Queue instead?
        /// </summary>
        public ConcurrentStack<VBO> vbos = new ConcurrentStack<VBO>();
        
        /// <summary>
        /// Gets an estimated Video RAM (graphics memory) usage, broken into component usages.
        /// </summary>
        /// <returns></returns>
        public List<Tuple<string, long>> CalculateVRAMUsage()
        {
            List<Tuple<string, long>> toret = new List<Tuple<string, long>>();
            long modelc = 0;
            foreach (Model model in Models.LoadedModels)
            {
                modelc += model.GetVRAMUsage();
            }
            toret.Add(new Tuple<string, long>("Models", modelc));
            long texturec = 0;
            foreach (Texture texture in Textures.LoadedTextures)
            {
                texturec += texture.Width * texture.Height * 4;
            }
            toret.Add(new Tuple<string, long>("Textures", texturec));
            long blocktexturec = TBlock.TWidth * TBlock.TWidth * 4 * 3;
            for (int i = 0; i < TBlock.Anims.Count; i++)
            {
                blocktexturec += TBlock.TWidth * TBlock.TWidth * 4 * TBlock.Anims[i].FBOs.Length;
            }
            toret.Add(new Tuple<string, long>("BlockTextures", blocktexturec));
            long chunkc = 0;
            foreach (Chunk chunk in TheRegion.LoadedChunks.Values)
            {
                if (chunk._VBO != null)
                {
                    chunkc += chunk._VBO.GetVRAMUsage();
                }
            }
            toret.Add(new Tuple<string, long>("chunks", chunkc));
            // TODO: Maybe also View3D render helper usage?
            return toret;
        }

        /// <summary>
        /// Early startup call to prepare the rendering system.
        /// </summary>
        void PreInitRendering()
        {
            GL.Viewport(0, 0, Window.Width, Window.Height);
            GL.Enable(EnableCap.Texture2D); // TODO: Other texture modes we use as well?
            GL.Enable(EnableCap.Blend);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Front);
        }

        /// <summary>
        /// The absolute maximum distance, in chunks, anything should ever reasonably be.
        /// </summary>
        /// <returns></returns>
        public float MaximumStraightBlockDistance()
        {
            return (CVars.r_renderdist.ValueI + 3) * Chunk.CHUNK_SIZE;
        }

        /// <summary>
        /// The current 'Z-Far' value, IE how far away the client can see, as an absolute limitation.
        /// </summary>
        /// <returns></returns>
        public float ZFar()
        {
            return MaximumStraightBlockDistance() * 2;
        }

        /// <summary>
        /// The rendering subsystem for the primary world view.
        /// This is in some situations temporarily swapped for the currently rendering view as needed.
        /// </summary>
        public View3D MainWorldView = new View3D();

        /// <summary>
        /// Early startup call to preparing some rendering systems.
        /// </summary>
        void InitRendering()
        {
            MainWorldView.CameraModifier = () => Player.GetRelativeQuaternion();
            ShadersCheck();
            View3D.CheckError("Load - Rendering - Shaders");
            generateMapHelpers();
            GenerateGrassHelpers();
            View3D.CheckError("Load - Rendering - Map/Grass");
            MainWorldView.ShadowingAllowed = true;
            MainWorldView.ShadowTexSize = () => CVars.r_shadowquality.ValueI;
            MainWorldView.Render3D = Render3D;
            MainWorldView.PostFirstRender = ReverseEntitiesOrder;
            MainWorldView.LLActive = CVars.r_transpll.ValueB; // TODO: CVar edit call back
            View3D.CheckError("Load - Rendering - Settings");
            MainWorldView.Generate(this, Window.Width, Window.Height);
            View3D.CheckError("Load - Rendering - ViewGen");
            skybox = new VBO[6];
            for (int i = 0; i < 6; i++)
            {
                skybox[i] = new VBO();
                skybox[i].Prepare();
            }
            skybox[0].AddSide(-Location.UnitZ, new TextureCoordinates());
            skybox[1].AddSide(Location.UnitZ, new TextureCoordinates());
            skybox[2].AddSide(-Location.UnitX, new TextureCoordinates());
            skybox[3].AddSide(Location.UnitX, new TextureCoordinates());
            skybox[4].AddSide(-Location.UnitY, new TextureCoordinates());
            skybox[5].AddSide(Location.UnitY, new TextureCoordinates());
            View3D.CheckError("Load - Rendering - VBO Prep");
            for (int i = 0; i < 6; i++)
            {
                skybox[i].GenerateVBO();
            }
            View3D.CheckError("Load - Rendering - Final");
        }

        /// <summary>
        /// Grab all the correct shader objects.
        /// </summary>
        public void ShadersCheck()
        {
            string def = CVars.r_good_graphics.ValueB ? "#MCM_GOOD_GRAPHICS" : "#";
            s_shadow = Shaders.GetShader("shadow" + def);
            s_shadowvox = Shaders.GetShader("shadowvox" + def);
            s_fbo = Shaders.GetShader("fbo" + def);
            s_fbot = Shaders.GetShader("fbo" + def + ",MCM_TRANSP_ALLOWED");
            s_fbov = Shaders.GetShader("fbo_vox" + def);
            s_fbo_refract = Shaders.GetShader("fbo" + def + ",MCM_REFRACT");
            s_fbov_refract = Shaders.GetShader("fbo_vox" + def + ",MCM_REFRACT");
            s_shadowadder = Shaders.GetShader("lightadder" + def + ",MCM_SHADOWS");
            s_lightadder = Shaders.GetShader("lightadder" + def);
            s_transponly = Shaders.GetShader("transponly" + def);
            s_transponlyvox = Shaders.GetShader("transponlyvox" + def);
            s_transponlylit = Shaders.GetShader("transponly" + def + ",MCM_LIT");
            s_transponlyvoxlit = Shaders.GetShader("transponlyvox" + def + ",MCM_LIT");
            s_transponlylitsh = Shaders.GetShader("transponly" + def + ",MCM_LIT,MCM_SHADOWS");
            s_transponlyvoxlitsh = Shaders.GetShader("transponlyvox" + def + ",MCM_LIT,MCM_SHADOWS");
            s_godray = Shaders.GetShader("godray" + def);
            s_mapvox = Shaders.GetShader("map_vox" + def);
            s_transpadder = Shaders.GetShader("transpadder" + def);
            s_finalgodray = Shaders.GetShader("finalgodray" + def);
            s_finalgodray_toonify = Shaders.GetShader("finalgodray" + def + ",MCM_TOONIFY");
            s_finalgodray_lights = Shaders.GetShader("finalgodray" + def + ",MCM_LIGHTS");
            s_finalgodray_lights_toonify = Shaders.GetShader("finalgodray" + def + ",MCM_LIGHTS,MCM_TOONIFY");
            s_finalgodray_lights_motblur = Shaders.GetShader("finalgodray" + def + ",MCM_LIGHTS,MCM_MOTBLUR");
            s_forw = Shaders.GetShader("forward" + def);
            s_forw_vox = Shaders.GetShader("forward" + def + ",MCM_VOX");
            s_forw_trans = Shaders.GetShader("forward" + def + ",MCM_TRANSP");
            s_forw_vox_trans = Shaders.GetShader("forward" + def + ",MCM_VOX,MCM_TRANSP");
            s_transponly_ll = Shaders.GetShader("transponly" + def + ",MCM_LL");
            s_transponlyvox_ll = Shaders.GetShader("transponlyvox" + def + ",MCM_LL");
            s_transponlylit_ll = Shaders.GetShader("transponly" + def + ",MCM_LIT,MCM_LL");
            s_transponlyvoxlit_ll = Shaders.GetShader("transponlyvox" + def + ",MCM_LIT,MCM_LL");
            s_transponlylitsh_ll = Shaders.GetShader("transponly" + def + ",MCM_LIT,MCM_SHADOWS,MCM_LL");
            s_transponlyvoxlitsh_ll = Shaders.GetShader("transponlyvox" + def + ",MCM_LIT,MCM_SHADOWS,MCM_LL");
            s_ll_clearer = Shaders.GetShader("clearer" + def);
            s_ll_fpass = Shaders.GetShader("fpass" + def);
            s_hdrpass = Shaders.GetShader("hdrpass" + def);
            s_forw_grass = Shaders.GetShader("forward" + def + ",MCM_GEOM_ACTIVE?grass");
            s_fbo_grass = Shaders.GetShader("fbo" + def + ",MCM_GEOM_ACTIVE,MCM_PRETTY?grass");
            s_forw_particles = Shaders.GetShader("forward" + def + ",MCM_GEOM_ACTIVE,MCM_TRANSP,MCM_BRIGHT,MCM_NO_ALPHA_CAP?particles");
            s_forwt = Shaders.GetShader("forward" + def + ",MCM_NO_ALPHA_CAP,MCM_BRIGHT");
            s_transponly_particles = Shaders.GetShader("transponly" + def + ",MCM_ANY,MCM_GEOM_ACTIVE,MCM_PRETTY?particles");
            s_transponlylit_particles = Shaders.GetShader("transponly" + def + ",MCM_LIT,MCM_ANY,MCM_GEOM_ACTIVE,MCM_PRETTY?particles");
            s_transponlylitsh_particles = Shaders.GetShader("transponly" + def + ",MCM_LIT,MCM_SHADOWS,MCM_ANY,MCM_GEOM_ACTIVE,MCM_PRETTY?particles");
            s_transponly_ll_particles = Shaders.GetShader("transponly" + def + ",MCM_LL,MCM_ANY,MCM_GEOM_ACTIVE,MCM_PRETTY?particles");
            s_transponlylit_ll_particles = Shaders.GetShader("transponly" + def + ",MCM_LIT,MCM_LL,MCM_ANY,MCM_GEOM_ACTIVE,MCM_PRETTY?particles");
            s_transponlylitsh_ll_particles = Shaders.GetShader("transponly" + def + ",MCM_LIT,MCM_SHADOWS,MCM_LL,MCM_ANY,MCM_GEOM_ACTIVE,MCM_PRETTY?particles");
            // TODO: Better place for models?
            RainCyl = Models.GetModel("raincyl");
            RainCyl.LoadSkin(Textures);
            SnowCyl = Models.GetModel("snowcyl");
            SnowCyl.LoadSkin(Textures);
        }

        /// <summary>
        /// (TEMPORARY) A model for a rain cylinder.
        /// </summary>
        public Model RainCyl;

        /// <summary>
        /// (TEMPORARY) A model for a snow cylinder.
        /// </summary>
        public Model SnowCyl;

        /// <summary>
        /// FBO for the in-game map.
        /// </summary>
        int map_fbo_main = -1;
        
        /// <summary>
        /// Texture for the in-game map.
        /// </summary>
        int map_fbo_texture = -1;
        
        /// <summary>
        /// Depth texture for the in-game map.
        /// </summary>
        int map_fbo_depthtex = -1;

        /// <summary>
        /// Prepare the FBO and associated helpers for the in-game map.
        /// </summary>
        public void generateMapHelpers()
        {
            // TODO: Helper class!
            map_fbo_texture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, map_fbo_texture);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, 256, 256, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero); // TODO: Custom size!
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            map_fbo_depthtex = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, map_fbo_depthtex);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.DepthComponent32, 256, 256, 0, PixelFormat.DepthComponent, PixelType.Float, IntPtr.Zero); // TODO: Custom size!
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            map_fbo_main = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, map_fbo_main);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, map_fbo_texture, 0);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2D, map_fbo_depthtex, 0);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        /// <summary>
        /// Grass texture count. (For the array of grass texture slots).
        /// TODO: Manageable
        /// </summary>
        public const int GRASS_TEX_COUNT = 32;

        /// <summary>
        /// How wide the grass textures should be.
        /// </summary>
        public int GrassTextureWidth = 256;

        /// <summary>
        /// The texture for all grass types.
        /// </summary>
        public int GrassTextureID = -1;

        /// <summary>
        /// The last time any given grass texture was used. Probably not needed.
        /// </summary>
        public double[] GrassLastTexUse = new double[GRASS_TEX_COUNT];

        /// <summary>
        /// A map of grass texture names to their locations in the grass texture array.
        /// </summary>
        public Dictionary<string, int> GrassTextureLocations = new Dictionary<string, int>();

        /// <summary>
        /// Maps material IDs to grass textures. Uses an array instead of an actual map for simplicity.
        /// </summary>
        public int[] GrassMatSet;

        public void GenerateGrassHelpers()
        {
            GrassMatSet = new int[MaterialHelpers.ALL_MATS.Count];
            GrassTextureID = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2DArray, GrassTextureID);
            GL.TexStorage3D(TextureTarget3d.Texture2DArray, 1, SizedInternalFormat.Rgba8, GrassTextureWidth, GrassTextureWidth, GRASS_TEX_COUNT);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            for (int i = 0; i < GRASS_TEX_COUNT; i++)
            {
                GrassLastTexUse[i] = 0;
            }
            GrassTextureLocations.Clear();
            for (int i = 0; i < MaterialHelpers.ALL_MATS.Count; i++)
            {
                string pl = ((Material)i).GetPlant();
                if (pl != null)
                {
                    GrassMatSet[i] = GetGrassTextureID(FileHandler.CleanFileName(pl));
                }
                else
                {
                    GrassMatSet[i] = -1;
                }
            }
        }

        /// <summary>
        /// Gets the grass texture ID for a specific name (may cause it to be loaded!).
        /// Can return 0 if the grass texture array is full!
        /// </summary>
        public int GetGrassTextureID(string f)
        {
            int temp;
            if (GrassTextureLocations.TryGetValue(f, out temp))
            {
                return temp;
            }
            for (int i = 0; i < GRASS_TEX_COUNT; i++)
            {
                if (GrassLastTexUse[i] == 0)
                {
                    GrassLastTexUse[i] = GlobalTickTimeLocal;
                    GrassTextureLocations[f] = i;
                    Textures.LoadTextureIntoArray(f, i, GrassTextureWidth);
                    return i;
                }
            }
            // TODO: Delete any unused entry findable in favor of this new one.
            return 0;
        }

        /// <summary>
        /// The sky box VBO's.
        /// </summary>
        VBO[] skybox;

        /// <summary>
        /// The Shadow Pass shader.
        /// </summary>
        public Shader s_shadow;

        /// <summary>
        /// The Shadow Pass shader, for voxels.
        /// </summary>
        public Shader s_shadowvox;

        /// <summary>
        /// The final write + godrays shader.
        /// </summary>
        public Shader s_finalgodray;

        /// <summary>
        /// The final write + godrays shader, with lights on.
        /// </summary>
        public Shader s_finalgodray_lights;

        /// <summary>
        /// The final write + godrays shader, with toonify on.
        /// </summary>
        public Shader s_finalgodray_toonify;

        /// <summary>
        /// The final write + godrays shader, with lights and toonify on.
        /// </summary>
        public Shader s_finalgodray_lights_toonify;

        /// <summary>
        /// The final write + godrays shader, with lights and motion blur on.
        /// </summary>
        public Shader s_finalgodray_lights_motblur;
        
        /// <summary>
        /// The G-Buffer FBO shader.
        /// </summary>
        public Shader s_fbo;

        /// <summary>
        /// The G-Buffer FBO shader, for voxels.
        /// </summary>
        public Shader s_fbov;

        /// <summary>
        /// The G-Buffer FBO shader, for alltransparents (Skybox mainly).
        /// </summary>
        public Shader s_fbot;

        /// <summary>
        /// The G-Buffer FBO shader, for the refraction pass.
        /// </summary>
        public Shader s_fbo_refract;

        /// <summary>
        /// The G-Buffer FBO shader, for the refraction pass, for voxels.
        /// </summary>
        public Shader s_fbov_refract;

        /// <summary>
        /// The shader that adds shadowed lights to a scene.
        /// </summary>
        public Shader s_shadowadder;

        /// <summary>
        /// The shader that adds lights to a scene.
        /// </summary>
        public Shader s_lightadder;

        /// <summary>
        /// The shader used only for transparent data.
        /// </summary>
        public Shader s_transponly;

        /// <summary>
        /// The shader used only for transparent voxels.
        /// </summary>
        public Shader s_transponlyvox;

        /// <summary>
        /// The shader used only for transparent data with lighting.
        /// </summary>
        public Shader s_transponlylit;

        /// <summary>
        /// The shader used only for transparent voxels with lighting.
        /// </summary>
        public Shader s_transponlyvoxlit;

        /// <summary>
        /// The shader used only for transparent data with shadowed lighting.
        /// </summary>
        public Shader s_transponlylitsh;

        /// <summary>
        /// The shader used only for transparent voxels with shadowed lighting.
        /// </summary>
        public Shader s_transponlyvoxlitsh;

        /// <summary>
        /// The shader used for calculating godrays.
        /// </summary>
        public Shader s_godray;

        /// <summary>
        /// The shader used to calculate the in-game map.
        /// </summary>
        public Shader s_mapvox;

        /// <summary>
        /// The shader used as the final step of adding transparent data to the scene.
        /// TODO: Optimize this away.
        /// </summary>
        public Shader s_transpadder;

        /// <summary>
        /// The shader used for forward ('fast') rendering of data.
        /// </summary>
        public Shader s_forw;

        /// <summary>
        /// The shader used for forward ('fast') rendering of voxels.
        /// </summary>
        public Shader s_forw_vox;

        /// <summary>
        /// The shader used for forward ('fast') rendering of transparent data.
        /// </summary>
        public Shader s_forw_trans;

        /// <summary>
        /// The shader used for forward ('fast') rendering of transparent voxels.
        /// </summary>
        public Shader s_forw_vox_trans;

        /// <summary>
        /// The shader used for transparent data (LinkedList Transparency version).
        /// </summary>
        public Shader s_transponly_ll;

        /// <summary>
        /// The shader used for transparent voxels (LinkedList Transparency version).
        /// </summary>
        public Shader s_transponlyvox_ll;

        /// <summary>
        /// The shader used for lit transparent data (LinkedList Transparency version).
        /// </summary>
        public Shader s_transponlylit_ll;

        /// <summary>
        /// The shader used for lit transparent voxels (LinkedList Transparency version).
        /// </summary>
        public Shader s_transponlyvoxlit_ll;

        /// <summary>
        /// The shader used for shadowed lit transparent data (LinkedList Transparency version).
        /// </summary>
        public Shader s_transponlylitsh_ll;

        /// <summary>
        /// The shader used for shadowed lit transparent voxels (LinkedList Transparency version).
        /// </summary>
        public Shader s_transponlyvoxlitsh_ll;

        /// <summary>
        /// The shader used to clear LL data.
        /// </summary>
        public Shader s_ll_clearer;
        
        /// <summary>
        /// The shader used to finally apply LL data.
        /// </summary>
        public Shader s_ll_fpass;

        /// <summary>
        /// The shader used to assist in HDR calculation acceleration.
        /// </summary>
        public Shader s_hdrpass;

        /// <summary>
        /// The shader used for grass-sprites in forward rendering mode.
        /// </summary>
        public Shader s_forw_grass;

        /// <summary>
        /// The shader used for grass-sprites in deffered rendering mode.
        /// </summary>
        public Shader s_fbo_grass;

        /// <summary>
        /// The shader used for particles in forward rendering mode.
        /// </summary>
        public Shader s_forw_particles;

        /// <summary>
        /// The shader used for alltransparency rendering in forward mode (primarily the skybox).
        /// </summary>
        public Shader s_forwt;

        /// <summary>
        /// The shader used only for transparent particles.
        /// </summary>
        public Shader s_transponly_particles;

        /// <summary>
        /// The shader used only for transparent particles with lighting.
        /// </summary>
        public Shader s_transponlylit_particles;

        /// <summary>
        /// The shader used only for transparent particles with shadowed lighting.
        /// </summary>
        public Shader s_transponlylitsh_particles;

        /// <summary>
        /// The shader used for transparent particles (LinkedList Transparency version).
        /// </summary>
        public Shader s_transponly_ll_particles;

        /// <summary>
        /// The shader used for lit transparent particles (LinkedList Transparency version).
        /// </summary>
        public Shader s_transponlylit_ll_particles;

        /// <summary>
        /// The shader used for shadowed lit transparent particles (LinkedList Transparency version).
        /// </summary>
        public Shader s_transponlylitsh_ll_particles;

        /// <summary>
        /// Sorts all entities by distance to camera.
        /// TODO: Speed analysis? Probably doesn't matter with an average of 'a few hundred' entities...
        /// </summary>
        public void sortEntities()
        {
            TheRegion.Entities = TheRegion.Entities.OrderBy(o => (o.GetPosition().DistanceSquared(MainWorldView.RenderRelative))).ToList();
        }

        /// <summary>
        /// Reverses the order of entities.
        /// </summary>
        public void ReverseEntitiesOrder()
        {
            TheRegion.Entities.Reverse();
        }

        /// <summary>
        /// Helper to calculate true graphics FPS.
        /// </summary>
        public int gTicks = 0;

        /// <summary>
        /// Current true graphics FPS.
        /// </summary>
        public int gFPS = 0;

        /// <summary>
        /// Render ticks since last shadow update.
        /// </summary>
        int rTicks = 1000;

        /// <summary>
        /// Whether shadows should be redrawn this frame.
        /// </summary>
        public bool shouldRedrawShadows = false;

        /// <summary>
        /// The main entry point for the render and tick cycles.
        /// </summary>
        public void Window_RenderFrame(object sender, FrameEventArgs e)
        {
            lock (TickLock)
            {
                gDelta = e.Time;
                gTicks++;
                if (Window.Visible && Window.WindowState != WindowState.Minimized)
                {
                    try
                    {
                        Shaders.ColorMultShader.Bind();
                        GL.Uniform1(6, (float)GlobalTickTimeLocal);
                        if (CVars.r_3d_enable.ValueB || VR != null)
                        {
                            GL.Viewport(Window.Width / 2, 0, Window.Width / 2, Window.Height);
                            CScreen.FullRender(gDelta, 0, 0);
                            UIConsole.Draw();
                            GL.Viewport(0, 0, Window.Width / 2, Window.Height);
                            CScreen.FullRender(gDelta, 0, 0);
                            UIConsole.Draw();
                            GL.Viewport(0, 0, Window.Width, Window.Height);
                        }
                        else
                        {
                            CScreen.FullRender(gDelta, 0, 0);
                            UIConsole.Draw();
                        }
                    }
                    catch (Exception ex)
                    {
                        SysConsole.Output(OutputType.ERROR, "Rendering (general): " + ex.ToString());
                    }
                }
                Stopwatch timer = new Stopwatch();
                try
                {
                    timer.Start();
                    tick(e.Time);
                    timer.Stop();
                    TickTime = (double)timer.ElapsedMilliseconds / 1000f;
                    if (TickTime > TickSpikeTime)
                    {
                        TickSpikeTime = TickTime;
                    }
                    timer.Reset();
                }
                catch (Exception ex)
                {
                    SysConsole.Output(OutputType.ERROR, "Ticking: " + ex.ToString());
                }
                timer.Start();
                View3D.CheckError("Finish");
                if (VR != null)
                {
                    Shaders.ColorMultShader.Bind();
                    GL.UniformMatrix4(1, false, ref MainWorldView.SimpleOrthoMatrix);
                    GL.UniformMatrix4(2, false, ref View3D.IdentityMatrix);
                    GL.BindTexture(TextureTarget.Texture2D, MainWorldView.CurrentFBOTexture);
                    Rendering.RenderRectangle(-1, -1, 1, 1);
                }
                Window.SwapBuffers();
                if (VR != null)
                {
                    VR.Submit();
                }
                timer.Stop();
                FinishTime = (double)timer.ElapsedMilliseconds / 1000f;
                if (FinishTime > FinishSpikeTime)
                {
                    FinishSpikeTime = FinishTime;
                }
                timer.Reset();
            }
        }

        /// <summary>
        /// How long a tick took this frame.
        /// </summary>
        public double TickTime;
        
        /// <summary>
        /// How long GL.Finish took this frame.
        /// </summary>
        public double FinishTime;

        /// <summary>
        /// How long 2D rendering took this frame.
        /// </summary>
        public double TWODTime;

        /// <summary>
        /// How long this entire frame took.
        /// </summary>
        public double TotalTime;

        /// <summary>
        /// How long the longest recent tick took.
        /// </summary>
        public double TickSpikeTime;

        /// <summary>
        /// How long the longest recent Gl.Finsh took.
        /// </summary>
        public double FinishSpikeTime;

        /// <summary>
        /// How long the longest recent 2D rendering took.
        /// </summary>
        public double TWODSpikeTime;

        /// <summary>
        /// How long the longest recent frame took.
        /// </summary>
        public double TotalSpikeTime;

        /// <summary>
        /// The GlobalTickTimeLocal value for when the map was last updated.
        /// </summary>
        public double mapLastRendered = 0;

        /// <summary>
        /// Renders all 2D data to screen.
        /// </summary>
        public void Render2DGame()
        {
            try
            {
                Stopwatch timer = new Stopwatch();
                timer.Start();
                Establish2D();
                if (CVars.r_3d_enable.ValueB || VR != null)
                {
                    GL.Viewport(Window.Width / 2, 0, Window.Width / 2, Window.Height);
                    Render2D(false);
                    GL.Viewport(0, 0, Window.Width / 2, Window.Height);
                    Render2D(false);
                    GL.Viewport(0, 0, Window.Width, Window.Height);
                }
                else
                {
                    Render2D(false);
                }
                timer.Stop();
                TWODTime = (double)timer.ElapsedMilliseconds / 1000f;
                if (TWODTime > TWODSpikeTime)
                {
                    TWODSpikeTime = TWODTime;
                }
                timer.Reset();
            }
            catch (Exception ex)
            {
                SysConsole.Output("Rendering (2D)", ex);
            }
        }

        /// <summary>
        /// Renders the entire game.
        /// </summary>
        public void renderGame()
        {
            Stopwatch totalt = new Stopwatch();
            totalt.Start();
            try
            {
                MainWorldView.ForwardVec = Player.ForwardVector();
                // Frustum cf1 = null;
                if (VR != null)
                {
                    MainWorldView.CameraPos = Player.GetPosition();
                }
                else if (CVars.g_firstperson.ValueB)
                {
                    MainWorldView.CameraPos = PlayerEyePosition;
                }
                else
                {
                    CollisionResult cr = TheRegion.Collision.RayTrace(PlayerEyePosition, PlayerEyePosition - MainWorldView.CalcForward() * Player.ViewBackMod(), IgnorePlayer);
                    if (cr.Hit)
                    {
                        MainWorldView.CameraPos = cr.Position + cr.Normal * 0.05;
                    }
                    else
                    {
                        MainWorldView.CameraPos = cr.Position;
                    }
                }
                if (CVars.u_showmap.ValueB && mapLastRendered + 1.0 < TheRegion.GlobalTickTimeLocal) // TODO: 1.0 -> custom
                {
                    mapLastRendered = TheRegion.GlobalTickTimeLocal;
                    AABB box = new AABB() { Min = Player.GetPosition(), Max = Player.GetPosition() };
                    foreach (Chunk ch in TheRegion.LoadedChunks.Values)
                    {
                        box.Include(ch.WorldPosition.ToLocation() * Chunk.CHUNK_SIZE);
                        box.Include(ch.WorldPosition.ToLocation() * Chunk.CHUNK_SIZE + new Location(Chunk.CHUNK_SIZE));
                    }
                    box.Min -= MainWorldView.RenderRelative;
                    box.Max -= MainWorldView.RenderRelative;
                    Matrix4 ortho = Matrix4.CreateOrthographicOffCenter((float)box.Min.X, (float)box.Max.X, (float)box.Min.Y, (float)box.Max.Y, (float)box.Min.Z, (float)box.Max.Z);
                    Matrix4d oident = Matrix4d.Identity;
                    s_mapvox = s_mapvox.Bind();
                    GL.UniformMatrix4(View3D.MAT_LOC_VIEW, false, ref ortho);
                    MainWorldView.SetMatrix(View3D.MAT_LOC_OBJECT, oident);
                    GL.Viewport(0, 0, 256, 256); // TODO: Customizable!
                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, map_fbo_main);
                    GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
                    GL.ClearBuffer(ClearBuffer.Color, 0, new float[] { 0.0f, 0.2f, 0.1f, 1.0f });
                    GL.ClearBuffer(ClearBuffer.Depth, 0, new float[] { 1.0f });
                    GL.ActiveTexture(TextureUnit.Texture0);
                    GL.BindTexture(TextureTarget.Texture2DArray, TBlock.TextureID);
                    foreach (Chunk chunk in TheRegion.LoadedChunks.Values)
                    {
                        chunk.Render();
                    }
                    // TODO: Render static entities like trees too! Also, an icon for the player's own location! And perhaps that of other players/live entities, if set to render?!
                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                    GL.BindTexture(TextureTarget.Texture2DArray, 0);
                    GL.DrawBuffer(DrawBufferMode.Back);
                }
                sortEntities();
                Particles.Sort();
                Material headMat = TheRegion.GetBlockMaterial(VR == null ? MainWorldView.CameraPos : Player.GetBasicEyePos());
                MainWorldView.FogCol = headMat.GetFogColor();
                float fg = (float)headMat.GetFogAlpha();
                MainWorldView.FogAlpha = (FogEnhanceTime > 0.01) ? Math.Max(fg, (FogEnhanceTime < 1.0 ? (FogEnhanceStrength - ((1.0f - (float)FogEnhanceTime) * FogEnhanceStrength)) : FogEnhanceStrength)) : fg;
                MainWorldView.SunLoc = GetSunLocation();
                MainWorldView.Render();
                ReverseEntitiesOrder();
            }
            catch (Exception ex)
            {
                SysConsole.Output("Rendering (2D)", ex);
            }
            totalt.Stop();
            TotalTime = (double)totalt.ElapsedMilliseconds / 1000f;
            if (TotalTime > TotalSpikeTime)
            {
                TotalSpikeTime = TotalTime;
            }
        }

        /// <summary>
        /// How long the Fog should be 'enhanced', in seconds.
        /// </summary>
        public double FogEnhanceTime = 0;

        /// <summary>
        /// How strongly the Fog should be 'enhanced'.
        /// </summary>
        public float FogEnhanceStrength = 0.3f;

        /// <summary>
        /// How far the night sky is away from the camera.
        /// </summary>
        public float GetSecondSkyDistance()
        {
            return MaximumStraightBlockDistance() * 1.1f;
        }

        /// <summary>
        /// How far the day sky is away from the camera.
        /// </summary>
        public float GetSkyDistance()
        {
            return MaximumStraightBlockDistance();
        }

        /// <summary>
        /// Gets the present 3D location of the sun.
        /// </summary>
        public Location GetSunLocation()
        {
            return MainWorldView.CameraPos + TheSun.Direction * -(GetSkyDistance() * 0.96f);
        }

        /// <summary>
        /// Renders the entire skybox.
        /// </summary>
        public void RenderSkybox()
        {
            float skyAlpha = (float)Math.Max(Math.Min((SunAngle.Pitch - 70.0) / (-90.0), 1.0), 0.06);
            GL.ActiveTexture(TextureUnit.Texture3);
            Textures.Black.Bind();
            GL.ActiveTexture(TextureUnit.Texture2);
            Textures.Black.Bind();
            GL.ActiveTexture(TextureUnit.Texture1);
            Textures.NormalDef.Bind();
            GL.ActiveTexture(TextureUnit.Texture0);
            Rendering.SetMinimumLight(Math.Max(1.6f * skyAlpha, 1.0f)); // TODO: 1.6 -> Externally defined constant. SunLightMod? Also, verify this value is used properly!
            GL.Disable(EnableCap.CullFace);
            Rendering.SetColor(Color4.White);
            Matrix4 scale = Matrix4.CreateScale(GetSecondSkyDistance());
            GL.UniformMatrix4(2, false, ref scale);
            // TODO: Save textures!
            Textures.GetTexture("skies/" + CVars.r_skybox.Value + "_night/bottom").Bind();
            skybox[0].Render(false);
            Textures.GetTexture("skies/" + CVars.r_skybox.Value + "_night/top").Bind();
            skybox[1].Render(false);
            Textures.GetTexture("skies/" + CVars.r_skybox.Value + "_night/xm").Bind();
            skybox[2].Render(false);
            Textures.GetTexture("skies/" + CVars.r_skybox.Value + "_night/xp").Bind();
            skybox[3].Render(false);
            Textures.GetTexture("skies/" + CVars.r_skybox.Value + "_night/ym").Bind();
            skybox[4].Render(false);
            Textures.GetTexture("skies/" + CVars.r_skybox.Value + "_night/yp").Bind();
            skybox[5].Render(false);
            Rendering.SetColor(new Vector4(1, 1, 1, skyAlpha));
            scale = Matrix4.CreateScale(GetSkyDistance());
            GL.UniformMatrix4(2, false, ref scale);
            // TODO: Save textures!
            Textures.GetTexture("skies/" + CVars.r_skybox.Value + "/bottom").Bind();
            skybox[0].Render(false);
            Textures.GetTexture("skies/" + CVars.r_skybox.Value + "/top").Bind();
            skybox[1].Render(false);
            Textures.GetTexture("skies/" + CVars.r_skybox.Value + "/xm").Bind();
            skybox[2].Render(false);
            Textures.GetTexture("skies/" + CVars.r_skybox.Value + "/xp").Bind();
            skybox[3].Render(false);
            Textures.GetTexture("skies/" + CVars.r_skybox.Value + "/ym").Bind();
            skybox[4].Render(false);
            Textures.GetTexture("skies/" + CVars.r_skybox.Value + "/yp").Bind();
            skybox[5].Render(false);
            Rendering.SetColor(new Vector4(ClientUtilities.Convert(Location.One * SunLightModDirect), 1));
            float zf = ZFar();
            float spf = zf * 0.17f;
            Textures.GetTexture("skies/sun").Bind(); // TODO: Store var!
            Matrix4 rot = Matrix4.CreateTranslation(-spf * 0.5f, -spf * 0.5f, 0f)
                * Matrix4.CreateRotationY((float)((-SunAngle.Pitch - 90f) * Utilities.PI180))
                * Matrix4.CreateRotationZ((float)((180f + SunAngle.Yaw) * Utilities.PI180))
                * Matrix4.CreateTranslation(ClientUtilities.Convert(TheSun.Direction * -(GetSkyDistance() * 0.96f)));
            Rendering.RenderRectangle(0, 0, spf, spf, rot); // TODO: Adjust scale based on view rad
            Textures.GetTexture("skies/planet").Bind(); // TODO: Store var!
            float ppf = zf * 0.40f;
            Rendering.SetColor(new Color4(PlanetLight, PlanetLight, PlanetLight, 1));
            rot = Matrix4.CreateTranslation(-ppf * 0.5f, -ppf * 0.5f, 0f)
                * Matrix4.CreateRotationY((float)((-PlanetAngle.Pitch - 90f) * Utilities.PI180))
                * Matrix4.CreateRotationZ((float)((180f + PlanetAngle.Yaw) * Utilities.PI180))
                * Matrix4.CreateTranslation(ClientUtilities.Convert(PlanetDir * -(GetSkyDistance() * 0.8f)));
            Rendering.RenderRectangle(0, 0, ppf, ppf, rot);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.Enable(EnableCap.CullFace);
            Matrix4 ident = Matrix4.Identity;
            GL.UniformMatrix4(2, false, ref ident);
            Rendering.SetColor(Color4.White);
            Rendering.SetMinimumLight(0);
        }

        /// <summary>
        /// Sets the system for 2D rendering.
        /// </summary>
        public void Establish2D()
        {
            GL.Disable(EnableCap.DepthTest);
            Shaders.ColorMultShader.Bind();
            Ortho = Matrix4.CreateOrthographicOffCenter(0, Window.Width, Window.Height, 0, -1, 1);
            GL.UniformMatrix4(1, false, ref Ortho);
        }

        /// <summary>
        /// Whether the system is in "Voxel" rendering mode.
        /// </summary>
        public bool isVox = false;

        /// <summary>
        /// Switch the system to voxel rendering mode.
        /// </summary>
        public void SetVox()
        {
            if (isVox)
            {
                return;
            }
            isVox = true;
            if (MainWorldView.FBOid == FBOID.MAIN)
            {
                s_fbov = s_fbov.Bind();
                GL.BindTexture(TextureTarget.Texture2DArray, TBlock.TextureID);
                GL.ActiveTexture(TextureUnit.Texture2);
                GL.BindTexture(TextureTarget.Texture2DArray, TBlock.NormalTextureID);
                GL.ActiveTexture(TextureUnit.Texture1);
                GL.BindTexture(TextureTarget.Texture2DArray, TBlock.HelpTextureID);
                GL.ActiveTexture(TextureUnit.Texture0);
            }
            if (MainWorldView.FBOid == FBOID.REFRACT)
            {
                s_fbov_refract = s_fbov_refract.Bind();
                GL.BindTexture(TextureTarget.Texture2DArray, TBlock.TextureID);
                GL.ActiveTexture(TextureUnit.Texture2);
                GL.BindTexture(TextureTarget.Texture2DArray, TBlock.NormalTextureID);
                GL.ActiveTexture(TextureUnit.Texture1);
                GL.BindTexture(TextureTarget.Texture2DArray, TBlock.HelpTextureID);
                GL.ActiveTexture(TextureUnit.Texture0);
            }
            else if (MainWorldView.FBOid == FBOID.TRANSP_UNLIT)
            {
                s_transponlyvox = s_transponlyvox.Bind();
                GL.BindTexture(TextureTarget.Texture2DArray, TBlock.TextureID);
                GL.ActiveTexture(TextureUnit.Texture2);
                GL.BindTexture(TextureTarget.Texture2DArray, TBlock.NormalTextureID);
                GL.ActiveTexture(TextureUnit.Texture1);
                GL.BindTexture(TextureTarget.Texture2DArray, TBlock.HelpTextureID);
                GL.ActiveTexture(TextureUnit.Texture0);
            }
            else if (MainWorldView.FBOid == FBOID.TRANSP_LIT)
            {
                s_transponlyvoxlit = s_transponlyvoxlit.Bind();
                GL.BindTexture(TextureTarget.Texture2DArray, TBlock.TextureID);
                GL.ActiveTexture(TextureUnit.Texture2);
                GL.BindTexture(TextureTarget.Texture2DArray, TBlock.NormalTextureID);
                GL.ActiveTexture(TextureUnit.Texture1);
                GL.BindTexture(TextureTarget.Texture2DArray, TBlock.HelpTextureID);
                GL.ActiveTexture(TextureUnit.Texture0);
            }
            else if (MainWorldView.FBOid == FBOID.TRANSP_SHADOWS)
            {
                s_transponlyvoxlitsh = s_transponlyvoxlitsh.Bind();
                GL.BindTexture(TextureTarget.Texture2DArray, TBlock.TextureID);
                GL.ActiveTexture(TextureUnit.Texture2);
                GL.BindTexture(TextureTarget.Texture2DArray, TBlock.NormalTextureID);
                GL.ActiveTexture(TextureUnit.Texture1);
                GL.BindTexture(TextureTarget.Texture2DArray, TBlock.HelpTextureID);
                GL.ActiveTexture(TextureUnit.Texture0);
            }
            else if (MainWorldView.FBOid == FBOID.TRANSP_LL)
            {
                s_transponlyvox_ll = s_transponlyvox_ll.Bind();
                GL.BindTexture(TextureTarget.Texture2DArray, TBlock.TextureID);
                GL.ActiveTexture(TextureUnit.Texture2);
                GL.BindTexture(TextureTarget.Texture2DArray, TBlock.NormalTextureID);
                GL.ActiveTexture(TextureUnit.Texture1);
                GL.BindTexture(TextureTarget.Texture2DArray, TBlock.HelpTextureID);
                GL.ActiveTexture(TextureUnit.Texture0);
            }
            else if (MainWorldView.FBOid == FBOID.TRANSP_LIT_LL)
            {
                s_transponlyvoxlit_ll = s_transponlyvoxlit_ll.Bind();
                GL.BindTexture(TextureTarget.Texture2DArray, TBlock.TextureID);
                GL.ActiveTexture(TextureUnit.Texture2);
                GL.BindTexture(TextureTarget.Texture2DArray, TBlock.NormalTextureID);
                GL.ActiveTexture(TextureUnit.Texture1);
                GL.BindTexture(TextureTarget.Texture2DArray, TBlock.HelpTextureID);
                GL.ActiveTexture(TextureUnit.Texture0);
            }
            else if (MainWorldView.FBOid == FBOID.TRANSP_SHADOWS_LL)
            {
                s_transponlyvoxlitsh_ll = s_transponlyvoxlitsh_ll.Bind();
                GL.BindTexture(TextureTarget.Texture2DArray, TBlock.TextureID);
                GL.ActiveTexture(TextureUnit.Texture2);
                GL.BindTexture(TextureTarget.Texture2DArray, TBlock.NormalTextureID);
                GL.ActiveTexture(TextureUnit.Texture1);
                GL.BindTexture(TextureTarget.Texture2DArray, TBlock.HelpTextureID);
                GL.ActiveTexture(TextureUnit.Texture0);
            }
            else if (MainWorldView.FBOid == FBOID.FORWARD_SOLID)
            {
                s_forw_vox = s_forw_vox.Bind();
                GL.BindTexture(TextureTarget.Texture2DArray, TBlock.TextureID);
            }
            else if (MainWorldView.FBOid == FBOID.FORWARD_TRANSP)
            {
                s_forw_vox_trans = s_forw_vox_trans.Bind();
                GL.BindTexture(TextureTarget.Texture2DArray, TBlock.TextureID);
            }
            else if (MainWorldView.FBOid == FBOID.SHADOWS)
            {
                s_shadowvox = s_shadowvox.Bind();
                GL.BindTexture(TextureTarget.Texture2DArray, TBlock.TextureID);
            }
            if (FixPersp != Matrix4.Identity)
            {
                GL.UniformMatrix4(View3D.MAT_LOC_VIEW, false, ref FixPersp);
            }
        }

        /// <summary>
        /// Switch the system to entity rendering mode.
        /// </summary>
        public void SetEnts()
        {
            if (!isVox)
            {
                return;
            }
            isVox = false;
            if (MainWorldView.FBOid == FBOID.MAIN)
            {
                GL.ActiveTexture(TextureUnit.Texture2);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.ActiveTexture(TextureUnit.Texture1);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                s_fbo = s_fbo.Bind();
            }
            else if (MainWorldView.FBOid == FBOID.REFRACT)
            {
                GL.ActiveTexture(TextureUnit.Texture2);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.ActiveTexture(TextureUnit.Texture1);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                s_fbo_refract = s_fbo_refract.Bind();
            }
            else if (MainWorldView.FBOid == FBOID.TRANSP_UNLIT)
            {
                GL.ActiveTexture(TextureUnit.Texture2);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.ActiveTexture(TextureUnit.Texture1);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                s_transponly = s_transponly.Bind();
            }
            else if (MainWorldView.FBOid == FBOID.TRANSP_LIT)
            {
                GL.ActiveTexture(TextureUnit.Texture2);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.ActiveTexture(TextureUnit.Texture1);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                s_transponlylit = s_transponlylit.Bind();
            }
            else if (MainWorldView.FBOid == FBOID.TRANSP_SHADOWS)
            {
                GL.ActiveTexture(TextureUnit.Texture2);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.ActiveTexture(TextureUnit.Texture1);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                s_transponlylitsh = s_transponlylitsh.Bind();
            }
            else if (MainWorldView.FBOid == FBOID.TRANSP_LL)
            {
                GL.ActiveTexture(TextureUnit.Texture2);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.ActiveTexture(TextureUnit.Texture1);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                s_transponly_ll = s_transponly_ll.Bind();
            }
            else if (MainWorldView.FBOid == FBOID.TRANSP_LIT_LL)
            {
                GL.ActiveTexture(TextureUnit.Texture2);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.ActiveTexture(TextureUnit.Texture1);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                s_transponlylit_ll = s_transponlylit_ll.Bind();
            }
            else if (MainWorldView.FBOid == FBOID.TRANSP_SHADOWS_LL)
            {
                GL.ActiveTexture(TextureUnit.Texture2);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.ActiveTexture(TextureUnit.Texture1);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                s_transponlylitsh_ll = s_transponlylitsh_ll.Bind();
            }
            else if (MainWorldView.FBOid == FBOID.FORWARD_SOLID)
            {
                s_forw = s_forw.Bind();
                GL.BindTexture(TextureTarget.Texture2DArray, 0);
            }
            else if (MainWorldView.FBOid == FBOID.FORWARD_TRANSP)
            {
                s_forw_trans = s_forw_trans.Bind();
                GL.BindTexture(TextureTarget.Texture2DArray, 0);
            }
            else if (MainWorldView.FBOid == FBOID.SHADOWS)
            {
                GL.BindTexture(TextureTarget.Texture2DArray, 0);
                s_shadow = s_shadow.Bind();
            }
            if (FixPersp != Matrix4.Identity)
            {
                GL.UniformMatrix4(View3D.MAT_LOC_VIEW, false, ref FixPersp);
            }
        }

        /// <summary>
        /// The up/down position of the rain cylinder.
        /// </summary>
        public double RainCylPos = 0;

        /// <summary>
        /// Renders the VR helpers (Controllers in particular).
        /// </summary>
        public void RenderVR()
        {
            if (VR == null)
            {
                return;
            }
            SetEnts();
            Textures.White.Bind();
            Rendering.SetMinimumLight(1);
            Model tmod = Models.GetModel("vr/controller/vive"); // TODO: Store the model in a var somewhere?
            VBO mmcircle = tmod.MeshFor("circle").vbo;
            tmod.LoadSkin(Textures);
            // TODO: Special dynamic controller models!
            if (VR.Left != null)
            {
                Matrix4 pos = Matrix4.CreateScale(1.5f) * VR.Left.Position;
                VR.LeftTexture.CalcTexture(VR.Left, GlobalTickTimeLocal);
                isVox = true;
                SetEnts();
                mmcircle.Tex = new Texture() { Internal_Texture = VR.LeftTexture.Texture, Engine = Textures };
                GL.UniformMatrix4(2, false, ref pos);
                tmod.Draw();
            }
            if (VR.Right != null)
            {
                Matrix4 pos = Matrix4.CreateScale(1.5f) * VR.Right.Position;
                VR.RightTexture.CalcTexture(VR.Right, GlobalTickTimeLocal);
                isVox = true;
                SetEnts();
                mmcircle.Tex = new Texture() { Internal_Texture = VR.RightTexture.Texture, Engine = Textures };
                GL.UniformMatrix4(2, false, ref pos);
                tmod.Draw();
            }
        }

        /// <summary>
        /// Renders the 3D world upon instruction from the internal view render code.
        /// </summary>
        public void Render3D(View3D view)
        {
            GL.Enable(EnableCap.CullFace);
            if (view.ShadowsOnly)
            {
                for (int i = 0; i < TheRegion.ShadowCasters.Count; i++)
                {
                    TheRegion.ShadowCasters[i].Render();
                }
            }
            else
            {
                SetEnts();
                GL.ActiveTexture(TextureUnit.Texture1);
                Textures.NormalDef.Bind();
                GL.ActiveTexture(TextureUnit.Texture0);
                if (view.FBOid == FBOID.MAIN)
                {
                    s_fbot.Bind();
                    RenderSkybox();
                    s_fbo.Bind();
                }
                if (view.FBOid == FBOID.FORWARD_SOLID)
                {
                    s_forwt.Bind();
                    RenderSkybox();
                    s_forw.Bind();
                }
                SetEnts();
                for (int i = 0; i < TheRegion.Entities.Count; i++)
                {
                    TheRegion.Entities[i].Render();
                }
                SetEnts();
                if (CVars.g_weathermode.ValueI > 0)
                {
                    RainCylPos += gDelta * ((CVars.g_weathermode.ValueI == 1) ? 0.5 : 0.1);
                    while (RainCylPos > 1.0)
                    {
                        RainCylPos -= 1.0;
                    }
                    Matrix4d rot = (CVars.g_weathermode.ValueI == 2) ? Matrix4d.CreateRotationZ(Math.Sin(RainCylPos * 2f * Math.PI) * 0.1f) : Matrix4d.Identity;
                    for (int i = -10; i <= 10; i++)
                    {
                        Matrix4d mat = rot * Matrix4d.CreateTranslation(ClientUtilities.ConvertD(MainWorldView.CameraPos + new Location(0, 0, 4 * i + RainCylPos * -4)));
                        MainWorldView.SetMatrix(2, mat);
                        if (CVars.g_weathermode.ValueI == 1)
                        {
                            RainCyl.Draw();
                        }
                        else if (CVars.g_weathermode.ValueI == 2)
                        {
                            SnowCyl.Draw();
                        }
                    }
                }
                if (MainWorldView.FBOid == FBOID.MAIN)
                {
                    Rendering.SetMinimumLight(1f);
                }
                GL.ActiveTexture(TextureUnit.Texture1);
                Textures.NormalDef.Bind();
                GL.ActiveTexture(TextureUnit.Texture0);
                Particles.Engine.Render();
            }
            SetEnts();
            isVox = false;
            SetVox();
            TheRegion.Render();
            SetEnts();
            TheRegion.RenderPlants();
            if (!view.ShadowsOnly)
            {
                GL.ActiveTexture(TextureUnit.Texture1);
                Textures.NormalDef.Bind();
                GL.ActiveTexture(TextureUnit.Texture0);
            }
            Textures.White.Bind();
            Location itemSource = Player.ItemSource();
            Location mov = (CameraFinalTarget - itemSource) / CameraDistance;
            Location cpos = CameraFinalTarget - (CameraImpactNormal * 0.01f);
            Location cpos2 = CameraFinalTarget + (CameraImpactNormal * 0.91f);
            // TODO: 5 -> Variable length (Server controlled?)
            if (TheRegion.GetBlockMaterial(cpos) != Material.AIR && CameraDistance < 5)
            {
                if (CVars.u_highlight_targetblock.ValueB)
                {
                    Location cft = cpos.GetBlockLocation();
                    GL.LineWidth(3);
                    Rendering.SetColor(Color4.Blue);
                    Rendering.SetMinimumLight(1.0f);
                    Rendering.RenderLineBox(cft - mov * 0.01f, cft + Location.One - mov * 0.01f);
                    GL.LineWidth(1);
                    if (VR != null && VR.Right != null)
                    {
                        Rendering.SetColor(Color4.Red);
                        Rendering.RenderLine(itemSource, CameraFinalTarget);
                    }
                }
                if (CVars.u_highlight_placeblock.ValueB)
                {
                    Rendering.SetColor(Color4.Cyan);
                    Location cft2 = cpos2.GetBlockLocation();
                    Rendering.RenderLineBox(cft2, cft2 + Location.One);
                }
                Rendering.SetColor(Color4.White);
            }
            if (MainWorldView.FBOid == FBOID.MAIN)
            {
                Rendering.SetMinimumLight(0f);
            }
            if (CVars.n_debugmovement.ValueB)
            {
                Rendering.SetColor(Color4.Red);
                GL.LineWidth(5);
                foreach (Chunk chunk in TheRegion.LoadedChunks.Values)
                {
                    if (chunk._VBO == null && !chunk.IsAir)
                    {
                        Rendering.RenderLineBox(chunk.WorldPosition.ToLocation() * Chunk.CHUNK_SIZE, (chunk.WorldPosition.ToLocation() + Location.One) * Chunk.CHUNK_SIZE);
                    }
                }
                GL.LineWidth(1);
                Rendering.SetColor(Color4.White);
            }
            RenderVR();
            Textures.White.Bind();
            Rendering.SetMinimumLight(1);
            TheRegion.RenderEffects();
            Textures.GetTexture("effects/beam").Bind(); // TODO: Store
            for (int i = 0; i < TheRegion.Joints.Count; i++)
            {
                if (TheRegion.Joints[i] is ConnectorBeam)
                {
                    switch (((ConnectorBeam)TheRegion.Joints[i]).type)
                    {
                        case BeamType.STRAIGHT:
                            {
                                Location one = TheRegion.Joints[i].One.GetPosition();
                                if (TheRegion.Joints[i].One is CharacterEntity)
                                {
                                    one = ((CharacterEntity)TheRegion.Joints[i].One).GetEyePosition() + new Location(0, 0, -0.3);
                                }
                                Location two = TheRegion.Joints[i].Two.GetPosition();
                                Vector4 col = Rendering.AdaptColor(ClientUtilities.ConvertD((one + two) * 0.5), ((ConnectorBeam)TheRegion.Joints[i]).color);
                                Rendering.SetColor(col);
                                Rendering.RenderLine(one, two);
                            }
                            break;
                        case BeamType.CURVE:
                            {
                                Location one = TheRegion.Joints[i].One.GetPosition();
                                Location two = TheRegion.Joints[i].Two.GetPosition();
                                Location cPoint = (one + two) * 0.5f;
                                if (TheRegion.Joints[i].One is CharacterEntity)
                                {
                                    one = ((CharacterEntity)TheRegion.Joints[i].One).GetEyePosition() + new Location(0, 0, -0.3);
                                    cPoint = one + ((CharacterEntity)TheRegion.Joints[i].One).ForwardVector() * (two - one).Length();
                                }
                                DrawCurve(one, two, cPoint, ((ConnectorBeam)TheRegion.Joints[i]).color);
                            }
                            break;
                        case BeamType.MULTICURVE:
                            {
                                Location one = TheRegion.Joints[i].One.GetPosition();
                                Location two = TheRegion.Joints[i].Two.GetPosition();
                                double forlen = 1;
                                Location forw = Location.UnitZ;
                                if (TheRegion.Joints[i].One is CharacterEntity)
                                {
                                    one = ((CharacterEntity)TheRegion.Joints[i].One).GetEyePosition() + new Location(0, 0, -0.3);
                                    forlen = (two - one).Length();
                                    forw = ((CharacterEntity)TheRegion.Joints[i].One).ForwardVector();
                                }
                                Location spos = one + forw * forlen;
                                const int curves = 5;
                                BEPUutilities.Vector3 bvec = new BEPUutilities.Vector3(0, 0, 1);
                                BEPUutilities.Vector3 bvec2 = new BEPUutilities.Vector3(1, 0, 0);
                                BEPUutilities.Quaternion bquat;
                                BEPUutilities.Quaternion.GetQuaternionBetweenNormalizedVectors(ref bvec2, ref bvec, out bquat);
                                BEPUutilities.Vector3 forwvec = forw.ToBVector();
                                GL.LineWidth(6);
                                DrawCurve(one, two, spos, ((ConnectorBeam)TheRegion.Joints[i]).color);
                                for (int c = 0; c < curves; c++)
                                {
                                    double tang = TheRegion.GlobalTickTimeLocal + Math.PI * 2.0 * ((double)c / (double)curves);
                                    BEPUutilities.Vector3 res = BEPUutilities.Quaternion.Transform(forw.ToBVector(), bquat);
                                    BEPUutilities.Quaternion quat = BEPUutilities.Quaternion.CreateFromAxisAngle(forwvec, (float)(tang % (Math.PI * 2.0)));
                                    res = BEPUutilities.Quaternion.Transform(res, quat);
                                    res = res * (float)(0.1 * forlen);
                                    DrawCurve(one, two, spos + new Location(res), ((ConnectorBeam)TheRegion.Joints[i]).color);
                                }
                            }
                            break;
                    }
                }
            }
            Rendering.SetColor(Color4.White);
            Rendering.SetMinimumLight(0);
            Textures.White.Bind();
            if (!view.ShadowsOnly)
            {
                GL.ActiveTexture(TextureUnit.Texture1);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.ActiveTexture(TextureUnit.Texture0);
                Render2D(true);
            }
        }

        /// <summary>
        /// Draws a curved line in 3D space.
        /// </summary>
        void DrawCurve(Location one, Location two, Location cPoint, System.Drawing.Color color)
        {
            const int curvePoints = 10;
            const double step = 1.0 / curvePoints;
            Location curvePos = one;
            for (double t = step; t <= 1.0; t += step)
            {
                Vector4 col = Rendering.AdaptColor(ClientUtilities.Convert(cPoint), color);
                Rendering.SetColor(col);
                Location c2 = CalculateBezierPoint(t, one, cPoint, two);
                Rendering.RenderBilboardLine(curvePos, c2, 3, MainWorldView.CameraPos);
                curvePos = c2;
            }
        }

        /// <summary>
        /// Calculates a Bezier point along a curve.
        /// </summary>
        Location CalculateBezierPoint(double t, Location p0, Location p1, Location p2)
        {
            double u = 1 - t;
            return (u * u) * p0 + 2 * u * t * p1 + t * t * p2;
        }

        /// <summary>
        /// Whether textures should be rendered currently.
        /// </summary>
        public bool RenderTextures = true;

        /// <summary>
        /// How long extra items should be rendered for.
        /// </summary>
        public double RenderExtraItems = 0;

        /// <summary>
        /// The format for MS timestamps.
        /// </summary>
        const string timeformat = "#.000";

        /// <summary>
        /// The format for health data.
        /// </summary>
        const string healthformat = "0.0";

        /// <summary>
        /// The format for the ping display.
        /// </summary>
        const string pingformat = "000";

        /// <summary>
        /// A matrix that can be used to fix the perspective automatically (Identity when unused).
        /// </summary>
        public Matrix4 FixPersp = Matrix4.Identity;

        /// <summary>
        /// How high the HUD UI is from the bottom.
        /// </summary>
        public int UIBottomHeight = (itemScale * 2 + bottomup) + itemScale * 2;

        /// <summary>
        /// How big an item is on-screen.
        /// </summary>
        const int itemScale = 48;

        /// <summary>
        /// How far from the bottom to start things at.
        /// </summary>
        const int bottomup = 32 + 32;

        /// <summary>
        /// Renders the 2D screen data, or 3D pieces of the 2D screen.
        /// </summary>
        public void Render2D(bool sub3d)
        {
            if (sub3d)
            {
                //GL.Disable(EnableCap.DepthTest);
                FixPersp = Matrix4.CreateOrthographicOffCenter(0, Window.Width, Window.Height, 0, -(itemScale * 2), 1000);
                isVox = false;
                SetVox();
            }
            GL.Disable(EnableCap.CullFace);
            if (CVars.u_showhud.ValueB && !InvShown())
            {
                if (!sub3d && CVars.u_showping.ValueB)
                {
                    string pingdetail = "^0^e^&ping: " + (Math.Max(LastPingValue, GlobalTickTimeLocal - LastPingTime) * 1000.0).ToString(pingformat) + "ms";
                    string pingdet2 = "^0^e^&average: " + (APing * 1000.0).ToString(pingformat) + "ms";
                    FontSets.Standard.DrawColoredText(pingdetail, new Location(Window.Width - FontSets.Standard.MeasureFancyText(pingdetail), Window.Height - FontSets.Standard.font_default.Height * 2, 0));
                    FontSets.Standard.DrawColoredText(pingdet2, new Location(Window.Width - FontSets.Standard.MeasureFancyText(pingdet2), Window.Height - FontSets.Standard.font_default.Height, 0));
                }
                if (!sub3d && CVars.u_debug.ValueB)
                {
                    FontSets.Standard.DrawColoredText(FontSets.Standard.SplitAppropriately("^!^e^7gFPS(calc): " + (1f / gDelta) + ", gFPS(actual): " + gFPS
                        + "\nHeld Item: " + GetItemForSlot(QuickBarPos).ToString()
                        + "\nTimes -> Physics: " + TheRegion.PhysTime.ToString(timeformat) + ", Shadows: " + MainWorldView.ShadowTime.ToString(timeformat)
                        + ", FBO: " + MainWorldView.FBOTime.ToString(timeformat) + ", Lights: " + MainWorldView.LightsTime.ToString(timeformat) + ", 2D: " + TWODTime.ToString(timeformat)
                        + ", Tick: " + TickTime.ToString(timeformat) + ", Finish: " + FinishTime.ToString(timeformat) + ", Total: " + TotalTime.ToString(timeformat)
                        + "\nSpike Times -> Shadows: " + MainWorldView.ShadowSpikeTime.ToString(timeformat)
                        + ", FBO: " + MainWorldView.FBOSpikeTime.ToString(timeformat) + ", Lights: " + MainWorldView.LightsSpikeTime.ToString(timeformat) + ", 2D: " + TWODSpikeTime.ToString(timeformat)
                        + ", Tick: " + TickSpikeTime.ToString(timeformat) + ", Finish: " + FinishSpikeTime.ToString(timeformat) + ", Total: " + TotalSpikeTime.ToString(timeformat)
                        + "\nChunks loaded: " + TheRegion.LoadedChunks.Count + ", Chunks rendering currently: " + TheRegion.RenderingNow.Count + ", chunks waiting: " + TheRegion.NeedsRendering.Count + ", Entities loaded: " + TheRegion.Entities.Count
                        + "\nChunks prepping currently: " + TheRegion.PreppingNow.Count + ", chunks waiting for prep: " + TheRegion.PrepChunks.Count
                        + "\nPosition: " + Player.GetPosition().ToBasicString() + ", velocity: " + Player.GetVelocity().ToBasicString() + ", direction: " + Player.Direction.ToBasicString()
                        + "\nExposure: " + MainWorldView.MainEXP,
                        Window.Width - 10), new Location(0, 0, 0), Window.Height, 1, false, "^r^!^e^7");
                }
                int center = Window.Width / 2;
                if (RenderExtraItems > 0)
                {
                    RenderExtraItems -= gDelta;
                    if (RenderExtraItems < 0)
                    {
                        RenderExtraItems = 0;
                    }
                    RenderItem(GetItemForSlot(QuickBarPos - 5), new Location(center - (itemScale + itemScale + itemScale + itemScale + itemScale + itemScale + 3), Window.Height - (itemScale + 16 + bottomup), 0), itemScale, sub3d);
                    RenderItem(GetItemForSlot(QuickBarPos - 4), new Location(center - (itemScale + itemScale + itemScale + itemScale + itemScale + 3), Window.Height - (itemScale + 16 + bottomup), 0), itemScale, sub3d);
                    RenderItem(GetItemForSlot(QuickBarPos - 3), new Location(center - (itemScale + itemScale + itemScale + itemScale + 3), Window.Height - (itemScale + 16 + bottomup), 0), itemScale, sub3d);
                    RenderItem(GetItemForSlot(QuickBarPos + 3), new Location(center + (itemScale + itemScale + itemScale + 2), Window.Height - (itemScale + 16 + bottomup), 0), itemScale, sub3d);
                    RenderItem(GetItemForSlot(QuickBarPos + 4), new Location(center + (itemScale + itemScale + itemScale + itemScale + 2), Window.Height - (itemScale + 16 + bottomup), 0), itemScale, sub3d);
                    RenderItem(GetItemForSlot(QuickBarPos + 5), new Location(center + (itemScale + itemScale + itemScale + itemScale + itemScale + 2), Window.Height - (itemScale + 16 + bottomup), 0), itemScale, sub3d);
                }
                RenderItem(GetItemForSlot(QuickBarPos - 2), new Location(center - (itemScale + itemScale + itemScale + 3), Window.Height - (itemScale + 16 + bottomup), 0), itemScale, sub3d);
                RenderItem(GetItemForSlot(QuickBarPos - 1), new Location(center - (itemScale + itemScale + 2), Window.Height - (itemScale + 16 + bottomup), 0), itemScale, sub3d);
                RenderItem(GetItemForSlot(QuickBarPos + 1), new Location(center + (itemScale + 1), Window.Height - (itemScale + 16 + bottomup), 0), itemScale, sub3d);
                RenderItem(GetItemForSlot(QuickBarPos + 2), new Location(center + (itemScale + itemScale + 2), Window.Height - (itemScale + 16 + bottomup), 0), itemScale, sub3d);
                RenderItem(GetItemForSlot(QuickBarPos), new Location(center - (itemScale + 1), Window.Height - (itemScale * 2 + bottomup), 0), itemScale * 2, sub3d);
                if (!sub3d)
                {
                    string it = "^%^e^7" + GetItemForSlot(QuickBarPos).DisplayName;
                    float size = FontSets.Standard.MeasureFancyText(it);
                    FontSets.Standard.DrawColoredText(it, new Location(center - size / 2f, Window.Height - (itemScale * 2 + bottomup) - FontSets.Standard.font_default.Height - 5, 0));
                    float percent = 0;
                    if (Player.MaxHealth != 0)
                    {
                        percent = (float)Math.Round((Player.Health / Player.MaxHealth) * 10000) / 100f;
                    }
                    int healthbaroffset = 300;
                    Textures.White.Bind();
                    Rendering.SetColor(Color4.Black);
                    Rendering.RenderRectangle(center - healthbaroffset, Window.Height - 30, center + healthbaroffset, Window.Height - 2);
                    Rendering.SetColor(Color4.Red);
                    Rendering.RenderRectangle(center - healthbaroffset + 2, Window.Height - 28, center - (healthbaroffset - 2) * ((100 - percent) / 100), Window.Height - 4);
                    Rendering.SetColor(Color4.Cyan);
                    Rendering.RenderRectangle(center + 2, Window.Height - 28, center + healthbaroffset - 2, Window.Height - 4); // TODO: Armor percent
                    FontSets.SlightlyBigger.DrawColoredText("^S^!^e^0Health: " + Player.Health.ToString(healthformat) + "/" + Player.MaxHealth.ToString(healthformat) + " = " + percent.ToString(healthformat) + "%",
                        new Location(center - healthbaroffset + 4, Window.Height - 26, 0));
                    FontSets.SlightlyBigger.DrawColoredText("^S^%^e^0Armor: " + "100.0" + "/" + "100.0" + " = " + "100.0" + "%", // TODO: Armor values!
                        new Location(center + 4, Window.Height - 26, 0));
                    if (CVars.u_showmap.ValueB)
                    {
                        Rendering.SetColor(Color4.White);
                        Textures.Black.Bind();
                        Rendering.RenderRectangle(Window.Width - 16 - 200, 16, Window.Width - 16, 16 + 200); // TODO: Dynamic size?
                        GL.BindTexture(TextureTarget.Texture2D, map_fbo_texture);
                        Rendering.RenderRectangle(Window.Width - 16 - (200 - 2), 16 + 2, Window.Width - 16 - 2, 16 + (200 - 2));
                    }
                    int cX = Window.Width / 2;
                    int cY = Window.Height / 2;
                    int move = (int)Player.GetVelocity().LengthSquared() / 5;
                    if (move > 20)
                    {
                        move = 20;
                    }
                    Rendering.SetColor(Color4.White);
                    Textures.GetTexture("ui/hud/reticles/" + CVars.u_reticle.Value + "_tl").Bind(); // TODO: Save! Don't re-grab every tick!
                    Rendering.RenderRectangle(cX - CVars.u_reticlescale.ValueI - move, cY - CVars.u_reticlescale.ValueI - move, cX - move, cY - move);
                    Textures.GetTexture("ui/hud/reticles/" + CVars.u_reticle.Value + "_tr").Bind();
                    Rendering.RenderRectangle(cX + move, cY - CVars.u_reticlescale.ValueI - move, cX + CVars.u_reticlescale.ValueI + move, cY - move);
                    Textures.GetTexture("ui/hud/reticles/" + CVars.u_reticle.Value + "_bl").Bind();
                    Rendering.RenderRectangle(cX - CVars.u_reticlescale.ValueI - move, cY + move, cX - move, cY + CVars.u_reticlescale.ValueI + move);
                    Textures.GetTexture("ui/hud/reticles/" + CVars.u_reticle.Value + "_br").Bind();
                    Rendering.RenderRectangle(cX + move, cY + move, cX + CVars.u_reticlescale.ValueI + move, cY + CVars.u_reticlescale.ValueI + move);
                    if (CVars.u_showrangefinder.ValueB)
                    {
                        FontSets.Standard.DrawColoredText(CameraDistance.ToString("0.0"), new Location(cX + move + CVars.u_reticlescale.ValueI, cY + move + CVars.u_reticlescale.ValueI, 0));
                    }
                    if (CVars.u_showcompass.ValueB)
                    {
                        Textures.White.Bind();
                        Rendering.SetColor(Color4.Black);
                        Rendering.RenderRectangle(64, Window.Height - (32 + 32), Window.Width - 64, Window.Height - 32);
                        Rendering.SetColor(Color4.Gray);
                        Rendering.RenderRectangle(66, Window.Height - (32 + 30), Window.Width - 66, Window.Height - 34);
                        Rendering.SetColor(Color4.White);
                        RenderCompassCoord(Vector4d.UnitY, "N");
                        RenderCompassCoord(-Vector4d.UnitY, "S");
                        RenderCompassCoord(Vector4d.UnitX, "E");
                        RenderCompassCoord(-Vector4d.UnitX, "W");
                        RenderCompassCoord(new Vector4d(1, 1, 0, 0), "NE");
                        RenderCompassCoord(new Vector4d(1, -1, 0, 0), "SE");
                        RenderCompassCoord(new Vector4d(-1, 1, 0, 0), "NW");
                        RenderCompassCoord(new Vector4d(-1, -1, 0, 0), "SW");
                    }
                }
            }
            if (sub3d)
            {
                FixPersp = Matrix4.Identity;
            }
        }

        /// <summary>
        /// Renders a compass coordinate to the screen.
        /// </summary>
        public void RenderCompassCoord(Vector4d rel, string dir)
        {
            Vector4d camp = new Vector4d(ClientUtilities.ConvertD(PlayerEyePosition), 1.0);
            Vector4d north = Vector4d.Transform(camp + rel * 10, MainWorldView.PrimaryMatrixd);
            double northOnScreen = north.X / north.W;
            if (north.Z <= 0 && northOnScreen < 0)
            {
                northOnScreen = -1f;
            }
            else if (north.Z <= 0 && northOnScreen > 0)
            {
                northOnScreen = 1f;
            }
            northOnScreen = Math.Max(100, Math.Min(Window.Width - 100, (0.5f + northOnScreen * 0.5f) * Window.Width));
            FontSets.Standard.DrawColoredText(dir, new Location(northOnScreen, Window.Height - (32 + 28), 0));
        }

        /// <summary>
        /// Whether the view is currently orthographic.
        /// </summary>
        public bool IsOrtho = false;

        /// <summary>
        /// Renders an item on the 2D screen.
        /// </summary>
        /// <param name="item">The item to render.</param>
        /// <param name="pos">Where to render it.</param>
        /// <param name="size">How big to render it, in pixels.</param>
        public void RenderItem(ItemStack item, Location pos, int size, bool sub3d)
        {
            if (sub3d)
            {
                IsOrtho = true;
                item.Render3D(pos + new Location(size * 0.5f), (float)GlobalTickTimeLocal * 0.5f, new Location(size * 0.75));
                IsOrtho = false;
                return;
            }
            ItemFrame.Bind();
            Rendering.SetColor(Color4.White);
            Rendering.RenderRectangle((int)pos.X - 1, (int)pos.Y - 1, (int)(pos.X + size) + 1, (int)(pos.Y + size) + 1);
            item.Render(pos, new Location(size, size, 0));
            if (item.Count > 0)
            {
                FontSets.SlightlyBigger.DrawColoredText("^!^e^7^S" + item.Count, new Location(pos.X + 5, pos.Y + size - FontSets.SlightlyBigger.font_default.Height / 2f - 5, 0));
            }
        }

        /// <summary>
        /// What orthographic matrix is currently in use for 2D views.
        /// </summary>
        public Matrix4 Ortho;
    }
}
