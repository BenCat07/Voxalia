//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Voxalia.ClientGame.WorldSystem;
using Voxalia.ClientGame.GraphicsSystems;
using FreneticGameGraphics.LightingSystem;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using Voxalia.ClientGame.OtherSystems;
using BEPUphysics.CollisionShapes.ConvexShapes;
using Voxalia.Shared;
using Voxalia.ClientGame.ClientMainSystem;
using FreneticGameCore;
using FreneticGameGraphics.ClientSystem;

namespace Voxalia.ClientGame.EntitySystem
{
    class GlowstickEntity: GrenadeEntity
    {
        PointLight light;

        public float Brightness = 2.0f; // TODO: Controllable!

        public GlowstickEntity(Region tregion, int color) // TODO: Int -> Actual Color4F?
            : base(tregion, false)
        {
            System.Drawing.Color col = System.Drawing.Color.FromArgb(color);
            GColor = Color4F.FromArgb(col.R, col.G, col.B, col.A);
        }

        public override void Render()
        {
            if (TheClient.MainWorldView.FBOid == FBOID.MAIN)
            {
                // TODO: ??? GL.Uniform4(7, new Vector4(GColor.R * Brightness, GColor.G * Brightness, GColor.B * Brightness, 1f));
            }
            TheClient.Rendering.SetMinimumLight(Brightness, TheClient.MainWorldView);
            base.Render();
            TheClient.Rendering.SetMinimumLight(0, TheClient.MainWorldView);
            if (TheClient.MainWorldView.FBOid == FBOID.MAIN)
            {
                // TODO: ??? GL.Uniform4(7, new Vector4(0f, 0f, 0f, 0f));
            }
        }

        public override void Tick()
        {
            light.Reposition(GetPosition());
            base.Tick();
        }

        public override void SpawnBody()
        {
            light = new PointLight(GetPosition(), 15, GColor.RGB * Brightness);
            //light.SetCastShadows(false);
            TheClient.MainWorldView.Lights.Add(light);
            base.SpawnBody();
        }

        public override void DestroyBody()
        {
            TheClient.MainWorldView.Lights.Remove(light);
            light.Destroy();
            base.DestroyBody();
        }
    }

    public class GlowstickEntityConstructor : EntityTypeConstructor
    {
        public override Entity Create(Region tregion, byte[] data)
        {
            int col = Utilities.BytesToInt(Utilities.BytesPartial(data, PhysicsEntity.PhysicsNetworkDataLength, 4));
            GlowstickEntity ge = new GlowstickEntity(tregion, col);
            ge.ApplyPhysicsNetworkData(data);
            return ge;
        }
    }
}
