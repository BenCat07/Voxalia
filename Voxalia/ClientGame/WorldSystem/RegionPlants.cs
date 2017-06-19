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
using Voxalia.Shared;
using Voxalia.ClientGame.ClientMainSystem;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using BEPUphysics;
using BEPUphysics.Settings;
using Voxalia.ClientGame.JointSystem;
using Voxalia.ClientGame.EntitySystem;
using BEPUutilities.Threading;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.CollisionShapes.ConvexShapes;
using Voxalia.Shared.Collision;
using System.Diagnostics;
using Voxalia.ClientGame.GraphicsSystems;
using Voxalia.ClientGame.OtherSystems;
using FreneticGameCore;
using FreneticGameGraphics;
using FreneticGameGraphics.GraphicsHelpers;
using FreneticGameCore.Collision;

namespace Voxalia.ClientGame.WorldSystem
{
    public partial class Region
    {
        public void RenderPlants()
        {
            if (TheClient.CVars.r_plants.ValueB)
            {
                TheClient.SetEnts();
                RenderGrass();
            }
        }

        public int Shader_Compute_Grass_Swing;

        public void PrepPlants()
        {
            try
            {
                Shader_Compute_Grass_Swing = TheClient.Shaders.CompileCompute("grass_swing", "");
            }
            catch (Exception ex)
            {
                SysConsole.Output(ex);
            }
        }

        public float SphereRadFor(BEPUutilities.BoundingBox bb)
        {
            return (float)(new Location(bb.Max - bb.Min).BiggestValue());
        }

        public void SquishGrassSwing(Vector4 swinger)
        {
            if (!TheClient.CVars.r_compute.ValueB)
            {
                return;
            }
            Location spos = new Location(swinger.X, swinger.Y, swinger.Z);
            double maxdist = (swinger.W + Chunk.CHUNK_SIZE) * (swinger.W + Chunk.CHUNK_SIZE) * 4;
            GL.UseProgram(Shader_Compute_Grass_Swing);
            foreach (Chunk chk in LoadedChunks.Values)
            {
                Location cwor = chk.WorldPosition.ToLocation() * Chunk.CHUNK_SIZE;
                Location wpos = cwor + new Location(Chunk.CHUNK_SIZE * 0.5);
                if (chk.Plant_C > 0 && chk.Plant_VAO > 0 && wpos.DistanceSquared(spos) < maxdist)
                {
                    Location relp = spos - cwor;
                    GL.Uniform1(11, (uint)chk.Plant_C);
                    GL.Uniform4(12, new Vector4(ClientUtilities.Convert(relp), swinger.W));
                    GL.Uniform1(13, (float)GlobalTickTimeLocal);
                    GL.Uniform1(14, (float)Delta);
                    GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, chk.Plant_VBO_Pos);
                    GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 2, chk.Plant_VBO_Tcs);
                    GL.DispatchCompute(chk.Plant_C / 90 + 1, 1, 1);
                }
            }
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, 0);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 2, 0);
            View3D.CheckError("Squishing Grass");
        }
        
        public void RenderGrass()
        {
            if (TheClient.MainWorldView.FBOid == FBOID.FORWARD_SOLID)
            {
                TheClient.s_forw_grass = TheClient.s_forw_grass.Bind();
                GL.Uniform1(6, (float)TheClient.GlobalTickTimeLocal);
                GL.Uniform4(12, new Vector4(ClientUtilities.Convert(TheClient.MainWorldView.FogCol), TheClient.MainWorldView.FogAlpha));
                GL.Uniform2(14, new Vector2(TheClient.CVars.r_znear.ValueF, TheClient.ZFar()));
                GL.UniformMatrix4(1, false, ref TheClient.MainWorldView.PrimaryMatrix);
            }
            else if (TheClient.MainWorldView.FBOid == FBOID.MAIN)
            {
                TheClient.s_fbo_grass = TheClient.s_fbo_grass.Bind();
                GL.UniformMatrix4(1, false, ref TheClient.MainWorldView.PrimaryMatrix);
            }
            else if (TheClient.MainWorldView.FBOid == FBOID.SHADOWS && TheClient.MainWorldView.TranspShadows)
            {
                TheClient.s_shadow_grass = TheClient.s_shadow_grass.Bind();
            }
            else
            {
                return;
            }
            GL.ActiveTexture(TextureUnit.Texture3);
            GL.BindTexture(TextureTarget.Texture2DArray, 0);
            GL.ActiveTexture(TextureUnit.Texture2);
            GL.BindTexture(TextureTarget.Texture2DArray, 0);
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2DArray, 0);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2DArray, TheClient.GrassTextureID);
            GL.Uniform1(6, (float)GlobalTickTimeLocal);
            GL.Uniform3(7, ClientUtilities.Convert(ActualWind));
            GL.Uniform1(8, TheClient.CVars.r_plantdistance.ValueF * TheClient.CVars.r_plantdistance.ValueF);
            TheClient.Rendering.SetColor(GetSunAdjust());
            foreach (Chunk chunk in chToRender)
            {
                if (chunk.Plant_VAO != -1)
                {
                    Matrix4d mat = Matrix4d.CreateTranslation(ClientUtilities.ConvertD(chunk.WorldPosition.ToLocation() * Chunk.CHUNK_SIZE));
                    TheClient.MainWorldView.SetMatrix(2, mat);
                    GL.BindVertexArray(chunk.Plant_VAO);
                    GL.DrawElements(PrimitiveType.Points, chunk.Plant_C, DrawElementsType.UnsignedInt, IntPtr.Zero);
                }
            }
            TheClient.isVox = true;
            TheClient.SetEnts();
        }

    }
}
