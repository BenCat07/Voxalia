//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for contents of the license.
// If neither of these are not available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Voxalia.ClientGame.WorldSystem;
using Voxalia.ClientGame.GraphicsSystems;
using Voxalia.ClientGame.GraphicsSystems.LightingSystem;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using Voxalia.ClientGame.OtherSystems;
using BEPUphysics.CollisionShapes.ConvexShapes;
using Voxalia.Shared;

namespace Voxalia.ClientGame.EntitySystem
{
    public class GrenadeEntity: PhysicsEntity
    {
        public Model model;

        public Color4 GColor;

        public GrenadeEntity(Region tregion, bool shadows)
            : base(tregion, true, shadows)
        {
            model = TheClient.Models.Sphere;
            GColor = new Color4(0f, 0f, 0f, 1f);
            Shape = new CylinderShape(0.2f, 0.05f);
            Bounciness = 0.95f;
            SetMass(1);
        }

        public override void Render()
        {
            TheClient.SetEnts();
            TheClient.Textures.White.Bind();
            Matrix4d mat = Matrix4d.Scale(0.05f, 0.2f, 0.05f) * GetTransformationMatrix();
            TheClient.MainWorldView.SetMatrix(2, mat);
            TheClient.Rendering.SetColor(GColor);
            model.Draw();
            TheClient.Rendering.SetColor(Color4.White);
        }
    }

    public class GrenadeEntityConstructor : EntityTypeConstructor
    {
        public override Entity Create(Region tregion, byte[] data)
        {
            GrenadeEntity ge = new GrenadeEntity(tregion, true);
            ge.ApplyPhysicsNetworkData(data);
            return ge;
        }
    }
}
