﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ShadowOperations.ClientGame.ClientMainSystem;
using ShadowOperations.Shared;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;

namespace ShadowOperations.ClientGame.EntitySystem
{
    public class BasicPrimitiveEntity: PrimitiveEntity
    {
        public BasicPrimitiveEntity(Client tclient)
            : base(tclient)
        {
        }

        public override void Destroy()
        {
        }

        public override void Spawn()
        {
        }

        public Location scale;

        public override void Render()
        {
            if (TheClient.RenderTextures)
            {
                TheClient.Textures.White.Bind();
            }
            Matrix4 mat = Matrix4.CreateScale(scale.ToOVector()) * Matrix4.CreateTranslation(GetPosition().ToOVector()) * Matrix4.CreateTranslation((-scale * 0.5f).ToOVector());
            GL.UniformMatrix4(2, false, ref mat);
            TheClient.Models.Cube.Draw();
        }
    }
}
