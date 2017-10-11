//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using Voxalia.Shared;
using Voxalia.ClientGame.EntitySystem;
using BEPUutilities;
using Voxalia.ClientGame.WorldSystem;
using Voxalia.ClientGame.OtherSystems;
using Voxalia.Shared.Collision;
using Voxalia.ClientGame.GraphicsSystems;
using FreneticGameCore;
using FreneticGameCore.Collision;
using FreneticGameGraphics.LightingSystem;
using FreneticGameGraphics.GraphicsHelpers;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;

namespace Voxalia.ClientGame.ClientMainSystem
{
    public partial class Client
    {
        /// <summary>
        /// The "Sun" light source.
        /// </summary>
        public SkyLight TheSun = null;
        
        /// <summary>
        /// The "Planet" light source.
        /// </summary>
        public SkyLight ThePlanet = null;

        /// <summary>
        /// The "Sun -> Clouds" light source, for enhanced shadow effects.
        /// </summary>
        public SkyLight TheSunClouds = null;

        // Note: the client only has one region loaded at any given time.
        public Region TheRegion = null;

        /// <summary>
        /// How much light the sun should cast.
        /// </summary>
        public const float SunLightMod = 1.5f;

        /// <summary>
        /// How much light the sun shines with when looked directly at.
        /// </summary>
        public const float SunLightModDirect = SunLightMod * 2.0f;

        /// <summary>
        /// The light value (color + strength) the "sun" light source casts.
        /// </summary>
        public Location SunLightDef = Location.One * SunLightMod * 0.5;

        /// <summary>
        /// The light value (color + strength) the "sun -> clouds" light source casts.
        /// </summary>
        public Location CloudSunLightDef = Location.One * SunLightMod * 0.5;

        /// <summary>
        /// The light value (color + strength) the "planet" light source casts.
        /// </summary>
        public Location PlanetLightDef = new Location(0.75, 0.3, 0) * 0.25f;

        /// <summary>
        /// Builds the region data and populates it with minimal data.
        /// </summary>
        public void BuildWorld()
        {
            // TODO: DESTROY OLD REGION!?
            if (TheRegion != null)
            {
                foreach (ChunkSLODHelper sloddy in TheRegion.SLODs.Values)
                {
                    if (sloddy._VBO != null)
                    {
                        sloddy._VBO.Destroy();
                    }
                }
            }
            BuildLightsForWorld();
            TheRegion = new Region() { TheClient = this };
            TheRegion.BuildWorld();
            Player = new PlayerEntity(TheRegion);
            TheRegion.SpawnEntity(Player);
            MainWorldView.CameraUp = Player.UpDir;
        }

        /// <summary>
        /// Builds or rebuilds the the light sources for the world.
        /// TODO: Call this whenenver render distance changes!
        /// </summary>
        public void BuildLightsForWorld()
        {
            if (TheSun != null)
            {
                TheSun.Destroy();
                MainWorldView.Lights.Remove(TheSun);
                TheSunClouds.Destroy();
                MainWorldView.Lights.Remove(TheSunClouds);
                ThePlanet.Destroy();
                MainWorldView.Lights.Remove(ThePlanet);
            }
            GraphicsUtil.CheckError("Load - World - Deletes");
            int wid = CVars.r_shadowquality.ValueI;
            TheSun = new SkyLight(Location.Zero, MaximumStraightBlockDistance() * 2, SunLightDef, new Location(0, 0, -1), MaximumStraightBlockDistance() * 2 + Chunk.CHUNK_SIZE * 2, false, wid);
            MainWorldView.Lights.Add(TheSun);
            GraphicsUtil.CheckError("Load - World - Sun");
            // TODO: Separate cloud quality CVar?
            TheSunClouds = new SkyLight(Location.Zero, MaximumStraightBlockDistance() * 2, CloudSunLightDef, new Location(0, 0, -1), MaximumStraightBlockDistance() * 2 + Chunk.CHUNK_SIZE * 2, true, wid);
            MainWorldView.Lights.Add(TheSunClouds);
            GraphicsUtil.CheckError("Load - World - Clouds");
            // TODO: Separate planet quality CVar?
            ThePlanet = new SkyLight(Location.Zero, MaximumStraightBlockDistance() * 2, PlanetLightDef, new Location(0, 0, -1), MaximumStraightBlockDistance() * 2 + Chunk.CHUNK_SIZE * 2, false, wid);
            MainWorldView.Lights.Add(ThePlanet);
            GraphicsUtil.CheckError("Load - World - Planet");
            OnCloudShadowChanged(null, null);
            GraphicsUtil.CheckError("Load - World - Changed");
        }

