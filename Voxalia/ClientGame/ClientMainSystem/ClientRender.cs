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
            View3D.CheckError("Load - Rendering - Shaders");
            GenerateMapHelpers();
            GenerateGrassHelpers();
            PrepDecals();
            View3D.CheckError("Load - Rendering - Map/Grass");
            MainWorldView.ShadowingAllowed = true;
            MainWorldView.ShadowTexSize = () => CVars.r_shadowquality.ValueI;
            MainWorldView.Render3D = Render3D;
            MainWorldView.DecalRender = RenderDecal;
            MainWorldView.PostFirstRender = ReverseEntitiesOrder;
            MainWorldView.LLActive = CVars.r_transpll.ValueB; // TODO: CVar edit call back
            View3D.CheckError("Load - Rendering - Settings");
            MainWorldView.Generate(this, Window.Width, Window.Height);
            View3D.CheckError("Load - Rendering - ViewGen");
            ItemBarView.FastOnly = true;
            ItemBarView.ClearColor = new float[] { 1f, 1f, 1f, 1f };
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
            ItemBarView.Generate(this, 1024, 256);
            // TODO: Use the item bar in VR mode.
            View3D.CheckError("Load - Rendering - Item Bar");
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
            View3D.CheckError("Load - Rendering - Sky Prep");
            for (int i = 0; i < 6; i++)
            {
                skybox[i].GenerateVBO();
            }
            RainCyl = Models.GetModel("raincyl");
            RainCyl.LoadSkin(Textures);
            SnowCyl = Models.GetModel("snowcyl");
            SnowCyl.LoadSkin(Textures);
            View3D.CheckError("Load - Rendering - Final");
            TWOD_FBO = GL.GenFramebuffer();
            TWOD_FBO_Tex = GL.GenTexture();
            TWOD_FixTexture();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, TWOD_FBO);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, TWOD_FBO_Tex, 0);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
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
            s_shadowadder_ssao = Shaders.GetShader("lightadder" + def + ",MCM_SHADOWS,MCM_SSAO");
            s_lightadder_ssao = Shaders.GetShader("lightadder" + def + ",MCM_SSAO");
            s_transponly = Shaders.GetShader("transponly" + def);
            s_transponlyvox = Shaders.GetShader("transponlyvox" + def);
            s_transponlylit = Shaders.GetShader("transponly" + def + ",MCM_LIT");
            s_transponlyvoxlit = Shaders.GetShader("transponlyvox" + def + ",MCM_LIT");
            s_transponlylitsh = Shaders.GetShader("transponly" + def + ",MCM_LIT,MCM_SHADOWS");
            s_transponlyvoxlitsh = Shaders.GetShader("transponlyvox" + def + ",MCM_LIT,MCM_SHADOWS");
            s_godray = Shaders.GetShader("godray" + def);
            s_map = Shaders.GetShader("map" + def);
            s_mapvox = Shaders.GetShader("map" + def + ",MCM_VOX");
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
            s_shadow_grass = Shaders.GetShader("shadow" + def + ",MCM_GEOM_ACTIVE,MCM_PRETTY,MCM_SHADOWS?grass");
            s_forw_particles = Shaders.GetShader("forward" + def + ",MCM_GEOM_ACTIVE,MCM_TRANSP,MCM_BRIGHT,MCM_NO_ALPHA_CAP,MCM_FADE_DEPTH?particles");
            s_fbodecal = Shaders.GetShader("fbo" + def + ",MCM_INVERSE_FADE,MCM_NO_ALPHA_CAP,MCM_GEOM_ACTIVE,MCM_PRETTY?decal");
            s_forwdecal = Shaders.GetShader("forward" + def + ",MCM_INVERSE_FADE,MCM_NO_ALPHA_CAP,MCM_GEOM_ACTIVE?decal");
            s_forwt = Shaders.GetShader("forward" + def + ",MCM_NO_ALPHA_CAP,MCM_BRIGHT");
            s_transponly_particles = Shaders.GetShader("transponly" + def + ",MCM_ANY,MCM_GEOM_ACTIVE,MCM_PRETTY,MCM_FADE_DEPTH?particles");
            s_transponlylit_particles = Shaders.GetShader("transponly" + def + ",MCM_LIT,MCM_ANY,MCM_GEOM_ACTIVE,MCM_PRETTY,MCM_FADE_DEPTH?particles");
            s_transponlylitsh_particles = Shaders.GetShader("transponly" + def + ",MCM_LIT,MCM_SHADOWS,MCM_ANY,MCM_GEOM_ACTIVE,MCM_PRETTY,MCM_FADE_DEPTH?particles");
            s_transponly_ll_particles = Shaders.GetShader("transponly" + def + ",MCM_LL,MCM_ANY,MCM_GEOM_ACTIVE,MCM_PRETTY,MCM_FADE_DEPTH?particles");
            s_transponlylit_ll_particles = Shaders.GetShader("transponly" + def + ",MCM_LIT,MCM_LL,MCM_ANY,MCM_GEOM_ACTIVE,MCM_PRETTY,MCM_FADE_DEPTH?particles");
            s_transponlylitsh_ll_particles = Shaders.GetShader("transponly" + def + ",MCM_LIT,MCM_SHADOWS,MCM_LL,MCM_ANY,MCM_GEOM_ACTIVE,MCM_PRETTY,MCM_FADE_DEPTH?particles");
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
        /// The Shadow Pass shader.
        /// </summary>
        public Shader s_shadow;

        /// <summary>
        /// The Shadow Pass shader, for voxels.
        /// </summary>
        public Shader s_shadowvox;

        /// <summary>
        /// The Shadow Pass shader, for grass.
        /// </summary>
        public Shader s_shadow_grass;

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
        /// The shader that adds shadowed lights to a scene, with SSAO.
        /// </summary>
        public Shader s_shadowadder_ssao;

        /// <summary>
        /// The shader that adds lights to a scene, with SSAO.
        /// </summary>
        public Shader s_lightadder_ssao;

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
        /// The shader used for grass-sprites in deferred rendering mode.
        /// </summary>
        public Shader s_fbo_grass;

        /// <summary>
        /// The shader used for particles in forward rendering mode.
        /// </summary>
        public Shader s_forw_particles;

        /// <summary>
        /// The shader used for decal rendering in deferred rendering mode.
        /// </summary>
        public Shader s_fbodecal;
        
        /// <summary>
        /// The shader used for decal rendering in forward mode.
        /// </summary>
        public Shader s_forwdecal;

        /// <summary>
        /// The shader used for all-transparency rendering in forward mode (primarily the skybox).
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
        public bool shouldRedrawShadows = false;

        /// <summary>
        /// How many things the game is currently loading.
        /// </summary>
        public int Loading = 0;

        /// <summary>
        /// Draws the entire 2D environment.
        /// </summary>
        public void Draw2DEnv()
        {
            CScreen.FullRender(gDelta, 0, 0);
        }

        /// <summary>
        /// The main entry point for the render and tick cycles.
        /// </summary>
        public void Window_RenderFrame(object sender, FrameEventArgs e)
        {
            lock (TickLock)
            {
                gDelta = e.Time;
                int gfps = (int)(1.0 / gDelta);
                gFPS_Min = gFPS_Min == 0 ? gfps : Math.Min(gFPS_Min, gfps);
                gFPS_Max = Math.Max(gFPS_Max, gfps);
                gTicks++;
                View3D.CheckError("RenderFrame - Start");
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
                            View3D.CheckError("ItemBarRender");
                            TWOD_CFrame = 0;
                            Establish2D();
                            View3D.CheckError("RenderFrame - Establish");
                            GL.Disable(EnableCap.CullFace);
                            GL.BindFramebuffer(FramebufferTarget.Framebuffer, TWOD_FBO);
                            GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
                            GL.ClearBuffer(ClearBuffer.Color, 0, new float[] { 0, 0, 0, 0 });
                            Shaders.ColorMultShader.Bind();
                            GL.Uniform1(6, (float)GlobalTickTimeLocal);
                            View3D.CheckError("RenderFrame - Setup2D");
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
                            View3D.CheckError("RenderFrame - 2DEnv");
                            UIConsole.Draw();
                        }
                        View3D.CheckError("RenderFrame - Basic");
                        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                        GL.DrawBuffer(DrawBufferMode.Back);
                        Shaders.ColorMultShader.Bind();
                        Rendering.SetColor(Vector4.One);
                        GL.Disable(EnableCap.DepthTest);
                        GL.Disable(EnableCap.CullFace);
                        if (VR != null)
                        {
                            GL.UniformMatrix4(1, false, ref MainWorldView.SimpleOrthoMatrix);
                            GL.UniformMatrix4(2, false, ref View3D.IdentityMatrix);
                            GL.BindTexture(TextureTarget.Texture2D, MainWorldView.CurrentFBOTexture);
                            Rendering.RenderRectangle(-1, -1, 1, 1);
                        }
                        View3D.CheckError("RenderFrame - VR");
                        GL.BindTexture(TextureTarget.Texture2D, TWOD_FBO_Tex);
                        Matrix4 ortho = Matrix4.CreateOrthographicOffCenter(0, Window.Width, 0, Window.Height, -1, 1);
                        GL.UniformMatrix4(40, false, ref View3D.IdentityMatrix);
                        GL.UniformMatrix4(1, false, ref ortho);
                        Rendering.RenderRectangle(0, 0, Window.Width, Window.Height);
                        View3D.CheckError("RenderFrame - TWOD");
                        GL.BindTexture(TextureTarget.Texture2D, 0);
                        GL.Enable(EnableCap.CullFace);
                        GL.Enable(EnableCap.DepthTest);
                    }
                    catch (Exception ex)
                    {
                        SysConsole.Output(OutputType.ERROR, "Rendering (general): " + ex.ToString());
                    }
                }
                View3D.CheckError("PreTick");
                Stopwatch timer = new Stopwatch();
                try
                {
                    timer.Start();
                    tick(e.Time);
                    View3D.CheckError("Tick");
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
                    GL.BindTexture(TextureTarget.Texture2DArray, 0);
                    s_map = s_map.Bind();
                    GL.UniformMatrix4(View3D.MAT_LOC_VIEW, false, ref ortho);
                    MainWorldView.SetMatrix(View3D.MAT_LOC_OBJECT, oident);
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
            return 2500f;
        }

        /// <summary>
        /// How far the day sky is away from the camera.
        /// </summary>
        public float GetSkyDistance()
        {
            return 2000f;
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
            GL.UniformMatrix4(1, false, ref MainWorldView.OutViewMatrix);
            float skyAlpha = (float)Math.Max(Math.Min((SunAngle.Pitch - 70.0) / (-90.0), 1.0), 0.06);
            GL.ActiveTexture(TextureUnit.Texture3);
            Textures.Black.Bind();
            GL.ActiveTexture(TextureUnit.Texture2);
            Textures.Black.Bind();
            GL.ActiveTexture(TextureUnit.Texture1);
            Textures.NormalDef.Bind();
            GL.ActiveTexture(TextureUnit.Texture0);
            Rendering.SetMinimumLight(1.0f);
            Rendering.SetColor(new Vector4(ClientUtilities.Convert(Location.One * 1.6f), 1)); // TODO: 1.6 -> Externally defined constant. SunLightMod? Also, verify this value is used properly!
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
            float spf = 600f;
            Textures.GetTexture("skies/sun").Bind(); // TODO: Store var!
            Matrix4 rot = Matrix4.CreateTranslation(-spf * 0.5f, -spf * 0.5f, 0f)
                * Matrix4.CreateRotationY((float)((-SunAngle.Pitch - 90f) * Utilities.PI180))
                * Matrix4.CreateRotationZ((float)((180f + SunAngle.Yaw) * Utilities.PI180))
                * Matrix4.CreateTranslation(ClientUtilities.Convert(TheSun.Direction * -(GetSkyDistance() * 0.96f)));
            Rendering.RenderRectangle(0, 0, spf, spf, rot); // TODO: Adjust scale based on view rad
            Textures.GetTexture("skies/planet").Bind(); // TODO: Store var!
            float ppf = 1000f;
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
            GL.UniformMatrix4(1, false, ref MainWorldView.PrimaryMatrix);
            SetVox();
            GL.UniformMatrix4(1, false, ref MainWorldView.OutViewMatrix);
            foreach (ChunkSLODHelper ch in TheRegion.SLODs.Values)
            {
                // TODO: 3 -> constants
                BEPUutilities.Vector3 min = ch.Coordinate.ToVector3() * Chunk.CHUNK_SIZE * 3;
                BEPUutilities.Vector3 helpervec = new BEPUutilities.Vector3(Chunk.CHUNK_SIZE * 3, Chunk.CHUNK_SIZE * 3, Chunk.CHUNK_SIZE * 3) * 5;
                if ((MainWorldView.CFrust == null || MainWorldView.LongFrustum == null || MainWorldView.LongFrustum.ContainsBox(min - helpervec, min + helpervec)))
                {
                    ch.Render();
                }
            }
            GL.UniformMatrix4(1, false, ref MainWorldView.PrimaryMatrix);
            GL.ClearBuffer(ClearBuffer.Depth, 0, new float[] { 1f });
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
            }
            else if (MainWorldView.FBOid == FBOID.FORWARD_TRANSP)
            {
                s_forw_vox_trans = s_forw_vox_trans.Bind();
                GL.BindTexture(TextureTarget.Texture2DArray, TBlock.TextureID);
            }
            else if (MainWorldView.FBOid == FBOID.SHADOWS || MainWorldView.FBOid == FBOID.STATIC_SHADOWS || MainWorldView.FBOid == FBOID.DYNAMIC_SHADOWS)
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
            else if (MainWorldView.FBOid == FBOID.FORWARD_EXTRAS)
            {
                s_forwdecal = s_forwdecal.Bind();
                GL.BindTexture(TextureTarget.Texture2DArray, 0);
            }
            else if (MainWorldView.FBOid == FBOID.MAIN_EXTRAS)
            {
                s_fbodecal = s_fbodecal.Bind();
                GL.BindTexture(TextureTarget.Texture2DArray, 0);
            }
            else if (MainWorldView.FBOid == FBOID.SHADOWS || MainWorldView.FBOid == FBOID.STATIC_SHADOWS || MainWorldView.FBOid == FBOID.DYNAMIC_SHADOWS)
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
            View3D.CheckError("Rendering - VR");
        }

        public int Dec_VAO = -1;
        public int Dec_VBO_Pos = -1;
        public int Dec_VBO_Nrm = -1;
        public int Dec_VBO_Ind = -1;
        public int Dec_VBO_Col = -1;
        public int Dec_VBO_Tcs = -1;
        public int DecTextureID = -1;

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
                    GL.BindTexture(TextureTarget.Texture2DArray, 0);
                    return i;
                }
            }
            // TODO: Delete any unused entry findable in favor of this new one.
            return 0;
        }

        public List<Tuple<Location, Vector3, Vector4, Vector2, double>> Decals = new List<Tuple<Location, Vector3, Vector4, Vector2, double>>();

        public void AddDecal(Location pos, Location ang, Vector4 color, float scale, string texture, double time)
        {
            Decals.Add(new Tuple<Location, Vector3, Vector4, Vector2, double>(pos, ClientUtilities.Convert(ang), color, new Vector2(scale, DecalGetTextureID(texture)), time));
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
            bool isMore = pDecals != Decals.Count;
            pDecals = Decals.Count;
            if (Decals.Count == 0)
            {
                return;
            }
            // TODO: Expiration goes here. Set isMore true if any expired.
            //GL.PolygonOffset(-1, -2);
            GL.Disable(EnableCap.CullFace);
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
                    pos[i] = ClientUtilities.Convert(Decals[i].Item1 - view.RenderRelative);
                    nrm[i] = Decals[i].Item2;
                    col[i] = Decals[i].Item3;
                    tcs[i] = Decals[i].Item4;
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
                TheClient.Particles.Engine.Render(DistMin, DistMax);
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
        }

        /// <summary>
        /// Renders the 3D world upon instruction from the internal view render code.
        /// </summary>
        /// <param name="view">The view to render.</param>
        public void Render3D(View3D view)
        {
            bool transparents = MainWorldView.FBOid.IsMainTransp() || MainWorldView.FBOid == FBOID.FORWARD_TRANSP;
            GL.Enable(EnableCap.CullFace);
            if (view.ShadowsOnly)
            {
                if (view.FBOid != FBOID.STATIC_SHADOWS)
                {
                    for (int i = 0; i < TheRegion.ShadowCasters.Count; i++)
                    {
                        if (view.FBOid == FBOID.DYNAMIC_SHADOWS && ((TheRegion.ShadowCasters[i] as PhysicsEntity)?.GenBlockShadows).GetValueOrDefault(false))
                        {
                            continue;
                        }
                        TheRegion.ShadowCasters[i].Render();
                    }
                }
                else
                {
                    for (int i = 0; i < TheRegion.GenShadowCasters.Length; i++)
                    {
                        TheRegion.GenShadowCasters[i].Render();
                    }
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
                if (transparents)
                {
                    List<Entity> entsRender = CVars.r_drawents.ValueB ? new List<Entity>(TheRegion.Entities) : new List<Entity>();
                    foreach (Chunk ch in TheRegion.chToRender)
                    {
                        entsRender.Add(new ChunkEntity(ch));
                    }
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
                        entsRender.Add(new ParticlesEntity(TheRegion) { DistMin = 40, DistMax = 100 }); // TODO: 100 -> particles view render distance!
                    }
                    Location pos = Player.GetPosition();
                    IEnumerable<Entity> ents = entsRender.OrderBy((e) => e.GetPosition().DistanceSquared(MainWorldView.RenderRelative)).Reverse();
                    foreach (Entity ent in ents)
                    {
                        ent.Render();
                    }
                }
                else if (CVars.r_drawents.ValueB)
                {
                    for (int i = 0; i < TheRegion.Entities.Count; i++)
                    {
                        TheRegion.Entities[i].Render();
                    }
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
            }
            View3D.CheckError("Rendering - 1");
            SetEnts();
            if (!transparents)
            {
                if (view.FBOid != FBOID.DYNAMIC_SHADOWS)
                {
                    isVox = false;
                    SetVox();
                    TheRegion.Render();
                    SetEnts();
                }
                TheRegion.RenderPlants();
            }
            View3D.CheckError("Rendering - 2");
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
            View3D.CheckError("Rendering - 2.25");
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
                View3D.CheckError("Rendering - 2.5");
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
            View3D.CheckError("Rendering - 2.75");
            if (CVars.n_debugmovement.ValueB)
            {
                Rendering.SetColor(Color4.Red);
                GL.LineWidth(5);
                int i = 0;
                foreach (Chunk chunk in TheRegion.LoadedChunks.Values)
                {
                    if ((chunk._VBOSolid == null || !chunk._VBOSolid.generated) && (chunk._VBOTransp == null || !chunk._VBOTransp.generated) && !chunk.IsAir)
                    {
                        Rendering.RenderLineBox(chunk.WorldPosition.ToLocation() * Chunk.CHUNK_SIZE, (chunk.WorldPosition.ToLocation() + Location.One) * Chunk.CHUNK_SIZE);
                        View3D.CheckError("Rendering - 2.8: " + i++);
                    }
                }
                GL.LineWidth(1);
                Rendering.SetColor(Color4.White);
            }
            RenderVR();
            View3D.CheckError("Rendering - 3");
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
            View3D.CheckError("Rendering - 4");
            Rendering.SetColor(Color4.White);
            Rendering.SetMinimumLight(0);
            Textures.White.Bind();
            if (!view.ShadowsOnly)
            {
                GL.ActiveTexture(TextureUnit.Texture1);
                GL.BindTexture(TextureTarget.Texture2D, 0);
                GL.ActiveTexture(TextureUnit.Texture0);
                //Render2D(true);
            }
            View3D.CheckError("Rendering - 5");
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
                    FontSets.Standard.DrawColoredText(FontSets.Standard.SplitAppropriately("^!^e^7gFPS(calc): " + (1f / gDelta).ToString(timeformat_fps2) + ", gFPS(actual): " + gFPS + ", gFPS(range): " + gFPS_Min + " to " + gFPS_Max
                        + "\nHeld Item: " + GetItemForSlot(QuickBarPos).ToString()
                        + "\nTimes -> Physics: " + TheRegion.PhysTime.ToString(timeformat) + ", Shadows: " + MainWorldView.ShadowTime.ToString(timeformat)
                        + ", FBO: " + MainWorldView.FBOTime.ToString(timeformat) + ", Lights: " + MainWorldView.LightsTime.ToString(timeformat) + ", 2D: " + TWODTime.ToString(timeformat)
                        + ", Tick: " + TickTime.ToString(timeformat) + ", Finish: " + FinishTime.ToString(timeformat) + ", Total: " + TotalTime.ToString(timeformat)
                        + "\nSpike Times -> Shadows: " + MainWorldView.ShadowSpikeTime.ToString(timeformat)
                        + ", FBO: " + MainWorldView.FBOSpikeTime.ToString(timeformat) + ", Lights: " + MainWorldView.LightsSpikeTime.ToString(timeformat) + ", 2D: " + TWODSpikeTime.ToString(timeformat)
                        + ", Tick: " + TickSpikeTime.ToString(timeformat) + ", Finish: " + FinishSpikeTime.ToString(timeformat) + ", Total: " + TotalSpikeTime.ToString(timeformat)
                        + "\nChunks loaded: " + TheRegion.LoadedChunks.Count + ", Chunks rendering currently: " + TheRegion.RenderingNow.Count + ", chunks waiting: " + TheRegion.NeedsRendering.Count + ", Entities loaded: " + TheRegion.Entities.Count
                        + "\nChunks prepping currently: " + TheRegion.PreppingNow.Count + ", chunks waiting for prep: " + TheRegion.PrepChunks.Count + ", SLOD chunk sets loaded: " + TheRegion.SLODs.Count
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
                item.Render3D(pos+ new Location(size * 0.5f, size * 0.5f, 0f), (float)GlobalTickTimeLocal * 0.5f, new Location(size * 0.75f));
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
