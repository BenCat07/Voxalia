//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Drawing;
using Voxalia.Shared;
using FreneticGameCore.Files;
using Voxalia.ClientGame.EntitySystem;
using Voxalia.ClientGame.ClientMainSystem;
using Voxalia.ClientGame.GraphicsSystems;
using FreneticGameCore;
using FreneticGameGraphics;

namespace Voxalia.ClientGame.OtherSystems
{
    public class ItemStack: ItemStackBase
    {
        public Client TheClient;

        public ItemStack(Client tclient, byte[] data)
        {
            TheClient = tclient;
            DataStream ds = new DataStream(data);
            DataReader dr = new DataReader(ds);
            Load(dr, (b) => new ItemStack(tclient, b));
        }

        public ItemStack(Client tclient, string name)
        {
            TheClient = tclient;
            Name = name;
        }

        public Texture Tex;

        public BlockItemEntity RenderedBlock = null;

        public ModelEntity RenderedModel = null;

        public System.Drawing.Color GetColor()
        {
            return DrawColor;
        }

        public override void SetTextureName(string name)
        {
            if (name == null || name.Length == 0)
            {
                Tex = null;
            }
            else
            {
                if (name.Contains(":") && name.Before(":").ToLowerFast() == "render_block")
                {
                    string[] blockDataToRender = name.After(":").SplitFast(',');
                    if (blockDataToRender[0] == "self")
                    {
                        BlockInternal bi = BlockInternal.FromItemDatum(Datum);
                        RenderedBlock = new BlockItemEntity(TheClient.TheRegion, bi.Material, bi.BlockData, bi.BlockPaint, bi.Damage);
                        RenderedBlock.GenVBO();
                    }
                    else
                    {
                        Material mat = MaterialHelpers.FromNameOrNumber(blockDataToRender[0]);
                        byte data = (byte)(blockDataToRender.Length < 2 ? 0 : Utilities.StringToInt(blockDataToRender[1]));
                        byte paint = (byte)(blockDataToRender.Length < 3 ? 0 : Colors.ForName(blockDataToRender[2]));
                        BlockDamage damage = blockDataToRender.Length < 4 ? BlockDamage.NONE : (BlockDamage)Enum.Parse(typeof(BlockDamage), blockDataToRender[3], true);
                        RenderedBlock = new BlockItemEntity(TheClient.TheRegion, mat, data, paint, damage);
                        RenderedBlock.GenVBO();
                    }
                    Tex = null;
                }
                if (name.Contains(":") && name.Before(":").ToLowerFast() == "render_model")
                {
                    string model = name.After(":");
                    if (model.ToLowerFast() == "self")
                    {
                        model = GetModelName();
                    }
                    RenderedModel = new ModelEntity(model, TheClient.TheRegion)
                    {
                        Visible = true
                    };
                    RenderedModel.PreHandleSpawn();
                    Tex = null;
                }
                else
                {
                    Tex = TheClient.Textures.GetTexture(name);
                }
            }
        }

        public override string GetTextureName()
        {
            if (RenderedBlock != null)
            {
                return "render_block:" + RenderedBlock.Mat + "," + RenderedBlock.Dat + "," + RenderedBlock.Paint + "," + RenderedBlock.Damage;
            }
            if (RenderedModel != null)
            {
                return "render_model:" + RenderedModel.model.Name;
            }
            return Tex?.Name;
        }

        public Model Mod;

        public override string GetModelName()
        {
            return Mod?.Name;
        }

        public override void SetModelName(string name)
        {
            Mod = TheClient.Models.GetModel(name);
        }

        public void Forget() // TODO: use this!
        {
            if (RenderedBlock != null)
            {
                RenderedBlock.vbo.Destroy();
            }
        }

        public void Render3D(Location pos, float rot, Location size, BEPUutilities.Matrix rotFix)
        {
            rotFix.Transpose();
            BEPUutilities.Matrix rot1 = BEPUutilities.Matrix.CreateFromAxisAngle(BEPUutilities.Vector3.UnitZ, rot)
                * BEPUutilities.Matrix.CreateFromAxisAngle(BEPUutilities.Vector3.UnitX, (float)(Math.PI * 0.25))
                * rotFix;
            if (RenderedBlock != null)
            {
                TheClient.isVox = false;
                TheClient.SetVox();
                TheClient.Rendering.SetMinimumLight(0.9f);
                RenderedBlock.WorldTransform = BEPUutilities.Matrix.CreateScale(size.ToBVector() * 0.70f)
                    * rot1
                    * BEPUutilities.Matrix.CreateTranslation(pos.ToBVector());
                RenderedBlock.Render();
                TheClient.Rendering.SetMinimumLight(0f);
            }
            else if (RenderedModel != null)
            {
                TheClient.isVox = true;
                TheClient.SetEnts();
                TheClient.Rendering.SetMinimumLight(0.9f);
                BEPUutilities.RigidTransform rt = BEPUutilities.RigidTransform.Identity;
                RenderedModel.Shape.GetBoundingBox(ref rt, out BEPUutilities.BoundingBox bb);
                BEPUutilities.Vector3 scale = BEPUutilities.Vector3.Max(bb.Max, -bb.Min);
                float len = (float)scale.Length();
                RenderedModel.WorldTransform = BEPUutilities.Matrix.CreateScale(size.ToBVector() * len)
                    * rot1
                    * BEPUutilities.Matrix.CreateTranslation(pos.ToBVector());
                RenderedModel.RenderSimpler();
                TheClient.Rendering.SetMinimumLight(0f);
            }
        }

        public void Render(Location pos, Location size, BEPUutilities.Matrix rotFix)
        {
            if (RenderedBlock != null)
            {
                return;
            }
            if (Tex == null)
            {
                if (Name == "block")
                {
                    Tex = TheClient.Textures.GetTexture("blocks/icons/" + SecondaryName.ToLowerFast());
                }
                else
                {
                    return;
                }
            }
            Tex.Bind();
            TheClient.Rendering.SetColor(TheClient.Rendering.AdaptColor(ClientUtilities.Convert(TheClient.Player.GetPosition()), GetColor()));
            TheClient.Rendering.RenderRectangle((int)pos.X, (int)pos.Y, (int)(pos.X + size.X), (int)(pos.Y + size.Y), ClientUtilities.Convert(rotFix));
        }
    }
}