        /// <summary>
        /// Called automatically when the cloud shadow CVar is changed to update that.
        /// </summary>
        public void OnCloudShadowChanged(object obj, EventArgs e)
        {
            bool cloudsready = MainWorldView.Lights.Contains(TheSunClouds);
            if (cloudsready && !CVars.r_cloudshadows.ValueB)
            {
                MainWorldView.Lights.Remove(TheSunClouds);
                SunLightDef = Location.One * SunLightMod;
            }
            else if (!cloudsready && CVars.r_cloudshadows.ValueB)
            {
                MainWorldView.Lights.Add(TheSunClouds);
                SunLightDef = Location.One * SunLightMod * 0.5;
            }
        }

        /// <summary>
        /// What angle the sun is currently at.
        /// </summary>
        public Location SunAngle = new Location(0, -75, 0);

        /// <summary>
        /// What angle the planet is currently at.
        /// </summary>
        public Location PlanetAngle = new Location(0, -56, 90);

        /// <summary>
        /// The current light value of the planet light source.
        /// </summary>
        public float PlanetLight = 1;

        /// <summary>
        /// The calculated distance between the planet and sun, for lighting purposes.
        /// </summary>
        public float PlanetSunDist = 0;

        /// <summary>
        /// The base most ambient light value.
        /// </summary>
        public Location BaseAmbient = new Location(0.1, 0.1, 0.1);

        /// <summary>
        /// Calculated minimum sunlight.
        /// </summary>
        public float sl_min = 0;

        /// <summary>
        /// Calculated maximum sunlight.
        /// </summary>
        public float sl_max = 1;

        /// <summary>
        /// The 3D vector direction of the planet.
        /// </summary>
        Location PlanetDir;

        /// <summary>
        /// Aproximate default sky color.
        /// </summary>
        public static readonly Location SkyApproxColDefault = new Location(0.5, 0.8, 1.0);

        /// <summary>
        /// The current approximate color of the sky.
        /// </summary>
        public Location SkyColor = SkyApproxColDefault;

        public Vector3i SunChunkPos = new Vector3i(int.MaxValue, int.MaxValue, int.MaxValue);

        private Location pSunAng = Location.Zero;

        public double timeSinceSkyPatch = 0;

