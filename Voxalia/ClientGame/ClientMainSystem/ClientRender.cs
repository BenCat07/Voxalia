//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
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
using Voxalia.ClientGame.OtherSystems;
using Voxalia.ClientGame.JointSystem;
using Voxalia.Shared.Collision;
using System.Diagnostics;
using Voxalia.ClientGame.WorldSystem;
using Voxalia.ClientGame.EntitySystem;
using FreneticGameCore.Files;
using FreneticGameCore;
using FreneticGameGraphics;
using FreneticGameCore.Collision;
using FreneticGameGraphics.GraphicsHelpers;
using FreneticGameGraphics.ClientSystem;
using FreneticGameGraphics.LightingSystem;

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
        /// </summary>
        public ConcurrentQueue<ChunkVBO> vbos = new ConcurrentQueue<ChunkVBO>();

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
            long chunkc_transp = 0;
            foreach (Chunk chunk in TheRegion.LoadedChunks.Values)
            {
                if (chunk._VBOSolid != null)
                {
                    chunkc += chunk._VBOSolid.GetVRAMUsage();
                }
                if (chunk._VBOTransp != null)
                {
                    chunkc += chunk._VBOTransp.GetVRAMUsage();
                }
            }
            toret.Add(new Tuple<string, long>("Chunks", chunkc));
            toret.Add(new Tuple<string, long>("Chunks_Transparent", chunkc_transp));
            // TODO: Maybe also View3D render helpers usage?
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

        const float FOGMAXDIST_3 = 55000;

        const float FOGMAXDIST_2 = 11000;

        const float FOGMAXDIST_1 = 30 * 90;

        float CurFogMax = FOGMAXDIST_1;

        public float ZFarOut()
        {
            return CurFogMax * 3.0f;
        }

        public float FogMaxDist()
        {
            if (CVars.r_compute.ValueB)
            {
                return CurFogMax * 2.0f;
            }
            return ZFar();
        }

        /// <summary>
        /// The rendering subsystem for the primary world view.
        /// This is in some situations temporarily swapped for the currently rendering view as needed.
        /// </summary>
        public View3D MainWorldView = new View3D();

        /// <summary>
        /// The rendering subsystem for the item bar.
        /// </summary>
        public View3D ItemBarView = new View3D();

        /// <summary>
        /// Early startup call to preparing some rendering systems.
        /// </summary>
        void InitRendering()
        {
            MainWorldView.CameraModifier = () => Player.GetRelativeQuaternion();
            ShadersCheck();
            GraphicsUtil.CheckError("Load - Rendering - Shaders");
            GenerateMapHelpers();
            GenerateGrassHelpers();
            PrepDecals();
            GraphicsUtil.CheckError("Load - Rendering - Map/Grass");
            MainWorldView.ShadowingAllowed = true;
            MainWorldView.ShadowTexSize = () => CVars.r_shadowquality.ValueI;
            MainWorldView.Render3D = Render3D;
            MainWorldView.DecalRender = RenderDecal;
            MainWorldView.PostFirstRender = ReverseEntitiesOrder;
            MainWorldView.LLActive = CVars.r_transpll.ValueB; // TODO: CVar edit call back
            GraphicsUtil.CheckError("Load - Rendering - Settings");
            MainWorldView.Generate(Engine, Window.Width, Window.Height);
            GraphicsUtil.CheckError("Load - Rendering - ViewGen");
            ItemBarView.FastOnly = true;
            ItemBarView.ClearColor = new float[] { 1f, 1f, 1f, 0f };
            ItemBarView.Render3D = RenderItemBar;
            foreach (LightObject light in ItemBarView.Lights)
            {
                foreach (Light li in light.InternalLights)
                {
                    li.Destroy();
                }
            }
            ItemBarView.Lights.Clear();
            ItemBarView.RenderClearAlpha = 0f;
            SkyLight tlight = new SkyLight(new Location(0, 0, 10), 64, Location.One, new Location(0, -1, -1).Normalize(), 64, false, 64);
            ItemBarView.Lights.Add(tlight);
            ItemBarView.Width = 1024;
            ItemBarView.Height = 256;
            ItemBarView.GenerateFBO();
            ItemBarView.Generate(Engine, 1024, 256);
            // TODO: Use the item bar in VR mode.
            GraphicsUtil.CheckError("Load - Rendering - Item Bar");
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
            GraphicsUtil.CheckError("Load - Rendering - Sky Prep");
            for (int i = 0; i < 6; i++)
            {
                skybox[i].GenerateVBO();
            }
            RainCyl = Models.GetModel("raincyl");
            RainCyl.LoadSkin(Textures);
            SnowCyl = Models.GetModel("snowcyl");
            SnowCyl.LoadSkin(Textures);
            GraphicsUtil.CheckError("Load - Rendering - Final");
            TWOD_FBO = GL.GenFramebuffer();
            TWOD_FBO_Tex = GL.GenTexture();
            TWOD_FixTexture();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, TWOD_FBO);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, TWOD_FBO_Tex, 0);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            FixView(MainWorldView);
            FixView(MainItemView);
            FixView(ItemBarView);
        }

        public void RenderItemBar(View3D renderer)
        {
            Render2D(true);
            // TODO: Render2D(false) as well in here! Item bar only though!
        }

        public int TWOD_CFrame = 0;

        public int TWOD_FBO;

        public int TWOD_FBO_Tex;

        public void TWOD_FixTexture()
        {
            GL.BindTexture(TextureTarget.Texture2D, TWOD_FBO_Tex);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, Window.Width, Window.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        public bool AllowLL = false;

        public void OnLLChanged(object sender, EventArgs e)
        {
            if (!AllowLL && CVars.r_transpll.ValueB)
            {
                CVars.r_transpll.Set(false);
            }
        }

        /// <summary>
        /// Grab all the correct shader objects.
        /// </summary>
        public void ShadersCheck()
        {
            string def = Shaders.MCM_GOOD_GRAPHICS ? "#MCM_GOOD_GRAPHICS" : "#";
            s_shadowvox = Shaders.GetShader("shadowvox" + def);
            s_fbovslod = Shaders.GetShader("fbo_vox" + def + ",MCM_TRANSP_ALLOWED");
            s_fbov = Shaders.GetShader("fbo_vox" + def + ",MCM_TH");
            s_fbov_refract = Shaders.GetShader("fbo_vox" + def + ",MCM_REFRACT");
            s_transponlyvox = Shaders.GetShader("transponlyvox" + def);
            s_transponlyvoxlit = Shaders.GetShader("transponlyvox" + def + ",MCM_LIT");
            s_transponlyvoxlitsh = Shaders.GetShader("transponlyvox" + def + ",MCM_LIT,MCM_SHADOWS");
            s_map = Shaders.GetShader("map" + def);
            s_mapvox = Shaders.GetShader("map" + def + ",MCM_VOX");
            s_transpadder = Shaders.GetShader("transpadder" + def);
            string forw_extra = (CVars.r_forward_normals.ValueB ? ",MCM_NORMALS" : "")
                + (CVars.r_forward_lights.ValueB ? ",MCM_LIGHTS" : "")
                + (CVars.r_forward_shadows.ValueB ? ",MCM_SHADOWS" : "");
            s_forw_vox_slod = Shaders.GetShader("forward_vox" + def + ",MCM_SIMPLE_LIGHT,MCM_NO_ALPHA_CAP,MCM_ANTI_TRANSP,MCM_SLOD_LIGHT");
            s_forw_vox = Shaders.GetShader("forward_vox" + def + ",MCM_TH" + forw_extra);
            s_forw_vox_trans = Shaders.GetShader("forward_vox" + def + ",MCM_TRANSP,MCM_TH" + forw_extra);
            if (AllowLL)
            {
                s_transponlyvox_ll = Shaders.GetShader("transponlyvox" + def + ",MCM_LL");
                s_transponlyvoxlit_ll = Shaders.GetShader("transponlyvox" + def + ",MCM_LIT,MCM_LL");
                s_transponlyvoxlitsh_ll = Shaders.GetShader("transponlyvox" + def + ",MCM_LIT,MCM_SHADOWS,MCM_LL");
            }
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
        public void GenerateMapHelpers()
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
            if (GrassTextureLocations.TryGetValue(f, out int temp))
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
        /// The Shadow Pass shader, for voxels.
        /// </summary>
        public Shader s_shadowvox;

        /// <summary>
        /// The G-Buffer FBO shader, for voxels.
        /// </summary>
        public Shader s_fbov;

        /// <summary>
        /// The G-Buffer FBO shader, for SLOD voxels.
        /// </summary>
        public Shader s_fbovslod;

        /// <summary>
        /// The G-Buffer FBO shader, for the refraction pass, for voxels.
        /// </summary>
        public Shader s_fbov_refract;

        /// <summary>
        /// The shader used only for transparent voxels.
        /// </summary>
        public Shader s_transponlyvox;

        /// <summary>
        /// The shader used only for transparent voxels with lighting.
        /// </summary>
        public Shader s_transponlyvoxlit;

        /// <summary>
        /// The shader used only for transparent voxels with shadowed lighting.
        /// </summary>
        public Shader s_transponlyvoxlitsh;

        /// <summary>
        /// The shader used to calculate the in-game map data.
        /// </summary>
        public Shader s_map;

        /// <summary>
        /// The shader used to calculate the in-game map voxels.
        /// </summary>
        public Shader s_mapvox;

        /// <summary>
        /// The shader used as the final step of adding transparent data to the scene.
        /// TODO: Optimize this away.
        /// </summary>
        public Shader s_transpadder;

        /// <summary>
        /// The shader used for forward ('fast') rendering of voxels.
        /// </summary>
        public Shader s_forw_vox;

        /// <summary>
        /// The shader used for forward ('fast') rendering of SLOD voxels.
        /// </summary>
        public Shader s_forw_vox_slod;

        /// <summary>
        /// The shader used for forward ('fast') rendering of transparent voxels.
        /// </summary>
        public Shader s_forw_vox_trans;

        /// <summary>
        /// The shader used for transparent voxels (LinkedList Transparency version).
        /// </summary>
        public Shader s_transponlyvox_ll;

        /// <summary>
        /// The shader used for lit transparent voxels (LinkedList Transparency version).
        /// </summary>
        public Shader s_transponlyvoxlit_ll;

        /// <summary>
        /// The shader used for shadowed lit transparent voxels (LinkedList Transparency version).
        /// </summary>
        public Shader s_transponlyvoxlitsh_ll;

        /// <summary>
        /// Sorts all entities by distance to camera.
        /// TODO: Speed analysis? Probably doesn't matter with an average of 'a few hundred' entities...
        /// </summary>
        public void SortEntities()
        {
            TheRegion.Entities = TheRegion.Entities.OrderBy((o) => o.GetPosition().DistanceSquared(MainWorldView.RenderRelative)).ToList();
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

        public int gFPS_Min = 0;

        public int gFPS_Max = 0;

        /// <summary>
        /// Render ticks since last shadow update.
        /// </summary>
        int rTicks = 1000;

        /// <summary>
        /// Whether shadows should be redrawn this frame.
        /// </summary>
        public bool shouldRedrawShadows = true;

        /// <summary>
        /// How many things the game is currently loading.
        /// </summary>
        public int Loading = 0;

        /// <summary>
        /// Draws the entire 2D environment.
        /// </summary>
        public void Draw2DEnv()
        {
            CWindow.MainUI.InternalCurrentScreen = CScreen;
            CWindow.MainUI.Draw();
            //CScreen.FullRender(CWindow.MainUI, gDelta, 0, 0);
        }

        /// <summary>
        /// The main entry point for the render and tick cycles.
        /// </summary>
        public void Window_RenderFrame(object sender, FrameEventArgs e)
        {
            ErrorCode ec = GL.GetError();
            while (ec != ErrorCode.NoError)
            {
                SysConsole.Output(OutputType.WARNING, "Unhandled GL error: " + ec);
                ec = GL.GetError();
            }
            lock (TickLock)
            {
                float goalMax = (VoxelComputer.Tops3Chunk == null || !VoxelComputer.Tops3Chunk.generated) ? (VoxelComputer.Tops2Chunk == null || !VoxelComputer.Tops2Chunk.generated ? FOGMAXDIST_1 : FOGMAXDIST_2) : FOGMAXDIST_3;
                if (goalMax > CurFogMax)
                {
                    CurFogMax = Math.Min(goalMax, CurFogMax * (float)Math.Pow(1.5f, Delta));
                }
                else
                {
                    CurFogMax = Math.Max(goalMax, CurFogMax * (float)Math.Pow(0.6666f, Delta));
                }
                gDelta = e.Time;
                int gfps = (int)(1.0 / gDelta);
                gFPS_Min = gFPS_Min == 0 ? gfps : Math.Min(gFPS_Min, gfps);
                gFPS_Max = Math.Max(gFPS_Max, gfps);
                gTicks++;
                GraphicsUtil.CheckError("RenderFrame - Start");
                if (Window.Visible && Window.WindowState != WindowState.Minimized && Window.Width > 10 && Window.Height > 10)
                {
                    try
                    {
                        if (CScreen != TheLoadScreen)
                        {
                            RenderGame();
                        }
                        TWOD_CFrame++;
                        if (CScreen != TheGameScreen || TWOD_CFrame > CVars.u_rate.ValueI)
                        {
                            ItemBarView.CameraPos = -Forw * 10;
                            ItemBarView.ForwardVec = Forw;
                            ItemBarView.CameraUp = () => Location.UnitY; // TODO: Should this really be Y? Probably not...
                            View3D temp = MainWorldView;
                            MainWorldView = ItemBarView;
                            ItemBarView.Render();
                            MainWorldView = temp;
                            GraphicsUtil.CheckError("ItemBarRender");
                            TWOD_CFrame = 0;
                            Establish2D();
                            GraphicsUtil.CheckError("RenderFrame - Establish");
                            GL.Disable(EnableCap.CullFace);
                            GL.BindFramebuffer(FramebufferTarget.Framebuffer, TWOD_FBO);
                            GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
                            GL.ClearBuffer(ClearBuffer.Color, 0, new float[] { 0, 0, 0, 0 });
                            Shaders.ColorMultShader.Bind();
                            //GL.Uniform1(6, (float)GlobalTickTimeLocal);
                            GraphicsUtil.CheckError("RenderFrame - Setup2D");
                            if (CVars.r_3d_enable.ValueB || VR != null)
                            {
                                GL.Viewport(Window.Width / 2, 0, Window.Width / 2, Window.Height);
                                Draw2DEnv();
                                GL.Viewport(0, 0, Window.Width / 2, Window.Height);
                                Draw2DEnv();
                                GL.Viewport(0, 0, Window.Width, Window.Height);
                            }
                            else
                            {
                                Draw2DEnv();
                            }
                            GraphicsUtil.CheckError("RenderFrame - 2DEnv");
                            UIConsole.Draw();
                        }
                        GraphicsUtil.CheckError("RenderFrame - Basic");
                        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                        GL.DrawBuffer(DrawBufferMode.Back);
                        Shaders.ColorMultShader.Bind();
                        Rendering.SetColor(Vector4.One, MainWorldView);
                        GL.Disable(EnableCap.DepthTest);
                        GL.Disable(EnableCap.CullFace);
                        if (VR != null)
                        {
                            GL.UniformMatrix4(1, false, ref MainWorldView.SimpleOrthoMatrix);
                            GL.UniformMatrix4(2, false, ref View3D.IdentityMatrix);
                            GL.BindTexture(TextureTarget.Texture2D, MainWorldView.CurrentFBOTexture);
                            Rendering.RenderRectangle(-1, -1, 1, 1);
                        }
                        GraphicsUtil.CheckError("RenderFrame - VR");
                        GL.BindTexture(TextureTarget.Texture2D, TWOD_FBO_Tex);
                        Matrix4 ortho = Matrix4.CreateOrthographicOffCenter(0, Window.Width, 0, Window.Height, -1, 1);
                        GL.UniformMatrix4(100, false, ref View3D.IdentityMatrix);
                        GL.UniformMatrix4(1, false, ref ortho);
                        Rendering.RenderRectangle(0, 0, Window.Width, Window.Height);
                        GraphicsUtil.CheckError("RenderFrame - TWOD");
                        GL.BindTexture(TextureTarget.Texture2D, 0);
                        GL.Enable(EnableCap.CullFace);
                        GL.Enable(EnableCap.DepthTest);
                    }
                    catch (Exception ex)
                    {
                        SysConsole.Output(OutputType.ERROR, "Rendering (general): " + ex.ToString());
                    }
                }
                GraphicsUtil.CheckError("PreTick");
                Stopwatch timer = new Stopwatch();
                try
                {
                    timer.Start();
                    Tick(e.Time);
                    GraphicsUtil.CheckError("Tick");
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
                GraphicsUtil.CheckError("Finish");
                ec = GL.GetError();
                while (ec != ErrorCode.NoError)
                {
                    SysConsole.Output(OutputType.WARNING, "Unhandled GL error: " + ec);
                    ec = GL.GetError();
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
                if (CVars.r_3d_enable.ValueB || VR != null)
                {
                    CDrawWidth = Window.Width / 2;
                    GL.Viewport(Window.Width / 2, 0, Window.Width / 2, Window.Height);
                    Render2D(false);
                    GL.Viewport(0, 0, Window.Width / 2, Window.Height);
                    Render2D(false);
                    GL.Viewport(0, 0, Window.Width, Window.Height);
                }
                else
                {
                    CDrawWidth = Window.Width;
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
        public void RenderGame()
        {
            Stopwatch totalt = new Stopwatch();
            totalt.Start();
            try
            {
                MainWorldView.ForwardVec = Player.ForwardVector();
                if (VR != null)
                {
                    MainWorldView.CameraPos = Player.GetPosition();
                }
                else if (IsMainMenu)
                {
                    Location forw = Utilities.ForwardVector_Deg((GlobalTickTimeLocal * 2.0) % 360.0, 65);
                    MainWorldView.ForwardVec = -forw;
                    Location eyep = Player.GetCameraPosition();
                    CollisionResult cr = TheRegion.Collision.RayTrace(eyep, eyep + forw * Constants.CHUNK_WIDTH, IgnorePlayer);
                    if (cr.Hit)
                    {
                        MainWorldView.CameraPos = cr.Position + cr.Normal * 0.05;
                    }
                    else
                    {
                        MainWorldView.CameraPos = cr.Position;
                    }
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
                    GL.UniformMatrix4(ShaderLocations.Common.PROJECTION, false, ref ortho);
                    MainWorldView.SetMatrix(ShaderLocations.Common.WORLD, oident);
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
                    GL.BindTexture(TextureTarget.Texture2DArray, 0);
                    s_map = s_map.Bind();
                    GL.UniformMatrix4(ShaderLocations.Common.PROJECTION, false, ref ortho);
                    MainWorldView.SetMatrix(ShaderLocations.Common.WORLD, oident);
                    Textures.White.Bind();
                    foreach (Entity ent in TheRegion.Entities)
                    {
                        ent.RenderForMap();
                    }
                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                    GL.BindTexture(TextureTarget.Texture2DArray, 0);
                    GL.DrawBuffer(DrawBufferMode.Back);
                }
                SortEntities();
                Particles.Sort();
                Material headMat = TheRegion.GetBlockMaterial(VR == null ? MainWorldView.CameraPos : Player.GetBasicEyePos());
                MainWorldView.FogCol = headMat.GetFogColor();
                float fg = (float)headMat.GetFogAlpha();
                if (CVars.r_fog.ValueB && fg < 1.0f)
                {
                    fg = 0.9f;
                    MainWorldView.FogCol = SkyColor;
                }
                MainWorldView.FogAlpha = (FogEnhanceTime > 0.01) ? Math.Max(fg, (FogEnhanceTime < 1.0 ? (FogEnhanceStrength - ((1.0f - (float)FogEnhanceTime) * FogEnhanceStrength)) : FogEnhanceStrength)) : fg;
                MainWorldView.SunLocation = GetSunLocation();
                // TODO: MainWorldView.AudioLevel = Sounds.EstimateAudioLevel();
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
            return ZFarOut() * 0.65f;
        }

        /// <summary>
        /// How far the day sky is away from the camera.
        /// </summary>
        public float GetSkyDistance()
        {
            return ZFarOut() * 0.5f;
        }

        /// <summary>
        /// Gets the present 3D location of the sun.
        /// </summary>
        public Location GetSunLocation()
        {
            return MainWorldView.CameraPos + TheSun.Direction * -(GetSkyDistance() * 0.9f);
        }

        /// <summary>
        /// Renders the entire skybox.
        /// </summary>
        public void RenderSkybox()
        {
            GraphicsUtil.CheckError("Rendering - Sky - Before");
            GL.UniformMatrix4(1, false, ref MainWorldView.OutViewMatrix);
            GraphicsUtil.CheckError("Rendering - Sky - Prep - Mat");
            float skyAlpha = (float)Math.Max(Math.Min((SunAngle.Pitch - 70.0) / (-90.0), 1.0), 0.06);
            GL.ActiveTexture(TextureUnit.Texture3);
            Textures.Black.Bind();
            GL.ActiveTexture(TextureUnit.Texture2);
            Textures.Black.Bind();
            GL.ActiveTexture(TextureUnit.Texture1);
            Textures.NormalDef.Bind();
            GL.ActiveTexture(TextureUnit.Texture0);
            GraphicsUtil.CheckError("Rendering - Sky - Prep - Textures");
            //Rendering.SetMinimumLight(1.0f, MainWorldView);
            GraphicsUtil.CheckError("Rendering - Sky - Prep - MinLight");
            //Rendering.SetColor(new Vector4(ClientUtilities.Convert(Location.One * 1.6f), 1)); // TODO: 1.6 -> Externally defined constant. SunLightMod? Also, verify this value is used properly!
            GL.Disable(EnableCap.CullFace);
            Rendering.SetColor(Color4.White, MainWorldView);
            GraphicsUtil.CheckError("Rendering - Sky - Prep - Color");
            Matrix4 scale = Matrix4.CreateScale(GetSecondSkyDistance());
            GL.UniformMatrix4(2, false, ref scale);
            GraphicsUtil.CheckError("Rendering - Sky - Prep - Scale");
            GraphicsUtil.CheckError("Rendering - Sky - Prep");
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
            Rendering.SetColor(new Vector4(1, 1, 1, skyAlpha), MainWorldView);
            scale = Matrix4.CreateScale(GetSkyDistance());
            GL.UniformMatrix4(2, false, ref scale);
            GraphicsUtil.CheckError("Rendering - Sky - Night");
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
            Rendering.SetColor(new Vector4(ClientUtilities.Convert(Location.One * SunLightModDirect), 1), MainWorldView);
            GraphicsUtil.CheckError("Rendering - Sky - Light");
            //Rendering.SetMinimumLight(0, MainWorldView);
            GraphicsUtil.CheckError("Rendering - Sky - Sun - Pre 1");
            Rendering.SetColor(Color4.White, MainWorldView);
            GraphicsUtil.CheckError("Rendering - Sky - Sun - Pre 1.5");
            if (CVars.r_fast.ValueB)
            {
                Engine.Shaders3D.s_forwt_nofog.Bind();
                GL.UniformMatrix4(1, false, ref MainWorldView.OutViewMatrix);
                //GL.Uniform2(14, new Vector2(60f, ZFarOut()));
            }
            else
            {
                // TODO: Deferred option?
            }
            GraphicsUtil.CheckError("Rendering - Sky - Sun - Pre 2");
            float zf = ZFar();
            float spf = ZFarOut() * 0.3333f;
            Textures.GetTexture("skies/sun").Bind(); // TODO: Store var!
            Matrix4 rot = Matrix4.CreateTranslation(-spf * 0.5f, -spf * 0.5f, 0f)
                * Matrix4.CreateRotationY((float)((-SunAngle.Pitch - 90f) * Utilities.PI180))
                * Matrix4.CreateRotationZ((float)((180f + SunAngle.Yaw) * Utilities.PI180))
                * Matrix4.CreateTranslation(ClientUtilities.Convert(TheSun.Direction * -(GetSkyDistance() * 0.95f)));
            Rendering.RenderRectangle(0, 0, spf, spf, rot); // TODO: Adjust scale based on view rad
            GraphicsUtil.CheckError("Rendering - Sky - Sun");
            Textures.GetTexture("skies/planet_sphere").Bind(); // TODO: Store var!
            float ppf = ZFarOut() * 0.5f;
            Rendering.SetColor(new Color4(PlanetLight, PlanetLight, PlanetLight, 1), MainWorldView);
            rot = Matrix4.CreateScale(ppf * 0.5f)
                * Matrix4.CreateTranslation(-ppf * 0.5f, -ppf * 0.5f, 0f)
                //* Matrix4.CreateRotationY((float)((-PlanetAngle.Pitch - 90f) * Utilities.PI180))
                * Matrix4.CreateRotationZ((float)((180f + PlanetAngle.Yaw) * Utilities.PI180))
                * Matrix4.CreateTranslation(ClientUtilities.Convert(PlanetDir * -(GetSkyDistance() * 0.79f)));
            //Rendering.RenderRectangle(0, 0, ppf, ppf, rot);
            GL.UniformMatrix4(2, false, ref rot);
            Models.Sphere.Draw();
            GraphicsUtil.CheckError("Rendering - Sky - Planet");
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.Enable(EnableCap.CullFace);
            Matrix4 ident = Matrix4.Identity;
            GL.UniformMatrix4(2, false, ref ident);
            GraphicsUtil.CheckError("Rendering - Sky - N3 - Pre");
            //Rendering.SetMinimumLight(0, MainWorldView);
            GraphicsUtil.CheckError("Rendering - Sky - N3 - Light");
            Rendering.SetColor(Color4.White, MainWorldView);
            GraphicsUtil.CheckError("Rendering - Sky - N3 - Color");
            if (CVars.r_fast.ValueB)
            {
                Engine.Shaders3D.s_forwt_obj.Bind();
                GL.UniformMatrix4(1, false, ref MainWorldView.OutViewMatrix);
                //GL.Uniform2(14, new Vector2(60f, ZFarOut()));
            }
            else
            {
                // TODO: Deferred option?
            }
            Location pos = MainWorldView.RenderRelative;
            double maxDist = ZFar() * ZFar() * 0.5;
            for (int i = 0; i < TheRegion.Entities.Count; i++) // TODO: Move this block to AFTER chunk SLODs
            {
                if (TheRegion.Entities[i].CanDistanceRender && TheRegion.Entities[i].GetPosition().DistanceSquared(pos) > maxDist)
                {
                    TheRegion.Entities[i].RenderWithOffsetLOD(Location.Zero);
                }
                //Models.GetModel("plants/trees/treevox01").DrawLOD(Player.GetPosition() + Player.ForwardVector() * ZFar() * 0.75);
            }
            for (int i = 0; i < VoxelComputer.TopsTrees.Count; i++)
            {
                // TODO: MORE EFFICIENT RENDERING!!!
                VoxelComputer.TopsTrees[i].Value.DrawLOD(VoxelComputer.TopsTrees[i].Key.ToLocation(), MainWorldView);
            }
            GraphicsUtil.CheckError("Rendering - Sky - Render Offset Ents");
            GL.UniformMatrix4(1, false, ref MainWorldView.PrimaryMatrix);
            if (MainWorldView.FBOid.IsForward())
            {
                //GL.Uniform2(14, new Vector2(CVars.r_znear.ValueF, ZFar()));
            }
            SetVox();
            if (MainWorldView.FBOid.IsForward())
            {
                s_forw_vox_slod = s_forw_vox_slod.Bind();
                //GL.Uniform2(14, new Vector2(60f, ZFarOut()));
            }
            else
            {
                s_fbovslod = s_fbovslod.Bind();
            }
            GL.UniformMatrix4(1, false, ref MainWorldView.OutViewMatrix);
            GraphicsUtil.CheckError("Rendering - Sky - PostPrep");
            foreach (ChunkSLODHelper ch in TheRegion.SLODs.Values)
            {
                Location min = ch.Coordinate.ToLocation() * Chunk.CHUNK_SIZE * Constants.CHUNKS_PER_SLOD;
                Location helpervec = new Location(Chunk.CHUNK_SIZE * Constants.CHUNKS_PER_SLOD, Chunk.CHUNK_SIZE * Constants.CHUNKS_PER_SLOD, Chunk.CHUNK_SIZE * Constants.CHUNKS_PER_SLOD)
                    * (Constants.CHUNKS_PER_SLOD + 2);
                if ((MainWorldView.CFrust == null || MainWorldView.LongFrustum == null || MainWorldView.LongFrustum.ContainsBox(min - helpervec, min + helpervec)))
                {
                    ch.Render();
                }
            }
            if (CVars.r_compute.ValueB && VoxelComputer.Tops3Chunk != null && VoxelComputer.Tops3Chunk.generated)
            {
                const int C_EXTRA = 750;
                const double C_SUB = C_EXTRA + C_EXTRA / 2;
                Matrix4d mat = Matrix4d.CreateTranslation(VoxelComputer.Tops3X * Chunk.CHUNK_SIZE - C_SUB * Chunk.CHUNK_SIZE, VoxelComputer.Tops3Y * Chunk.CHUNK_SIZE - C_SUB * Chunk.CHUNK_SIZE, 0);
                TheRegion.TheClient.MainWorldView.SetMatrix(2, mat);
                GL.Uniform1(8, (float)C_EXTRA * 0.5f);
                VoxelComputer.Tops3Chunk.Render();
            }
            else
            {
                if (CVars.r_compute.ValueB && VoxelComputer.TopsChunk != null && VoxelComputer.TopsChunk.generated)
                {
                    const int C_EXTRA = 30;
                    const double C_SUB = C_EXTRA + C_EXTRA / 2;
                    Matrix4d mat = Matrix4d.CreateTranslation(VoxelComputer.TopsX * Chunk.CHUNK_SIZE - C_SUB * Chunk.CHUNK_SIZE, VoxelComputer.TopsY * Chunk.CHUNK_SIZE - C_SUB * Chunk.CHUNK_SIZE, 0);
                    TheRegion.TheClient.MainWorldView.SetMatrix(2, mat);
                    GL.Uniform1(8, (float)C_EXTRA * 0.5f);
                    VoxelComputer.TopsChunk.Render();
                }
                if (CVars.r_compute.ValueB && VoxelComputer.Tops2Chunk != null && VoxelComputer.Tops2Chunk.generated)
                {
                    const int C_EXTRA = 150;
                    const double C_SUB = C_EXTRA + C_EXTRA / 2;
                    Matrix4d mat = Matrix4d.CreateTranslation(VoxelComputer.Tops2X * Chunk.CHUNK_SIZE - C_SUB * Chunk.CHUNK_SIZE, VoxelComputer.Tops2Y * Chunk.CHUNK_SIZE - C_SUB * Chunk.CHUNK_SIZE, 0);
                    TheRegion.TheClient.MainWorldView.SetMatrix(2, mat);
                    GL.Uniform1(8, (float)C_EXTRA * 0.5f);
                    VoxelComputer.Tops2Chunk.Render();
                }
            }
            /*
            foreach (Chunk ch in TheRegion.LoadedChunks.Values)
            {
                if (ch.PosMultiplier < 5)
                {
                    continue;
                }
                Location min = ch.WorldPosition.ToLocation() * Chunk.CHUNK_SIZE;
                Location max = min + new Location(Chunk.CHUNK_SIZE);
                if ((MainWorldView.CFrust == null || MainWorldView.LongFrustum == null || MainWorldView.LongFrustum.ContainsBox(min, max)))
                {
                    ch.Render();
                }
            }*/
            // TODO: Very distant clouds? new ParticlesEntity(TheRegion) { DistMin = 100, DistMax = TEMP_PARTICLE_MAXRANGE, OutView = true }.Render();
            GraphicsUtil.CheckError("Rendering - Sky - Slods");
            SetEnts();
            if (MainWorldView.FBOid.IsForward())
            {
                if (CVars.r_forwardreflections.ValueB && MainWorldView.FBOid == FBOID.FORWARD_SOLID && MainWorldView.RS4P.IsBound)
                {
                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, MainWorldView.OV_FBO);
                    GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, MainWorldView.RS4P.fbo);
                    GL.BlitFramebuffer(0, 0, MainWorldView.Width, MainWorldView.Height, 0, 0, MainWorldView.Width, MainWorldView.Height, ClearBufferMask.DepthBufferBit, BlitFramebufferFilter.Nearest);
                    GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, 0);
                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, MainWorldView.RS4P.fbo);
                }
                // GL.Uniform2(14, new Vector2(CVars.r_znear.ValueF, ZFar()));
            }
            GL.UniformMatrix4(1, false, ref MainWorldView.PrimaryMatrix);
            SetVox();
            if (MainWorldView.FBOid.IsForward())
            {
                //GL.Uniform2(14, new Vector2(CVars.r_znear.ValueF, ZFar()));
            }
            GL.UniformMatrix4(1, false, ref MainWorldView.PrimaryMatrix);
            GL.ClearBuffer(ClearBuffer.Depth, 0, new float[] { 1f });
            GraphicsUtil.CheckError("Rendering - Sky - Clear");
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
            GL.Viewport(0, 0, Window.Width, Window.Height);
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
                GL.ActiveTexture(TextureUnit.Texture2);
                GL.BindTexture(TextureTarget.Texture2DArray, TBlock.NormalTextureID);
                GL.ActiveTexture(TextureUnit.Texture1);
                GL.BindTexture(TextureTarget.Texture2DArray, TBlock.HelpTextureID);
                GL.ActiveTexture(TextureUnit.Texture0);
            }
            else if (MainWorldView.FBOid == FBOID.FORWARD_TRANSP)
            {
                s_forw_vox_trans = s_forw_vox_trans.Bind();
                GL.BindTexture(TextureTarget.Texture2DArray, TBlock.TextureID);
                GL.ActiveTexture(TextureUnit.Texture2);
                GL.BindTexture(TextureTarget.Texture2DArray, TBlock.NormalTextureID);
                GL.ActiveTexture(TextureUnit.Texture1);
                GL.BindTexture(TextureTarget.Texture2DArray, TBlock.HelpTextureID);
                GL.ActiveTexture(TextureUnit.Texture0);
            }
            else if (MainWorldView.FBOid == FBOID.SHADOWS || MainWorldView.FBOid == FBOID.STATIC_SHADOWS || MainWorldView.FBOid == FBOID.DYNAMIC_SHADOWS)
            {
                s_shadowvox = s_shadowvox.Bind();
                GL.BindTexture(TextureTarget.Texture2DArray, TBlock.TextureID);
            }
            if (FixPersp != Matrix4.Identity)
            {
                GL.UniformMatrix4(ShaderLocations.Common.PROJECTION, false, ref FixPersp);
            }
        }

        bool pBones = false;

        /// <summary>
        /// Switch the system to entity rendering mode.
        /// </summary>
        public void SetEnts(bool bones = false)
        {
            if (!isVox && bones == pBones)
            {
                return;
            }
            pBones = bones;
            isVox = false;
            if (MainWorldView.FBOid == FBOID.MAIN)
            {
                GL.ActiveTexture(TextureUnit.Texture2);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.ActiveTexture(TextureUnit.Texture1);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                Engine.Shaders3D.s_fbo = Engine.Shaders3D.s_fbo.Bind();
            }
            else if (MainWorldView.FBOid == FBOID.REFRACT)
            {
                GL.ActiveTexture(TextureUnit.Texture2);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.ActiveTexture(TextureUnit.Texture1);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                Engine.Shaders3D.s_fbo_refract = Engine.Shaders3D.s_fbo_refract.Bind();
            }
            else if (MainWorldView.FBOid == FBOID.TRANSP_UNLIT)
            {
                GL.ActiveTexture(TextureUnit.Texture2);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.ActiveTexture(TextureUnit.Texture1);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                Engine.Shaders3D.s_transponly = Engine.Shaders3D.s_transponly.Bind();
            }
            else if (MainWorldView.FBOid == FBOID.TRANSP_LIT)
            {
                GL.ActiveTexture(TextureUnit.Texture2);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.ActiveTexture(TextureUnit.Texture1);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                Engine.Shaders3D.s_transponlylit = Engine.Shaders3D.s_transponlylit.Bind();
            }
            else if (MainWorldView.FBOid == FBOID.TRANSP_SHADOWS)
            {
                GL.ActiveTexture(TextureUnit.Texture2);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.ActiveTexture(TextureUnit.Texture1);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                Engine.Shaders3D.s_transponlylitsh = Engine.Shaders3D.s_transponlylitsh.Bind();
            }
            else if (MainWorldView.FBOid == FBOID.TRANSP_LL)
            {
                GL.ActiveTexture(TextureUnit.Texture2);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.ActiveTexture(TextureUnit.Texture1);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                Engine.Shaders3D.s_transponly_ll = Engine.Shaders3D.s_transponly_ll.Bind();
            }
            else if (MainWorldView.FBOid == FBOID.TRANSP_LIT_LL)
            {
                GL.ActiveTexture(TextureUnit.Texture2);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.ActiveTexture(TextureUnit.Texture1);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                Engine.Shaders3D.s_transponlylit_ll = Engine.Shaders3D.s_transponlylit_ll.Bind();
            }
            else if (MainWorldView.FBOid == FBOID.TRANSP_SHADOWS_LL)
            {
                GL.ActiveTexture(TextureUnit.Texture2);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.ActiveTexture(TextureUnit.Texture1);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                Engine.Shaders3D.s_transponlylitsh_ll = Engine.Shaders3D.s_transponlylitsh_ll.Bind();
            }
            else if (MainWorldView.FBOid == FBOID.FORWARD_SOLID)
            {
                if (bones)
                {
                    Engine.Shaders3D.s_forw = Engine.Shaders3D.s_forw.Bind();
                }
                else
                {
                    Engine.Shaders3D.s_forw_nobones = Engine.Shaders3D.s_forw_nobones.Bind();
                }
                GL.ActiveTexture(TextureUnit.Texture2);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2DArray, 0);
            }
            else if (MainWorldView.FBOid == FBOID.FORWARD_TRANSP)
            {
                if (bones)
                {
                    Engine.Shaders3D.s_forw_trans = Engine.Shaders3D.s_forw_trans.Bind();
                }
                else
                {
                    Engine.Shaders3D.s_forw_trans_nobones = Engine.Shaders3D.s_forw_trans_nobones.Bind();
                }
                GL.ActiveTexture(TextureUnit.Texture2);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.ActiveTexture(TextureUnit.Texture1);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2DArray, 0);
            }
            else if (MainWorldView.FBOid == FBOID.FORWARD_EXTRAS)
            {
                Engine.Shaders3D.s_forwdecal = Engine.Shaders3D.s_forwdecal.Bind();
                GL.ActiveTexture(TextureUnit.Texture2);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.ActiveTexture(TextureUnit.Texture1);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2DArray, 0);
            }
            else if (MainWorldView.FBOid == FBOID.MAIN_EXTRAS)
            {
                Engine.Shaders3D.s_fbodecal = Engine.Shaders3D.s_fbodecal.Bind();
                GL.ActiveTexture(TextureUnit.Texture2);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.ActiveTexture(TextureUnit.Texture1);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2DArray, 0);
            }
            else if (MainWorldView.FBOid == FBOID.SHADOWS || MainWorldView.FBOid == FBOID.STATIC_SHADOWS || MainWorldView.FBOid == FBOID.DYNAMIC_SHADOWS)
            {
                GL.BindTexture(TextureTarget.Texture2DArray, 0);
                if (bones)
                {
                    Engine.Shaders3D.s_shadow = Engine.Shaders3D.s_shadow.Bind();
                }
                else
                {
                    Engine.Shaders3D.s_shadow_nobones = Engine.Shaders3D.s_shadow_nobones.Bind();
                }
            }
            if (FixPersp != Matrix4.Identity)
            {
                GL.UniformMatrix4(ShaderLocations.Common.PROJECTION, false, ref FixPersp);
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
            Rendering.SetMinimumLight(1, MainWorldView);
            Model tmod = Models.GetModel("vr/controller/vive"); // TODO: Store the model in a var somewhere?
            VBO mmcircle = tmod.MeshFor("circle").vbo;
            tmod.LoadSkin(Textures);
            // TODO: Special dynamic controller models!
            if (VR.Left != null)
            {
                Matrix4 pos = Matrix4.CreateScale(1.5f) * VR.Left.Position;
                VR.LeftTexture.CalcTexture(VR.Left, GlobalTickTimeLocal, this);
                isVox = true;
                SetEnts();
                mmcircle.Tex = new Texture() { Internal_Texture = VR.LeftTexture.Texture, Engine = Textures };
                GL.UniformMatrix4(2, false, ref pos);
                tmod.Draw();
            }
            if (VR.Right != null)
            {
                Matrix4 pos = Matrix4.CreateScale(1.5f) * VR.Right.Position;
                VR.RightTexture.CalcTexture(VR.Right, GlobalTickTimeLocal, this);
                isVox = true;
                SetEnts();
                mmcircle.Tex = new Texture() { Internal_Texture = VR.RightTexture.Texture, Engine = Textures };
                GL.UniformMatrix4(2, false, ref pos);
                tmod.Draw();
            }
            GraphicsUtil.CheckError("Rendering - VR");
        }

        public int Dec_VAO = -1;
        public int Dec_VBO_Pos = -1;
        public int Dec_VBO_Nrm = -1;
        public int Dec_VBO_Ind = -1;
        public int Dec_VBO_Col = -1;
        public int Dec_VBO_Tcs = -1;
        public int DecTextureID = -1;
        public int DecNTextureID = -1;

        public const int DecTextureWidth = 64; // TODO: Configurable!
        public const int DecTextureCount = 128; // TODO: Configurable!

        public double[] DecalLastTexUse = new double[DecTextureCount];

        public Dictionary<string, int> DecalTextureLocations = new Dictionary<string, int>();

        public void PrepDecals()
        {
            Dec_VAO = GL.GenVertexArray();
            Dec_VBO_Pos = GL.GenBuffer();
            Dec_VBO_Nrm = GL.GenBuffer();
            Dec_VBO_Ind = GL.GenBuffer();
            Dec_VBO_Col = GL.GenBuffer();
            Dec_VBO_Tcs = GL.GenBuffer();
            DecTextureID = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2DArray, DecTextureID);
            GL.TexStorage3D(TextureTarget3d.Texture2DArray, 1, SizedInternalFormat.Rgba8, DecTextureWidth, DecTextureWidth, DecTextureCount);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.BindTexture(TextureTarget.Texture2DArray, 0);
            DecNTextureID = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2DArray, DecNTextureID);
            GL.TexStorage3D(TextureTarget3d.Texture2DArray, 1, SizedInternalFormat.Rgba8, DecTextureWidth, DecTextureWidth, DecTextureCount);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.BindTexture(TextureTarget.Texture2DArray, 0);
        }

        public int DecalGetTextureID(string f)
        {
            if (DecalTextureLocations.TryGetValue(f, out int temp))
            {
                return temp;
            }
            for (int i = 0; i < DecTextureCount; i++)
            {
                if (DecalLastTexUse[i] == 0)
                {
                    DecalLastTexUse[i] = GlobalTickTimeLocal;
                    DecalTextureLocations[f] = i;
                    GL.BindTexture(TextureTarget.Texture2DArray, DecTextureID);
                    Textures.LoadTextureIntoArray(f, i, DecTextureWidth);
                    GL.BindTexture(TextureTarget.Texture2DArray, DecNTextureID);
                    Textures.LoadTextureIntoArray(f + "_nrm", i, DecTextureWidth);
                    GL.BindTexture(TextureTarget.Texture2DArray, 0);
                    return i;
                }
            }
            // TODO: Delete any unused entry findable in favor of this new one.
            return 0;
        }

        public List<DecalInfo> Decals = new List<DecalInfo>();

        public void AddDecal(Location pos, Location ang, Vector4 color, float scale, string texture, double time)
        {
            Decals.Add(new DecalInfo() { Position = pos, NormalDirection = ClientUtilities.Convert(ang), Color = color, Scale = scale, TextureDecalID = DecalGetTextureID(texture), RemainingTime = time });
            // TODO: Actually implement the time? >= 0 fades out, < 0 is there til chunk unload!
        }

        public bool DecalPrepped = false;

        int pDecals = 0;

        /// <summary>
        /// Renders the 3D world's decals upon instruction from the internal view render code.
        /// </summary>
        /// <param name="view">The view to render.</param>
        public void RenderDecal(View3D view)
        {
            //bool isMore = pDecals != Decals.Count;
            pDecals = Decals.Count;
            if (Decals.Count == 0)
            {
                return;
            }
            // TODO: Expiration goes here? Set isMore true if any expired.
            //GL.PolygonOffset(-1, -2);
            GL.Disable(EnableCap.CullFace);
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2DArray, DecNTextureID);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2DArray, DecTextureID);
            //GL.Enable(EnableCap.PolygonOffsetFill);
            GL.BindVertexArray(Dec_VAO);
            GL.DepthFunc(DepthFunction.Lequal);
            // TODO: Add back the isMore check, alongside a reasonably-limited-distance render offsetter uniform var
            //if (isMore || !DecalPrepped)
            {
                Vector3[] pos = new Vector3[Decals.Count];
                Vector3[] nrm = new Vector3[Decals.Count];
                Vector4[] col = new Vector4[Decals.Count];
                Vector2[] tcs = new Vector2[Decals.Count];
                uint[] ind = new uint[Decals.Count];
                for (int i = 0; i < Decals.Count; i++)
                {
                    pos[i] = ClientUtilities.Convert(Decals[i].Position - view.RenderRelative);
                    nrm[i] = Decals[i].NormalDirection;
                    col[i] = Decals[i].Color;
                    tcs[i] = new Vector2(Decals[i].Scale, Decals[i].TextureDecalID);
                    ind[i] = (uint)i;
                }
                GL.BindBuffer(BufferTarget.ArrayBuffer, Dec_VBO_Pos);
                GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(pos.Length * Vector3.SizeInBytes), pos, BufferUsageHint.StaticDraw);
                GL.BindBuffer(BufferTarget.ArrayBuffer, Dec_VBO_Nrm);
                GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(nrm.Length * Vector3.SizeInBytes), nrm, BufferUsageHint.StaticDraw);
                GL.BindBuffer(BufferTarget.ArrayBuffer, Dec_VBO_Tcs);
                GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(tcs.Length * Vector2.SizeInBytes), tcs, BufferUsageHint.StaticDraw);
                GL.BindBuffer(BufferTarget.ArrayBuffer, Dec_VBO_Col);
                GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(col.Length * Vector4.SizeInBytes), col, BufferUsageHint.StaticDraw);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, Dec_VBO_Ind);
                GL.BufferData(BufferTarget.ElementArrayBuffer, (IntPtr)(ind.Length * sizeof(uint)), ind, BufferUsageHint.StaticDraw);
                if (!DecalPrepped)
                {
                    GL.BindBuffer(BufferTarget.ArrayBuffer, Dec_VBO_Pos);
                    GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 0, 0);
                    GL.EnableVertexAttribArray(0);
                    GL.BindBuffer(BufferTarget.ArrayBuffer, Dec_VBO_Nrm);
                    GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 0, 0);
                    GL.EnableVertexAttribArray(1);
                    GL.BindBuffer(BufferTarget.ArrayBuffer, Dec_VBO_Tcs);
                    GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, 0, 0);
                    GL.EnableVertexAttribArray(2);
                    GL.BindBuffer(BufferTarget.ArrayBuffer, Dec_VBO_Col);
                    GL.VertexAttribPointer(4, 4, VertexAttribPointerType.Float, false, 0, 0);
                    GL.EnableVertexAttribArray(4);
                    GL.BindBuffer(BufferTarget.ElementArrayBuffer, Dec_VBO_Ind);
                    DecalPrepped = true;
                }
            }
            Matrix4 ident = Matrix4.Identity;
            GL.UniformMatrix4(2, false, ref ident);
            GL.DrawElements(PrimitiveType.Points, Decals.Count, DrawElementsType.UnsignedInt, IntPtr.Zero);
            GL.BindVertexArray(0);
            GL.DepthFunc(DepthFunction.Less);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.Enable(EnableCap.CullFace);
            //GL.PolygonOffset(0, 0);
            //GL.Disable(EnableCap.PolygonOffsetFill);
        }

        public class ParticlesEntity : Entity
        {
            public double DistMin;

            public double DistMax;

            public bool OutView = false;

            public ParticlesEntity(Region tregion)
                : base(tregion, false, false)
            {
            }

            public override void Render()
            {
                TheClient.SetEnts();
                GL.ActiveTexture(TextureUnit.Texture1);
                TheClient.Textures.NormalDef.Bind();
                GL.ActiveTexture(TextureUnit.Texture0);
                TheClient.Particles.Engine.Render(DistMin, DistMax, OutView);
                GL.ActiveTexture(TextureUnit.Texture1);
                TheClient.Textures.NormalDef.Bind();
                GL.ActiveTexture(TextureUnit.Texture0);
            }

            public override BEPUutilities.Quaternion GetOrientation()
            {
                throw new NotImplementedException();
            }

            public override void SetOrientation(BEPUutilities.Quaternion quat)
            {
                throw new NotImplementedException();
            }

            public override Location GetPosition()
            {
                return TheClient.MainWorldView.CameraPos + new Location((DistMin + DistMax) * 0.5, 0, 0);
            }

            public override void SetPosition(Location pos)
            {
                throw new NotImplementedException();
            }

            public override string ToString()
            {
                return "Particles: " + DistMin;
            }
        }

        public class ChunkEntity : Entity
        {
            public ChunkEntity(Chunk tchunk)
                : base(tchunk.OwningRegion, false, false)
            {
                MainChunk = tchunk;
            }

            public Chunk MainChunk;

            public override void Render()
            {
                TheClient.SetVox();
                TheRegion.ConfigureForRenderChunk();
                MainChunk.Render();
                TheRegion.EndChunkRender();
                TheClient.SetEnts();
            }

            public override BEPUutilities.Quaternion GetOrientation()
            {
                throw new NotImplementedException();
            }

            public override void SetOrientation(BEPUutilities.Quaternion quat)
            {
                throw new NotImplementedException();
            }

            public override Location GetPosition()
            {
                return MainChunk.WorldPosition.ToLocation() * Constants.CHUNK_WIDTH;
            }

            public override void SetPosition(Location pos)
            {
                throw new NotImplementedException();
            }

            public override string ToString()
            {
                return "Chunk: " + MainChunk.WorldPosition;
            }
        }

        const double TEMP_PARTICLE_MAXRANGE = 100000; // TODO: CVar!

        void AddParticles(List<Entity> entsRender)
        {
            if (CVars.r_particles.ValueB)
            {
                // TODO: More clever logic, based on actual entity and particle clumpings?
                // TODO: Alternately, each set of particles (per source) as its own separate bit?
                entsRender.Add(new ParticlesEntity(TheRegion) { DistMin = 0, DistMax = 0.5 });
                entsRender.Add(new ParticlesEntity(TheRegion) { DistMin = 0.5, DistMax = 1 });
                entsRender.Add(new ParticlesEntity(TheRegion) { DistMin = 1, DistMax = 1.75 });
                entsRender.Add(new ParticlesEntity(TheRegion) { DistMin = 1.75, DistMax = 3 });
                entsRender.Add(new ParticlesEntity(TheRegion) { DistMin = 3, DistMax = 4.5 });
                entsRender.Add(new ParticlesEntity(TheRegion) { DistMin = 4.5, DistMax = 7 });
                entsRender.Add(new ParticlesEntity(TheRegion) { DistMin = 7, DistMax = 12 });
                entsRender.Add(new ParticlesEntity(TheRegion) { DistMin = 12, DistMax = 20 });
                entsRender.Add(new ParticlesEntity(TheRegion) { DistMin = 20, DistMax = 40 });
                entsRender.Add(new ParticlesEntity(TheRegion) { DistMin = 40, DistMax = 100 });
            }
        }

        /// <summary>
        /// Renders the 3D world upon instruction from the internal view render code.
        /// </summary>
        /// <param name="view">The view to render.</param>
        public void Render3D(View3D view)
        {
            GraphicsUtil.CheckError("Rendering - 0 - Prep");
            bool transparents = MainWorldView.FBOid.IsMainTransp() || MainWorldView.FBOid == FBOID.FORWARD_TRANSP;
            GL.Enable(EnableCap.CullFace);
            if (view.ShadowsOnly)
            {
                if (view.FBOid != FBOID.STATIC_SHADOWS)
                {
                    GraphicsUtil.CheckError("Rendering - 0 - DynShadows (Pre)");
                    for (int i = 0; i < TheRegion.ShadowCasters.Count; i++)
                    {
                        if (view.FBOid == FBOID.DYNAMIC_SHADOWS && ((TheRegion.ShadowCasters[i] as PhysicsEntity)?.GenBlockShadows).GetValueOrDefault(false))
                        {
                            continue;
                        }
                        TheRegion.ShadowCasters[i].Render();
#if DEBUG
                        GraphicsUtil.CheckError("Rendering - 0 - DynShadows: " + i);
                        /*
                        if (GraphicsUtil.CheckError("Rendering - 0 - DynShadows: " + i))
                        {
                            SysConsole.Output(OutputType.DEBUG, "Caught: " + TheRegion.ShadowCasters[i]);
                        }*/
#endif
                    }
                    GraphicsUtil.CheckError("Rendering - 0 - DynShadows");
                    if (view.FBOid != FBOID.DYNAMIC_SHADOWS)
                    {
                        foreach (Chunk ch in TheRegion.LoadedChunks.Values)
                        {
                            ch.Render();
#if DEBUG
                            GraphicsUtil.CheckError("Rendering - 0 - Shadow:Chunks - Layer: " + ch.WorldPosition.Z);
#endif
                        }
                    }
                    List<Entity> entsRender = new List<Entity>();
                    AddParticles(entsRender);
                    foreach (Entity ent in entsRender)
                    {
                        ent.Render();
#if DEBUG
                        GraphicsUtil.CheckError("Rendering - 0 - TranspShadow - Specific: " + ent.ToString());
#endif
                    }
                }
                else
                {
                    SetVox();
                    TheRegion.ConfigureForRenderChunk();
                    GraphicsUtil.CheckError("Rendering - 0 - Configure Chunks");
                    foreach (Chunk ch in TheRegion.LoadedChunks.Values)
                    {
                        ch.Render();
#if DEBUG
                        GraphicsUtil.CheckError("Rendering - 0 - StaticShadow:Chunks - Layer: " + ch.WorldPosition.Z);
#endif
                    }
                    SetEnts();
                    GraphicsUtil.CheckError("Rendering - 0 - Configure Ents");
                    for (int i = 0; i < TheRegion.GenShadowCasters.Length; i++)
                    {
                        TheRegion.GenShadowCasters[i].Render();
#if DEBUG
                        GraphicsUtil.CheckError("Rendering - 0 - Shadow Gen - Specific: " + TheRegion.GenShadowCasters[i].ToString());
#endif
                    }
                }
                GraphicsUtil.CheckError("Rendering - 0 - Shadows");
            }
            else
            {
                SetEnts();
                GL.ActiveTexture(TextureUnit.Texture1);
                Textures.NormalDef.Bind();
                GL.ActiveTexture(TextureUnit.Texture0);
                if (view.FBOid == FBOID.MAIN)
                {
                    Engine.Shaders3D.s_fbot.Bind();
                    RenderSkybox();
                    Engine.Shaders3D.s_fbo.Bind();
                }
                if (view.FBOid == FBOID.FORWARD_SOLID)
                {
                    Engine.Shaders3D.s_forwt.Bind();
                    RenderSkybox();
                    Engine.Shaders3D.s_forw.Bind();
                }
                GraphicsUtil.CheckError("Rendering - 0 - Sky");
                SetEnts();
                if (transparents)
                {
                    GraphicsUtil.CheckError("Rendering - 0 - Transp - Pre");
                    List<Entity> entsRender = CVars.r_drawents.ValueB ? new List<Entity>(TheRegion.Entities) : new List<Entity>();
                    foreach (Chunk ch in TheRegion.chToRender)
                    {
                        entsRender.Add(new ChunkEntity(ch));
                    }
                    AddParticles(entsRender);
                    Location pos = Player.GetPosition();
                    IEnumerable<Entity> ents = entsRender.OrderBy((e) => e.GetPosition().DistanceSquared(MainWorldView.RenderRelative)).Reverse();
                    GraphicsUtil.CheckError("Rendering - 0 - Transp - Prepared");
                    foreach (Entity ent in ents)
                    {
                        ent.Render();
#if DEBUG
                        GraphicsUtil.CheckError("Rendering - 0 - Transp - Specific: " + ent.ToString());
#endif
                    }
                    GraphicsUtil.CheckError("Rendering - 0 - Transp");
                }
                else if (view.FBOid.IsMainSolid())
                {
                    GraphicsUtil.CheckError("Rendering - 0 - Pre");
                    SetEnts();
                    TheRegion.MainRender();
                    GraphicsUtil.CheckError("Rendering - 0 - Main - RegionMain");
                    List<Entity> entsRender = CVars.r_drawents.ValueB ? new List<Entity>(TheRegion.Entities) : new List<Entity>();
                    if (view.FBOid != FBOID.DYNAMIC_SHADOWS)
                    {
                        foreach (Chunk ch in TheRegion.chToRender)
                        {
                            entsRender.Add(new ChunkEntity(ch));
                        }
                    }
                    GraphicsUtil.CheckError("Rendering - 0 - Prepped");
                    Location pos = Player.GetPosition();
                    IEnumerable<Entity> ents = entsRender.OrderBy((e) => e.GetPosition().DistanceSquared(MainWorldView.RenderRelative));
                    foreach (Entity ent in ents)
                    {
                        ent.Render();
#if DEBUG
                        GraphicsUtil.CheckError("Rendering - 0 - Specific: " + ent.ToString());
#endif
                    }
                    GraphicsUtil.CheckError("Rendering - 0 - Main");
                    TheRegion.RenderPlants();
                    GraphicsUtil.CheckError("Rendering - 0 - Plants");
                }
                else if (CVars.r_drawents.ValueB)
                {
                    for (int i = 0; i < TheRegion.Entities.Count; i++)
                    {
                        TheRegion.Entities[i].Render();
                    }
                    GraphicsUtil.CheckError("Rendering - 0 - Other/Ents");
                }
                if (!transparents && view.FBOid != FBOID.DYNAMIC_SHADOWS && !view.FBOid.IsMainSolid())
                {
                    isVox = false;
                    SetVox();
                    TheRegion.Render();
                    SetEnts();
                    GraphicsUtil.CheckError("Rendering - 0 - Region");
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
                    GraphicsUtil.CheckError("Rendering - 0 - Weather");
                }
                if (MainWorldView.FBOid == FBOID.MAIN)
                {
                    Rendering.SetMinimumLight(1f, MainWorldView);
                }
                GraphicsUtil.CheckError("Rendering - 0 - EndF");
            }
            GraphicsUtil.CheckError("Rendering - 1");
            SetEnts();
            GraphicsUtil.CheckError("Rendering - 2");
            if (view.FBOid == FBOID.STATIC_SHADOWS)
            {
                return;
            }
            SetEnts();
            Textures.White.Bind();
            Location itemSource = Player.ItemSource();
            Location mov = (CameraFinalTarget - itemSource) / CameraDistance;
            Location cpos = CameraFinalTarget - (CameraImpactNormal * 0.01f);
            Location cpos2 = CameraFinalTarget + (CameraImpactNormal * 0.91f);
            GraphicsUtil.CheckError("Rendering - 2.25");
            // TODO: 5 -> Variable length (Server controlled?)
            if (TheRegion.GetBlockMaterial(cpos) != Material.AIR && CameraDistance < 5)
            {
                if (CVars.u_highlight_targetblock.ValueB)
                {
                    Location cft = cpos.GetBlockLocation();
                    GL.LineWidth(3);
                    Rendering.SetColor(Color4.Blue, MainWorldView);
                    Rendering.SetMinimumLight(1.0f, MainWorldView);
                    Rendering.RenderLineBox(cft - mov * 0.01f, cft + Location.One - mov * 0.01f, MainWorldView);
                    GL.LineWidth(1);
                    if (VR != null && VR.Right != null)
                    {
                        Rendering.SetColor(Color4.Red, MainWorldView);
                        Rendering.RenderLine(itemSource, CameraFinalTarget, MainWorldView);
                    }
                }
                GraphicsUtil.CheckError("Rendering - 2.5");
                if (CVars.u_highlight_placeblock.ValueB)
                {
                    Rendering.SetColor(Color4.Cyan, MainWorldView);
                    Rendering.SetMinimumLight(1.0f, MainWorldView);
                    Location cft2 = cpos2.GetBlockLocation();
                    Rendering.RenderLineBox(cft2, cft2 + Location.One, MainWorldView);
                }
                Rendering.SetColor(Color4.White, MainWorldView);
            }
            Rendering.SetMinimumLight(0f, MainWorldView);
            GraphicsUtil.CheckError("Rendering - 2.75");
            if (CVars.n_debugmovement.ValueB)
            {
                Rendering.SetColor(Color4.Red, MainWorldView);
                GL.LineWidth(5);
#if DEBUG
                int i = 0;
#endif
                foreach (Chunk chunk in TheRegion.LoadedChunks.Values)
                {
                    if ((chunk._VBOSolid == null || !chunk._VBOSolid.generated) && (chunk._VBOTransp == null || !chunk._VBOTransp.generated) && !chunk.IsAir)
                    {
                        Rendering.RenderLineBox(chunk.WorldPosition.ToLocation() * Chunk.CHUNK_SIZE, (chunk.WorldPosition.ToLocation() + Location.One) * Chunk.CHUNK_SIZE, MainWorldView);
#if DEBUG
                        GraphicsUtil.CheckError("Rendering - 2.8: " + i++);
#endif
                    }
                }
                Vector3i cur = TheRegion.ChunkLocFor(Player.GetPosition());
                for (int x = -2; x <= 2; x++)
                {
                    for (int y = -2; y <= 2; y++)
                    {
                        for (int z = -2; z <= 2; z++)
                        {
                            Rendering.RenderLineBox((cur + new Vector3i(x, y, z)).ToLocation() * Chunk.CHUNK_SIZE, (cur + new Vector3i(x + 1, y + 1, z + 1)).ToLocation() * Chunk.CHUNK_SIZE, MainWorldView);
                        }
                    }
                }
                GL.LineWidth(1);
                Rendering.SetColor(Color4.White, MainWorldView);
            }
            RenderVR();
            GraphicsUtil.CheckError("Rendering - 3");
            Textures.White.Bind();
            Rendering.SetMinimumLight(1, MainWorldView);
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
                                Rendering.SetColor(col, MainWorldView);
                                Rendering.RenderLine(one, two, MainWorldView);
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
                                BEPUutilities.Quaternion.GetQuaternionBetweenNormalizedVectors(ref bvec2, ref bvec, out BEPUutilities.Quaternion bquat);
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
            GraphicsUtil.CheckError("Rendering - 4");
            Rendering.SetColor(Color4.White, MainWorldView);
            Rendering.SetMinimumLight(0, MainWorldView);
            Textures.White.Bind();
            if (!view.ShadowsOnly)
            {
                GL.ActiveTexture(TextureUnit.Texture1);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.ActiveTexture(TextureUnit.Texture0);
                //Render2D(true);
            }
            GraphicsUtil.CheckError("Rendering - 5");
        }

        /// <summary>
        /// Draws a curved line in 3D space.
        /// </summary>
        void DrawCurve(Location one, Location two, Location cPoint, Color4F color)
        {
            const int curvePoints = 10;
            const double step = 1.0 / curvePoints;
            Location curvePos = one;
            for (double t = step; t <= 1.0; t += step)
            {
                Vector4 col = Rendering.AdaptColor(ClientUtilities.Convert(cPoint), color);
                Rendering.SetColor(col, MainWorldView);
                Location c2 = CalculateBezierPoint(t, one, cPoint, two);
                Rendering.RenderBilboardLine(curvePos, c2, 3, MainWorldView.CameraPos, MainWorldView);
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
        /// The format for the second FPS counter.
        /// </summary>
        const string timeformat_fps2 = "00.0";

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
            if (IsMainMenu)
            {
                return;
            }
            if (sub3d)
            {
                //GL.Disable(EnableCap.DepthTest);
                FixPersp = Matrix4.CreateOrthographicOffCenter(0, 1024, 256, 0, -(itemScale * 2f), (itemScale * 2f));
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
                    Location cpos = CameraFinalTarget - (CameraImpactNormal * 0.01f);
                    cpos = cpos.GetBlockLocation();
                    FontSets.Standard.DrawColoredText(FontSets.Standard.SplitAppropriately("^!^e^7gFPS(calc): " + (1f / gDelta).ToString(timeformat_fps2) + ", gFPS(actual): " + gFPS + ", gFPS(range): " + gFPS_Min + " to " + gFPS_Max
                        //+ "\nHeld Item: " + GetItemForSlot(QuickBarPos).ToString()
                        + "\nTimes -> Physics: " + TheRegion.PhysTime.ToString(timeformat) + ", Shadows: " + MainWorldView.ShadowTime.ToString(timeformat)
                        + ", FBO: " + MainWorldView.FBOTime.ToString(timeformat) + ", Lights: " + MainWorldView.LightsTime.ToString(timeformat) + ", 2D: " + TWODTime.ToString(timeformat)
                        + ", Tick: " + TickTime.ToString(timeformat) + ", Finish: " + FinishTime.ToString(timeformat) + ", Total: " + TotalTime.ToString(timeformat) + ", Crunch: " + TheRegion.CrunchTime.ToString(timeformat)
                        + "\nSpike Times -> Shadows: " + MainWorldView.ShadowSpikeTime.ToString(timeformat)
                        + ", FBO: " + MainWorldView.FBOSpikeTime.ToString(timeformat) + ", Lights: " + MainWorldView.LightsSpikeTime.ToString(timeformat) + ", 2D: " + TWODSpikeTime.ToString(timeformat)
                        + ", Tick: " + TickSpikeTime.ToString(timeformat) + ", Finish: " + FinishSpikeTime.ToString(timeformat) + ", Total: " + TotalSpikeTime.ToString(timeformat) + ", Crunch: " + TheRegion.CrunchSpikeTime.ToString(timeformat)
                        + "\nChunks loaded: " + TheRegion.LoadedChunks.Count + ", Chunks rendering currently: " + TheRegion.RenderingNow.Count + ", chunks waiting: " + TheRegion.NeedsRendering.Count + ", Entities loaded: " + TheRegion.Entities.Count
                        + "\nChunks prepping currently: " + TheRegion.PreppingNow.Count + ", chunks waiting for prep: " + TheRegion.PrepChunks.Count + ", SLOD chunk sets loaded: " + TheRegion.SLODs.Count + ", chunks awaiting lighting: " + TheRegion.CalcingLights.Count
                        + "\nPosition: " + Player.GetPosition().ToBasicString() + ", velocity: " + Player.GetVelocity().ToBasicString() + ", direction: " + Player.Direction.ToBasicString()
                        + "\nExposure: " + MainWorldView.MainEXP + ", Target Block: " + cpos + ", Target Block Type: " + TheRegion.GetBlockMaterial(cpos),
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
                    CWindow.Rendering2D.SetColor(Color4.Black);
                    CWindow.Rendering2D.RenderRectangle(CWindow.MainUI.UIContext, center - healthbaroffset, Window.Height - 30, center + healthbaroffset, Window.Height - 2);
                    CWindow.Rendering2D.SetColor(Color4.Red);
                    CWindow.Rendering2D.RenderRectangle(CWindow.MainUI.UIContext, center - healthbaroffset + 2, Window.Height - 28, center - (healthbaroffset - 2) * ((100 - percent) / 100), Window.Height - 4);
                    CWindow.Rendering2D.SetColor(Color4.Cyan);
                    CWindow.Rendering2D.RenderRectangle(CWindow.MainUI.UIContext, center + 2, Window.Height - 28, center + healthbaroffset - 2, Window.Height - 4); // TODO: Armor percent
                    FontSets.SlightlyBigger.DrawColoredText("^S^!^e^0Health: " + Player.Health.ToString(healthformat) + "/" + Player.MaxHealth.ToString(healthformat) + " = " + percent.ToString(healthformat) + "%",
                        new Location(center - healthbaroffset + 4, Window.Height - 26, 0));
                    FontSets.SlightlyBigger.DrawColoredText("^S^%^e^0Armor: " + "100.0" + "/" + "100.0" + " = " + "100.0" + "%", // TODO: Armor values!
                        new Location(center + 4, Window.Height - 26, 0));
                    if (CVars.u_showmap.ValueB)
                    {
                        CWindow.Rendering2D.SetColor(Color4.White);
                        Textures.Black.Bind();
                        CWindow.Rendering2D.RenderRectangle(CWindow.MainUI.UIContext, Window.Width - 16 - 200, 16, Window.Width - 16, 16 + 200); // TODO: Dynamic size?
                        GL.BindTexture(TextureTarget.Texture2D, map_fbo_texture);
                        CWindow.Rendering2D.RenderRectangle(CWindow.MainUI.UIContext, Window.Width - 16 - (200 - 2), 16 + 2, Window.Width - 16 - 2, 16 + (200 - 2));
                    }
                    int cX = Window.Width / 2;
                    int cY = Window.Height / 2;
                    int move = (int)Player.GetVelocity().LengthSquared() / 5;
                    if (move > 20)
                    {
                        move = 20;
                    }
                    CWindow.Rendering2D.SetColor(Color4.White);
                    Textures.GetTexture("ui/hud/reticles/" + CVars.u_reticle.Value + "_tl").Bind(); // TODO: Save! Don't re-grab every tick!
                    CWindow.Rendering2D.RenderRectangle(CWindow.MainUI.UIContext, cX - CVars.u_reticlescale.ValueI - move, cY - CVars.u_reticlescale.ValueI - move, cX - move, cY - move);
                    Textures.GetTexture("ui/hud/reticles/" + CVars.u_reticle.Value + "_tr").Bind();
                    CWindow.Rendering2D.RenderRectangle(CWindow.MainUI.UIContext, cX + move, cY - CVars.u_reticlescale.ValueI - move, cX + CVars.u_reticlescale.ValueI + move, cY - move);
                    Textures.GetTexture("ui/hud/reticles/" + CVars.u_reticle.Value + "_bl").Bind();
                    CWindow.Rendering2D.RenderRectangle(CWindow.MainUI.UIContext, cX - CVars.u_reticlescale.ValueI - move, cY + move, cX - move, cY + CVars.u_reticlescale.ValueI + move);
                    Textures.GetTexture("ui/hud/reticles/" + CVars.u_reticle.Value + "_br").Bind();
                    CWindow.Rendering2D.RenderRectangle(CWindow.MainUI.UIContext, cX + move, cY + move, cX + CVars.u_reticlescale.ValueI + move, cY + CVars.u_reticlescale.ValueI + move);
                    if (CVars.u_showrangefinder.ValueB)
                    {
                        FontSets.Standard.DrawColoredText(CameraDistance.ToString("0.0"), new Location(cX + move + CVars.u_reticlescale.ValueI, cY + move + CVars.u_reticlescale.ValueI, 0));
                    }
                    if (CVars.u_showcompass.ValueB)
                    {
                        Textures.White.Bind();
                        CWindow.Rendering2D.SetColor(Color4.Black);
                        CWindow.Rendering2D.RenderRectangle(CWindow.MainUI.UIContext, 64, Window.Height - (32 + 32), Window.Width - 64, Window.Height - 32);
                        CWindow.Rendering2D.SetColor(Color4.Gray);
                        CWindow.Rendering2D.RenderRectangle(CWindow.MainUI.UIContext, 66, Window.Height - (32 + 30), Window.Width - 66, Window.Height - 34);
                        CWindow.Rendering2D.SetColor(Color4.White);
                        RenderCompassCoord(Vector4d.UnitY, "N");
                        RenderCompassCoord(-Vector4d.UnitY, "S");
                        RenderCompassCoord(Vector4d.UnitX, "E");
                        RenderCompassCoord(-Vector4d.UnitX, "W");
                        RenderCompassCoord(new Vector4d(1, 1, 0, 0), "NE");
                        RenderCompassCoord(new Vector4d(1, -1, 0, 0), "SE");
                        RenderCompassCoord(new Vector4d(-1, 1, 0, 0), "NW");
                        RenderCompassCoord(new Vector4d(-1, -1, 0, 0), "SW");
                    }
                    if (!IsChatVisible())
                    {
                        ChatRenderRecent();
                    }
                }
            }
            if (sub3d)
            {
                FixPersp = Matrix4.Identity;
            }
            else
            {
                if (Loading > 0)
                {
                    RenderLoader(CDrawWidth - 100f, 100f, 100f, gDelta);
                }
            }
        }

        public int CDrawWidth;

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
        /// <param name="sub3d">Whether to render in 3D (true) or 2D (false).</param>
        public void RenderItem(ItemStack item, Location pos, int size, bool sub3d)
        {
            if (sub3d)
            {
                IsOrtho = true;
                pos.X += 512f - (Window.Width * 0.5f);
                pos.Y += 256f - (Window.Height);// - bottomup;
                item.Render3D(pos + new Location(size * 0.5f, size * 0.5f, 0f), (float)GlobalTickTimeLocal * 0.5f, new Location(size * 0.75f));
                IsOrtho = false;
                return;
            }
            GraphicsUtil.CheckError("Render Item - Pre Rendered Item");
            ItemFrame.Bind();
            CWindow.Rendering2D.SetColor(Color4.White);
            CWindow.Rendering2D.RenderRectangle(CWindow.MainUI.UIContext, (int)pos.X - 1, (int)pos.Y - 1, (int)(pos.X + size) + 1, (int)(pos.Y + size) + 1);
            item.Render(pos, new Location(size, size, 0));
            GraphicsUtil.CheckError("Render Item - Post Rendered Item");
            if (item.Count > 0)
            {
                FontSets.SlightlyBigger.DrawColoredText("^!^e^7^S" + item.Count, new Location(pos.X + 5, pos.Y + size - FontSets.SlightlyBigger.font_default.Height / 2f - 5, 0));
            }
        }

        /// <summary>
        /// What orthographic matrix is currently in use for 2D views.
        /// </summary>
        public Matrix4 Ortho;

        public void FixView(View3D view)
        {
            view.ViewPatchOne = (shadowmat_dat, light_dat, fogDist, maxLit, c) =>
            {

                s_forw_vox.Bind();
                if (CVars.r_forward_lights.ValueB)
                {
                    GL.Uniform1(15, (float)c);
                    GL.UniformMatrix4(20, View3D.LIGHTS_MAX, false, shadowmat_dat);
                    GL.UniformMatrix4(20 + View3D.LIGHTS_MAX, View3D.LIGHTS_MAX, false, light_dat);
                }
                GraphicsUtil.CheckError("Render/Fast - Uniforms 5");
                GL.UniformMatrix4(1, false, ref view.PrimaryMatrix);
                GL.UniformMatrix4(2, false, ref View3D.IdentityMatrix);
                GL.Uniform1(6, (float)Engine.GlobalTickTime);
                GraphicsUtil.CheckError("Render/Fast - Uniforms 5.1");
                // TODO: GL.Uniform1(7, AudioLevel);
                GL.Uniform4(12, new Vector4(view.FogCol.ToOpenTK(), view.FogAlpha));
                GraphicsUtil.CheckError("Render/Fast - Uniforms 5.15");
                GL.Uniform1(13, fogDist);
                //GL.Uniform2(14, zfar_rel); // ?
                Engine.Rendering.SetColor(Color4.White, view);
                GraphicsUtil.CheckError("Render/Fast - Uniforms 5.2");
                if (!Engine.Forward_Lights)
                {
                    GL.Uniform3(10, -TheSun.Direction.ToOpenTK());
                    GraphicsUtil.CheckError("Render/Fast - Uniforms 5.21");
                    GL.Uniform3(11, maxLit);
                }
                s_forw_vox_slod.Bind();
                GraphicsUtil.CheckError("Render/Fast - Uniforms 5.25");
                GL.UniformMatrix4(1, false, ref view.PrimaryMatrix);
                GL.UniformMatrix4(2, false, ref View3D.IdentityMatrix);
                GL.Uniform1(6, (float)Engine.GlobalTickTime);
                GL.Uniform4(12, new Vector4(view.FogCol.ToOpenTK(), view.FogAlpha));
                GL.Uniform1(13, fogDist);
                //GL.Uniform2(14, zfar_rel); // ?
                Engine.Rendering.SetColor(Color4.White, view);
                GL.Uniform3(10, -TheSun.Direction.ToOpenTK());
                GL.Uniform3(11, maxLit);
            };
            view.ViewPatchTwo = () =>
            {
                s_forw_vox = s_forw_vox.Bind();
                GL.UniformMatrix4(1, false, ref view.PrimaryMatrix_OffsetFor3D);
            };
            view.ViewPatchThree = (fogDist, shadowmat_dat, light_dat, c) =>
            {
                s_forw_vox_trans.Bind();
                GL.UniformMatrix4(1, false, ref view.PrimaryMatrix);
                GL.UniformMatrix4(2, false, ref View3D.IdentityMatrix);
                GL.Uniform1(6, (float)Engine.GlobalTickTime);
                // TODO: GL.Uniform1(7, AudioLevel);
                GL.Uniform4(12, new Vector4(view.FogCol.ToOpenTK(), view.FogAlpha));
                GL.Uniform1(13, fogDist);
                // GL.Uniform2(14, zfar_rel); // ?
                Engine.Rendering.SetColor(Color4.White, view);
                if (Engine.Forward_Lights)
                {
                    GL.Uniform1(15, (float)c);
                    GL.UniformMatrix4(20, View3D.LIGHTS_MAX, false, shadowmat_dat);
                    GL.UniformMatrix4(20 + View3D.LIGHTS_MAX, View3D.LIGHTS_MAX, false, light_dat);
                }
            };
            view.ViewPatchFour = () =>
            {
                s_forw_vox_trans = s_forw_vox_trans.Bind();
                GL.UniformMatrix4(1, false, ref view.PrimaryMatrix_OffsetFor3D);
            };
            view.ViewPatchFive = () =>
            {
                FixPersp = Matrix4.Identity;
            };
            view.ViewPatchSix = (i, x) =>
            {
                s_shadowvox = s_shadowvox.Bind();
                view.SetMatrix(2, Matrix4d.Identity);
                view.Lights[i].InternalLights[x].SetProj(view);
                GL.Uniform1(5, (view.Lights[i].InternalLights[x] is LightOrtho) ? 1.0f : 0.0f);
                GL.Uniform1(4, view.Lights[i].InternalLights[x].transp ? 1.0f : 0.0f);
            };
            view.ViewPatchSeven = () =>
            {
                s_fbov = s_fbov.Bind();
                GraphicsUtil.CheckError("Render - GBuffer - Uniforms - 0");
                GL.Uniform1(6, (float)GlobalTickTimeLocal);
                GL.UniformMatrix4(1, false, ref view.PrimaryMatrix);
                GL.UniformMatrix4(2, false, ref View3D.IdentityMatrix);
                // TODO: GL.Uniform1(7, AudioLevel);
                GL.Uniform2(8, new Vector2(sl_min, sl_max));
                s_fbovslod = s_fbovslod.Bind();
                GraphicsUtil.CheckError("Render - GBuffer - Uniforms - 0.5");
                GL.Uniform1(6, (float)GlobalTickTimeLocal);
                GL.UniformMatrix4(1, false, ref view.PrimaryMatrix);
                GL.UniformMatrix4(2, false, ref View3D.IdentityMatrix);
                GL.Uniform2(8, new Vector2(sl_min, sl_max));
                GraphicsUtil.CheckError("Render - GBuffer - Uniforms - 1");
            };
            view.ViewPatchEight = () =>
            {
                s_fbov = s_fbov.Bind();
                GL.UniformMatrix4(1, false, ref view.PrimaryMatrix_OffsetFor3D);
            };
            view.ViewPatchNine = () =>
            {
                s_fbov_refract = s_fbov_refract.Bind();
                GL.Uniform1(6, (float)GlobalTickTimeLocal);
                GL.UniformMatrix4(1, false, ref view.PrimaryMatrix);
                GL.UniformMatrix4(2, false, ref View3D.IdentityMatrix);
                GL.Uniform2(8, new Vector2(sl_min, sl_max));
            };
            view.ViewPatchTen = () =>
            {
                s_fbov_refract = s_fbov_refract.Bind();
                GL.UniformMatrix4(1, false, ref view.PrimaryMatrix_OffsetFor3D);
            };
            view.ViewPatchEleven = () =>
            {
                if (CVars.r_transplighting.ValueB)
                {
                    if (CVars.r_transpshadows.ValueB && CVars.r_shadows.ValueB)
                    {
                        if (Engine.AllowLL)
                        {
                            s_transponlyvoxlitsh_ll = s_transponlyvoxlitsh_ll.Bind();
                        }
                        else
                        {
                            s_transponlyvoxlitsh = s_transponlyvoxlitsh.Bind();
                        }
                    }
                    else
                    {
                        if (Engine.AllowLL)
                        {
                            s_transponlyvoxlit_ll = s_transponlyvoxlit_ll.Bind();
                        }
                        else
                        {
                            s_transponlyvoxlit = s_transponlyvoxlit.Bind();
                        }
                    }
                }
                else
                {
                    if (Engine.AllowLL)
                    {
                        s_transponlyvox_ll = s_transponlyvox_ll.Bind();
                    }
                    else
                    {
                        s_transponlyvox = s_transponlyvox.Bind();
                    }
                }
                GL.UniformMatrix4(1, false, ref view.PrimaryMatrix);
                GL.UniformMatrix4(2, false, ref View3D.IdentityMatrix);
                GL.Uniform1(4, view.DesaturationAmount);
            };
            view.ViewPatchTwelve = () =>
            {
                if (CVars.r_transplighting.ValueB)
                {
                    if (CVars.r_transpshadows.ValueB && CVars.r_shadows.ValueB)
                    {
                        if (Engine.AllowLL)
                        {
                            s_transponlyvoxlitsh_ll = s_transponlyvoxlitsh_ll.Bind();
                        }
                        else
                        {
                            s_transponlyvoxlitsh = s_transponlyvoxlitsh.Bind();
                        }
                    }
                    else
                    {
                        if (Engine.AllowLL)
                        {
                            s_transponlyvoxlit_ll = s_transponlyvoxlit_ll.Bind();
                        }
                        else
                        {
                            s_transponlyvoxlit = s_transponlyvoxlit.Bind();
                        }
                    }
                }
                else
                {
                    if (Engine.AllowLL)
                    {
                        s_transponlyvox_ll = s_transponlyvox_ll.Bind();
                    }
                    else
                    {
                        s_transponlyvox = s_transponlyvox.Bind();
                    }
                }
                GL.UniformMatrix4(1, false, ref view.PrimaryMatrix_OffsetFor3D);
            };
            view.ViewPatchThirteen = (mat_lhelp, s_mats, l_dats1) =>
            {
                if (CVars.r_transpshadows.ValueB && CVars.r_shadows.ValueB)
                {
                    if (CVars.r_transpll.ValueB)
                    {
                        s_transponlyvoxlitsh_ll = s_transponlyvoxlitsh_ll.Bind();
                    }
                    else
                    {
                        s_transponlyvoxlitsh = s_transponlyvoxlitsh.Bind();
                    }
                }
                else
                {
                    if (Engine.AllowLL)
                    {
                        s_transponlyvoxlit_ll = s_transponlyvoxlit_ll.Bind();
                    }
                    else
                    {
                        s_transponlyvoxlit = s_transponlyvoxlit.Bind();
                    }
                }
                GL.UniformMatrix4(2, false, ref View3D.IdentityMatrix);
                GL.Uniform1(6, (float)GlobalTickTimeLocal);
                // TODO: GL.Uniform1(7, AudioLevel);
                GL.Uniform2(8, new Vector2(view.Width, view.Height));
                GL.UniformMatrix4(9, false, ref mat_lhelp);
                GL.UniformMatrix4(20, View3D.LIGHTS_MAX, false, s_mats);
                GL.UniformMatrix4(20 + View3D.LIGHTS_MAX, View3D.LIGHTS_MAX, false, l_dats1);
            };
            view.ViewPatchFourteen = (matabc) =>
            {
                s_transponlyvox_ll.Bind();
                // GL.UniformMatrix4(1, false, ref combined);
                GL.UniformMatrix4(2, false, ref View3D.IdentityMatrix);
                GL.UniformMatrix4(9, false, ref matabc);
            };
            view.ViewPatchFifteen = () =>
            {
                s_transponlyvox.Bind();
                //GL.UniformMatrix4(1, false, ref combined);
                GL.UniformMatrix4(2, false, ref View3D.IdentityMatrix);
            };
        }
    }
}
