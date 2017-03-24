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
using System.Threading.Tasks;
using Voxalia.ClientGame.WorldSystem;
using Voxalia.ClientGame.GraphicsSystems;
using Voxalia.Shared;
using Voxalia.Shared.Collision;
using Voxalia.ClientGame.OtherSystems;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using FreneticGameCore;

namespace Voxalia.ClientGame.EntitySystem
{
    class PrimitiveModelEntity : PrimitiveEntity
    {
        public Model model;

        public Location scale = Location.One;

        public PrimitiveModelEntity(string modelname, Region tregion)
            : base(tregion, false)
        {
            model = tregion.TheClient.Models.GetModel(modelname);
            Gravity = Location.Zero;
            Velocity = Location.Zero;
        }

        public BEPUutilities.Vector3 ModelMin;

        public BEPUutilities.Vector3 ModelMax;

        public override void Spawn()
        {
            List<BEPUutilities.Vector3> vecs = TheClient.Models.Handler.GetCollisionVertices(model.Original);
            Location zero = new Location(vecs[0]);
            AABB abox = new AABB() { Min = zero, Max = zero };
            for (int v = 1; v < vecs.Count; v++)
            {
                abox.Include(new Location(vecs[v]));
            }
            ModelMin = abox.Min.ToBVector();
            ModelMax = abox.Max.ToBVector();
        }

        public override void Destroy()
        {
        }

        public override void Render()
        {
            if (!Visible)
            {
                return;
            }
            TheClient.SetEnts();
            BEPUutilities.RigidTransform rt = new BEPUutilities.RigidTransform(GetPosition().ToBVector(), GetOrientation());
            BEPUutilities.RigidTransform.Transform(ref ModelMin, ref rt, out BEPUutilities.Vector3 bmin);
            BEPUutilities.RigidTransform.Transform(ref ModelMax, ref rt, out BEPUutilities.Vector3 bmax);
            if (TheClient.MainWorldView.CFrust != null && !TheClient.MainWorldView.CFrust.ContainsBox(new Location(bmin), new Location(bmax)))
            {
                return;
            }
            Matrix4d orient = GetOrientationMatrix();
            Matrix4d mat = (Matrix4d.Scale(ClientUtilities.ConvertD(scale)) * orient * Matrix4d.CreateTranslation(ClientUtilities.ConvertD(GetPosition())));
            TheClient.MainWorldView.SetMatrix(2, mat);
            TheClient.Rendering.SetMinimumLight(0.0f);
            if (model.Meshes[0].vbo.Tex == null)
            {
                TheClient.Textures.White.Bind();
            }
            model.Draw(); // TODO: Animation?
        }

    }
}