        /// <summary>
        /// Ticks the region, including all primary calculations and lighting updates.
        /// </summary>
        public void TickWorld(double delta)
        {
            timeSinceSkyPatch += delta;
            if (timeSinceSkyPatch > 5 || Math.Abs((SunAngle - pSunAng).BiggestValue()) > 10f) // TODO: CVar for this?
            {
                timeSinceSkyPatch = 0;
                CreateSkyBox();
            }
            Engine.SunAdjustDirection = TheSun.Direction;
            Engine.SunAdjustBackupLight = new OpenTK.Vector4(TheSun.InternalLights[0].color, 1.0f);
            Engine.MainView.SunLight_Minimum = sl_min;
            Engine.MainView.SunLight_Maximum = sl_max;
            rTicks++;
            if (rTicks >= CVars.r_shadowpace.ValueI)
            {
                Vector3i playerChunkPos = TheRegion.ChunkLocFor(Player.GetPosition());
                if (playerChunkPos != SunChunkPos || Math.Abs((SunAngle - pSunAng).BiggestValue()) > 0.1f)
                {
                    SunChunkPos = playerChunkPos;
                    Location corPos = (SunChunkPos.ToLocation() * Constants.CHUNK_WIDTH) + new Location(Constants.CHUNK_WIDTH * 0.5);
                    TheSun.Direction = Utilities.ForwardVector_Deg(SunAngle.Yaw, SunAngle.Pitch);
                    TheSun.Reposition(corPos - TheSun.Direction * 30 * 6);
                    TheSunClouds.Direction = TheSun.Direction;
                    TheSunClouds.Reposition(TheSun.EyePos);
                    PlanetDir = Utilities.ForwardVector_Deg(PlanetAngle.Yaw, PlanetAngle.Pitch);
                    ThePlanet.Direction = PlanetDir;
                    ThePlanet.Reposition(corPos - ThePlanet.Direction * 30 * 6);
                    BEPUutilities.Vector3 tsd = TheSun.Direction.ToBVector();
                    BEPUutilities.Vector3 tpd = PlanetDir.ToBVector();
                    BEPUutilities.Quaternion.GetQuaternionBetweenNormalizedVectors(ref tsd, ref tpd, out BEPUutilities.Quaternion diff);
                    PlanetSunDist = (float)BEPUutilities.Quaternion.GetAngleFromQuaternion(ref diff) / (float)Utilities.PI180;
                    if (PlanetSunDist < 75)
                    {
                        TheSun.InternalLights[0].color = new OpenTK.Vector3((float)Math.Min(SunLightDef.X * (PlanetSunDist / 15), 1),
                            (float)Math.Min(SunLightDef.Y * (PlanetSunDist / 20), 1), (float)Math.Min(SunLightDef.Z * (PlanetSunDist / 60), 1));
                        TheSunClouds.InternalLights[0].color = new OpenTK.Vector3((float)Math.Min(CloudSunLightDef.X * (PlanetSunDist / 15), 1),
                            (float)Math.Min(CloudSunLightDef.Y * (PlanetSunDist / 20), 1), (float)Math.Min(CloudSunLightDef.Z * (PlanetSunDist / 60), 1));
                        //ThePlanet.InternalLights[0].color = new OpenTK.Vector3(0, 0, 0);
                    }
                    else
                    {
                        TheSun.InternalLights[0].color = ClientUtilities.Convert(SunLightDef);
                        TheSunClouds.InternalLights[0].color = ClientUtilities.Convert(CloudSunLightDef);
                        //ThePlanet.InternalLights[0].color = ClientUtilities.Convert(PlanetLightDef * Math.Min((PlanetSunDist / 180f), 1f));
                    }
                    PlanetLight = PlanetSunDist / 180f;
                    if (SunAngle.Pitch < 10 && SunAngle.Pitch > -30)
                    {
                        float rel = 30 + (float)SunAngle.Pitch;
                        if (rel == 0)
                        {
                            rel = 0.00001f;
                        }
                        rel = 1f - (rel / 40f);
                        rel = Math.Max(Math.Min(rel, 1f), 0f);
                        float rel2 = Math.Max(Math.Min(rel * 1.5f, 1f), 0f);
                        TheSun.InternalLights[0].color = new OpenTK.Vector3(TheSun.InternalLights[0].color.X * rel2, TheSun.InternalLights[0].color.Y * rel, TheSun.InternalLights[0].color.Z * rel);
                        TheSunClouds.InternalLights[0].color = new OpenTK.Vector3(TheSunClouds.InternalLights[0].color.X * rel2, TheSunClouds.InternalLights[0].color.Y * rel, TheSunClouds.InternalLights[0].color.Z * rel);
                        MainWorldView.DesaturationAmount = (1f - rel) * 0.75f;
                        MainWorldView.ambient = BaseAmbient * ((1f - rel) * 0.5f + 0.5f);
                        sl_min = 0.2f - (1f - rel) * (0.2f - 0.05f);
                        sl_max = 0.8f - (1f - rel) * (0.8f - 0.15f);
                        SkyColor = SkyApproxColDefault * rel;
                    }
                    else if (SunAngle.Pitch >= 10)
                    {
                        TheSun.InternalLights[0].color = new OpenTK.Vector3(0, 0, 0);
                        TheSunClouds.InternalLights[0].color = new OpenTK.Vector3(0, 0, 0);
                        MainWorldView.DesaturationAmount = 0.75f;
                        MainWorldView.ambient = BaseAmbient * 0.5f;
                        sl_min = 0.05f;
                        sl_max = 0.15f;
                        SkyColor = Location.Zero;
                    }
                    else
                    {
                        sl_min = 0.2f;
                        sl_max = 0.8f;
                        MainWorldView.DesaturationAmount = 0f;
                        MainWorldView.ambient = BaseAmbient;
                        TheSun.InternalLights[0].color = ClientUtilities.Convert(SunLightDef);
                        TheSunClouds.InternalLights[0].color = ClientUtilities.Convert(CloudSunLightDef);
                        SkyColor = SkyApproxColDefault;
                    }
                    shouldRedrawShadows = true;
                }
                rTicks = 0;
            }
            TheRegion.TickWorld(delta);
        }

        /// <summary>
        /// The sky box texture.
        /// </summary>
        public int SKY_TEX = -1;

        /// <summary>
        /// The sky box frame buffer object.
        /// </summary>
        public int[] SKY_FBO = null;

        const int SKY_TEX_SIZE = 256;

        public OpenTK.Vector3[] SkyDirs = new OpenTK.Vector3[]
            {
                new OpenTK.Vector3(1, 0, 0),
                new OpenTK.Vector3(-1, 0, 0),
                new OpenTK.Vector3(0, 1, 0),
                new OpenTK.Vector3(0, -1, 0),
                new OpenTK.Vector3(0, 0, 1),
                new OpenTK.Vector3(0, 0, -1)
            };

