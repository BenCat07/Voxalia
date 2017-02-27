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
using Voxalia.ClientGame.GraphicsSystems.LightingSystem;
using Voxalia.ClientGame.OtherSystems;
using Voxalia.Shared.Collision;
using Voxalia.ClientGame.GraphicsSystems;

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
            BuildLightsForWorld();
            TheRegion = new Region();
            TheRegion.TheClient = this;
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
            View3D.CheckError("Load - World - Deletes");
            int wid = CVars.r_shadowquality.ValueI;
            TheSun = new SkyLight(Location.Zero, MaximumStraightBlockDistance() * 2, SunLightDef, new Location(0, 0, -1), MaximumStraightBlockDistance() * 2 + Chunk.CHUNK_SIZE * 2, false, wid);
            MainWorldView.Lights.Add(TheSun);
            View3D.CheckError("Load - World - Sun");
            // TODO: Separate cloud quality CVar?
            TheSunClouds = new SkyLight(Location.Zero, MaximumStraightBlockDistance() * 2, CloudSunLightDef, new Location(0, 0, -1), MaximumStraightBlockDistance() * 2 + Chunk.CHUNK_SIZE * 2, true, wid);
            MainWorldView.Lights.Add(TheSunClouds);
            View3D.CheckError("Load - World - Clouds");
            // TODO: Separate planet quality CVar?
            ThePlanet = new SkyLight(Location.Zero, MaximumStraightBlockDistance() * 2, PlanetLightDef, new Location(0, 0, -1), MaximumStraightBlockDistance() * 2 + Chunk.CHUNK_SIZE * 2, false, wid);
            MainWorldView.Lights.Add(ThePlanet);
            View3D.CheckError("Load - World - Planet");
            onCloudShadowChanged(null, null);
            View3D.CheckError("Load - World - Changed");
        }

        /// <summary>
        /// Called automatically when the cloud shadow CVar is changed to update that.
        /// </summary>
        public void onCloudShadowChanged(object obj, EventArgs e)
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
        public static readonly Location SkyApproxColDefault = new Location(0.1, 0.4, 0.5);

        /// <summary>
        /// The current approximate color of the sky.
        /// </summary>
        public Location SkyColor = SkyApproxColDefault;

        public Vector3i SunChunkPos = new Vector3i(int.MaxValue, int.MaxValue, int.MaxValue);

        /// <summary>
        /// Ticks the region, including all primary calculations and lighting updates.
        /// </summary>
        public void TickWorld(double delta)
        {
            rTicks++;
            if (rTicks >= CVars.r_shadowpace.ValueI)
            {
                Vector3i playerChunkPos = TheRegion.ChunkLocFor(Player.GetPosition());
                if (playerChunkPos != SunChunkPos) // TODO: Or sun/planet angle changed!
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
                    Quaternion diff;
                    Vector3 tsd = TheSun.Direction.ToBVector();
                    Vector3 tpd = PlanetDir.ToBVector();
                    Quaternion.GetQuaternionBetweenNormalizedVectors(ref tsd, ref tpd, out diff);
                    PlanetSunDist = (float)Quaternion.GetAngleFromQuaternion(ref diff) / (float)Utilities.PI180;
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
    }
}