        public OpenTK.Vector3[] SkyUps = new OpenTK.Vector3[]
            {
                new OpenTK.Vector3(0, 0, 1),
                new OpenTK.Vector3(0, 0, 1),
                new OpenTK.Vector3(0, 0, 1),
                new OpenTK.Vector3(0, 0, 1),
                new OpenTK.Vector3(0, 1, 0),
                new OpenTK.Vector3(0, 1, 0),
            };

        /// <summary>
        /// Creates the sky box texture.
        /// </summary>
        public void CreateSkyBox()
        {
            GraphicsUtil.CheckError("SKYBOX - Pre");
            if (SKY_TEX < 0)
            {
                SKY_TEX = GL.GenTexture();
                SKY_FBO = new int[6];
                GL.BindTexture(TextureTarget.Texture2DArray, SKY_TEX);
                GraphicsUtil.CheckError("SKYBOX - GenPrep");
                GL.TexStorage3D(TextureTarget3d.Texture2DArray, 1, SizedInternalFormat.Rgba8, SKY_TEX_SIZE, SKY_TEX_SIZE, 6);
                GraphicsUtil.CheckError("SKYBOX - TexConfig 0.1");
                GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMinFilter, (int)(TextureMinFilter.Linear));
                GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMagFilter, (int)(TextureMagFilter.Linear));
                GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
                GL.BindTexture(TextureTarget.Texture2DArray, 0);
                GraphicsUtil.CheckError("SKYBOX - TexConfig");
                for (int i = 0; i < 6; i++)
                {
                    SKY_FBO[i] = GL.GenFramebuffer();
                    GL.BindFramebuffer(FramebufferTarget.Framebuffer, SKY_FBO[i]);
                    GL.FramebufferTextureLayer(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, SKY_TEX, 0, i);
                    GraphicsUtil.CheckError("SKYBOX - FBO " + i);
                }
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            }
            GL.Viewport(0, 0, SKY_TEX_SIZE, SKY_TEX_SIZE);
            GraphicsUtil.CheckError("SKYBOX - Render/Fast - Uniforms Prep");
            Engine.Shaders3D.s_forwt_nofog.Bind();
            GL.Uniform1(6, (float)Engine.GlobalTickTime);
            GL.Uniform4(12, new OpenTK.Vector4(0f, 0f, 0f, 0f));
            GL.Uniform1(13, FogMaxDist());
            //GL.Uniform2(14, zfar_rel);
            Engine.Rendering.SetColor(Color4F.White, MainWorldView);
            Engine.Shaders3D.s_forwt.Bind();
            GraphicsUtil.CheckError("SKYBOX - Render/Fast - Uniforms 4.2");
            GL.Uniform1(6, (float)Engine.GlobalTickTime);
            GraphicsUtil.CheckError("SKYBOX - Render/Fast - Uniforms 4.3");
            GL.Uniform4(12, new OpenTK.Vector4(0f, 0f, 0f, 0f));
            GraphicsUtil.CheckError("SKYBOX - Render/Fast - Uniforms 4.4");
            GL.Uniform1(13, FogMaxDist());
            Matrix4 projo = Matrix4.CreatePerspectiveFieldOfView((float)(90.0 * Utilities.PI180), 1f, 60f, ZFarOut());
            GL.ActiveTexture(TextureUnit.Texture3);
            Textures.Black.Bind();
            GL.ActiveTexture(TextureUnit.Texture2);
            Textures.Black.Bind();
            GL.ActiveTexture(TextureUnit.Texture1);
            Textures.NormalDef.Bind();
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.Disable(EnableCap.CullFace);
            GL.Disable(EnableCap.DepthTest);
            Rendering.SetColor(Color4.White, MainWorldView);
            GraphicsUtil.CheckError("SKYBOX - Rendering - Sky - Prep - Color");
            float skyAlpha = (float)Math.Max(Math.Min((SunAngle.Pitch - 70.0) / (-90.0), 1.0), 0.06);
            for (int i = 0; i < 6; i++)
            {
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, SKY_FBO[i]);
                GraphicsUtil.CheckError("SKYBOX - Rendering - Sky - Prep - Color1.1");
                GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
                GraphicsUtil.CheckError("SKYBOX - Rendering - Sky - Prep - Color1.2");
                GL.ClearBuffer(ClearBuffer.Color, 0, new float[] { 0.0f, 1.0f, 1.0f, 1.0f });
                GraphicsUtil.CheckError("SKYBOX - Rendering - Sky - Prep - Color1.3");
                Matrix4 viewo = Matrix4.LookAt(OpenTK.Vector3.Zero, SkyDirs[i], SkyUps[i]);
                Matrix4 pv = viewo * projo;
                GL.UniformMatrix4(1, false, ref pv);
                Rendering.SetColor(Color4.White, MainWorldView);
                GraphicsUtil.CheckError("SKYBOX - Rendering - Sky - Prep - Color2");
                Matrix4 scale = Matrix4.CreateScale(GetSecondSkyDistance());
                GL.UniformMatrix4(2, false, ref scale);
                GraphicsUtil.CheckError("SKYBOX - Rendering - Sky - Prep - Scale");
                GraphicsUtil.CheckError("SKYBOX - Rendering - Sky - Prep");
                // TODO: Only render relevant side?
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
                Rendering.SetColor(new OpenTK.Vector4(1, 1, 1, skyAlpha), MainWorldView);
                scale = Matrix4.CreateScale(GetSkyDistance());
                GL.UniformMatrix4(2, false, ref scale);
                GraphicsUtil.CheckError("SKYBOX - Rendering - Sky - Night");
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
                GraphicsUtil.CheckError("SKYBOX - Rendering - Sky - Light");
                GraphicsUtil.CheckError("SKYBOX - Rendering - Sky - Sun - Pre 1");
                Rendering.SetColor(Color4.White, MainWorldView);
                Engine.Shaders3D.s_forwt_nofog.Bind();
                GL.UniformMatrix4(1, false, ref pv);
                GraphicsUtil.CheckError("SKYBOX - Rendering - Sky - Sun - Pre 2");
                float zf = ZFar();
                float spf = ZFarOut() * 0.3333f;
                Textures.GetTexture("skies/sun").Bind(); // TODO: Store var!
                Matrix4 rot = Matrix4.CreateTranslation(-spf * 0.5f, -spf * 0.5f, 0f)
                    * Matrix4.CreateRotationY((float)((-SunAngle.Pitch - 90f) * Utilities.PI180))
                    * Matrix4.CreateRotationZ((float)((180f + SunAngle.Yaw) * Utilities.PI180))
                    * Matrix4.CreateTranslation(ClientUtilities.Convert(TheSun.Direction * -(GetSkyDistance() * 0.95f)));
                Rendering.RenderRectangle(0, 0, spf, spf, rot);
                GraphicsUtil.CheckError("SKYBOX - Rendering - Sky - Sun");
                Textures.GetTexture("skies/planet_sphere").Bind(); // TODO: Store var!
                float ppf = ZFarOut() * 0.5f;
                GL.Enable(EnableCap.CullFace);
                Rendering.SetColor(new Color4(PlanetLight, PlanetLight, PlanetLight, 1), MainWorldView);
                rot = Matrix4.CreateScale(ppf * 0.5f)
                    * Matrix4.CreateTranslation(-ppf * 0.5f, -ppf * 0.5f, 0f)
                    * Matrix4.CreateRotationZ((float)((180f + PlanetAngle.Yaw) * Utilities.PI180))
                    * Matrix4.CreateTranslation(ClientUtilities.Convert(PlanetDir * -(GetSkyDistance() * 0.79f)));
                GL.UniformMatrix4(2, false, ref rot);
                Models.Sphere.Draw();
                GraphicsUtil.CheckError("SKYBOX - Rendering - Sky - Planet");
                GL.BindTexture(TextureTarget.Texture2D, 0);
                Matrix4 ident = Matrix4.Identity;
                GL.UniformMatrix4(2, false, ref ident);
                GL.Disable(EnableCap.CullFace);
                GraphicsUtil.CheckError("SKYBOX - Rendering - Sky - N3 - Pre");
                GraphicsUtil.CheckError("SKYBOX - Rendering - Sky - N3 - Light");
                Rendering.SetColor(Color4.White, MainWorldView);
                GraphicsUtil.CheckError("SKYBOX - Rendering - Sky - N3 - Color");
                // TODO: other sky/outview data here as well? (Chunks, trees, etc.?)
            }
            GL.Enable(EnableCap.CullFace);
            GL.Enable(EnableCap.DepthTest);
            Shaders.ColorMultShader.Bind();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.DrawBuffer(DrawBufferMode.Back);
            GL.Viewport(0, 0, Window.Width, Window.Height);
        }
    }
}
