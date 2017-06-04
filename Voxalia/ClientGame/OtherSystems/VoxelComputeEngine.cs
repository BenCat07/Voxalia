using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Voxalia.ClientGame.ClientMainSystem;
using Voxalia.ClientGame.WorldSystem;
using Voxalia.ClientGame.GraphicsSystems;
using OpenTK;
using OpenTK.Graphics;
using FreneticGameCore;
using OpenTK.Graphics.OpenGL4;
using Voxalia.Shared;

namespace Voxalia.ClientGame.OtherSystems
{
    public class VoxelComputeEngine
    {
        public int Program_Counter;

        public int Program_Cruncher;

        public int Texture_IDs;

        public Client TheClient;

        public void Init(Client tclient)
        {
            TheClient = tclient;
            Program_Counter = TheClient.Shaders.CompileCompute("vox_count");
            Program_Cruncher = TheClient.Shaders.CompileCompute("vox_crunch");
            View3D.CheckError("Compute - Startup - Shaders");
            int levels = TheClient.CVars.r_block_mipmaps.ValueB ? 4 : 1;
            float[] df = new float[MaterialHelpers.ALL_MATS.Count * 6];
            for (int i = 0; i < MaterialHelpers.ALL_MATS.Count; i++)
            {
                for (int x = 0; x < 6; x++)
                {
                    df[x * MaterialHelpers.ALL_MATS.Count + i] = MaterialHelpers.ALL_MATS[i].TID[x][0]; // TODO: Represent full range of random options somehow? Maybe make the texture taller via multiplication by max height of this value?
                }
            }
            Texture_IDs = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2DArray, Texture_IDs);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R32f, MaterialHelpers.ALL_MATS.Count, 6, 0, PixelFormat.Red, PixelType.Float, df);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.BindTexture(TextureTarget.Texture2DArray, 0);
            View3D.CheckError("Compute - Startup - Texture");
        }

        const BufferUsageHint hintter = BufferUsageHint.DynamicRead;

        public int Calc(Chunk ch)
        {
            // Create voxel buffer data
            int VoxelBuffer = GL.GenBuffer();
            int[] temp = new int[ch.CSize * ch.CSize * ch.CSize * 4];
            for (int i = 0; i < temp.Length; i += 4)
            {
                BlockInternal bi = ch.BlocksInternal[i / 4];
                temp[i + 0] = bi._BlockMaterialInternal;
                temp[i + 1] = bi.BlockLocalData;
                temp[i + 2] = bi.BlockData;
                temp[i + 3] = bi._BlockPaintInternal;
            }
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, VoxelBuffer);
            GL.BufferData(BufferTarget.ShaderStorageBuffer, temp.Length * sizeof(int), temp, BufferUsageHint.StaticDraw);
            View3D.CheckError("Compute - Prep 0");
            // Compile shaders
            // Create a results buffer
            int resBuf = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.AtomicCounterBuffer, resBuf);
            uint[] resses = new uint[1];
            GL.BufferData(BufferTarget.AtomicCounterBuffer, sizeof(int), resses, hintter);
            GL.BindBuffer(BufferTarget.AtomicCounterBuffer, 0);
            // Run the shader
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, VoxelBuffer);
            GL.BindBufferBase(BufferRangeTarget.AtomicCounterBuffer, 2, resBuf);
            GL.UseProgram(Program_Counter);
            GL.DispatchCompute(ch.CSize, ch.CSize, ch.CSize);
            GL.UseProgram(0);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, 0);
            GL.BindBufferBase(BufferRangeTarget.AtomicCounterBuffer, 2, 0);
            View3D.CheckError("Compute - Run");
            // Gather results
            GL.MemoryBarrier(MemoryBarrierFlags.AtomicCounterBarrierBit);
            GL.BindBuffer(BufferTarget.AtomicCounterBuffer, resBuf);
            GL.GetBufferSubData(BufferTarget.AtomicCounterBuffer, IntPtr.Zero, sizeof(uint), resses);
            GL.BindBuffer(BufferTarget.AtomicCounterBuffer, 0);
            View3D.CheckError("Compute - Read Count");
            int resd = (int)resses[0];
            // Start new buffers
            resses[0] = 0;
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, resBuf);
            resses = new uint[1];
            GL.BufferData(BufferTarget.ShaderStorageBuffer, sizeof(uint), resses, hintter);
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
            uint[] newBufs = new uint[9];
            GL.GenBuffers(9, newBufs);
            byte[] minimum_needed = null;// new byte[resd * Vector4.SizeInBytes];
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, newBufs[0]);
            GL.BufferData(BufferTarget.ShaderStorageBuffer, resd * Vector4.SizeInBytes, minimum_needed, hintter);
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, newBufs[1]);
            GL.BufferData(BufferTarget.ShaderStorageBuffer, resd * Vector4.SizeInBytes, minimum_needed, hintter);
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, newBufs[2]);
            GL.BufferData(BufferTarget.ShaderStorageBuffer, resd * Vector4.SizeInBytes, minimum_needed, hintter);
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, newBufs[3]);
            GL.BufferData(BufferTarget.ShaderStorageBuffer, resd * Vector4.SizeInBytes, minimum_needed, hintter);
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, newBufs[4]);
            GL.BufferData(BufferTarget.ShaderStorageBuffer, resd * Vector4.SizeInBytes, minimum_needed, hintter);
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, newBufs[5]);
            GL.BufferData(BufferTarget.ShaderStorageBuffer, resd * Vector4.SizeInBytes, minimum_needed, hintter);
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, newBufs[6]);
            GL.BufferData(BufferTarget.ShaderStorageBuffer, resd * Vector4.SizeInBytes, minimum_needed, hintter);
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, newBufs[7]);
            GL.BufferData(BufferTarget.ShaderStorageBuffer, resd * Vector4.SizeInBytes, minimum_needed, hintter);
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, newBufs[8]);
            GL.BufferData(BufferTarget.ShaderStorageBuffer, resd * sizeof(uint), minimum_needed, hintter);
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
            View3D.CheckError("Compute - New Buffers");
            // Bind new buffers
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, VoxelBuffer);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 2, resBuf);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 3, newBufs[0]);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 4, newBufs[1]);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 5, newBufs[2]);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 6, newBufs[3]);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 7, newBufs[4]);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 8, newBufs[5]);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 9, newBufs[6]);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 10, newBufs[7]);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 11, newBufs[8]);
            View3D.CheckError("Compute - New Buff Binds");
            // Compute!
            GL.UseProgram(Program_Cruncher);
            GL.BindImageTexture(0, Texture_IDs, 0, false, 0, TextureAccess.ReadOnly, SizedInternalFormat.R32f);
            GL.DispatchCompute(ch.CSize, ch.CSize, ch.CSize);
            GL.UseProgram(0);
            GL.Finish();
            GL.MemoryBarrier(MemoryBarrierFlags.AllBarrierBits);
            View3D.CheckError("Compute - Crunch");
            // Unbind new buffers
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, 0);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 2, 0);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 3, 0);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 4, 0);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 5, 0);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 6, 0);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 7, 0);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 8, 0);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 9, 0);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 10, 0);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 11, 0);
            View3D.CheckError("Compute - End Buffs");
            // OPTIONAL, REMOVE ME:
            {
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, newBufs[8]);
                uint[] rd = new uint[16];
                GL.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, rd.Length * sizeof(uint), rd);
                for (int i = 0; i < rd.Length; i++)
                {
                    SysConsole.Output(OutputType.DEBUG, "" + rd[i]);
                }
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, newBufs[0]);
                Vector4[] rd2 = new Vector4[16];
                GL.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, rd2.Length * Vector4.SizeInBytes, rd2);
                for (int i = 0; i < rd2.Length; i++)
                {
                    SysConsole.Output(OutputType.DEBUG, "" + rd2[i]);
                }
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, newBufs[1]);
                GL.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, rd2.Length * Vector4.SizeInBytes, rd2);
                for (int i = 0; i < rd2.Length; i++)
                {
                    SysConsole.Output(OutputType.DEBUG, "" + rd2[i]);
                }
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, newBufs[4]);
                GL.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, rd2.Length * Vector4.SizeInBytes, rd2);
                for (int i = 0; i < rd2.Length; i++)
                {
                    SysConsole.Output(OutputType.DEBUG, "" + rd2[i]);
                }
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, resBuf);
                GL.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, sizeof(uint), resses);
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
                SysConsole.Output(OutputType.DEBUG, "Found " + resses[0]);
            }
            // Prep VAO
            int vao = GL.GenVertexArray();
            GL.BindVertexArray(vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, newBufs[0]);
            GL.VertexAttribPointer(0, 4, VertexAttribPointerType.Float, false, 0, 0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, newBufs[1]);
            GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 0, 0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, newBufs[2]);
            GL.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, 0, 0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, newBufs[3]);
            GL.VertexAttribPointer(3, 4, VertexAttribPointerType.Float, false, 0, 0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, newBufs[4]);
            GL.VertexAttribPointer(4, 4, VertexAttribPointerType.Float, false, 0, 0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, newBufs[5]);
            GL.VertexAttribPointer(5, 4, VertexAttribPointerType.Float, false, 0, 0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, newBufs[6]);
            GL.VertexAttribPointer(6, 4, VertexAttribPointerType.Float, false, 0, 0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, newBufs[7]);
            GL.VertexAttribPointer(7, 4, VertexAttribPointerType.Float, false, 0, 0);
            GL.EnableVertexAttribArray(0);
            GL.EnableVertexAttribArray(1);
            GL.EnableVertexAttribArray(2);
            GL.EnableVertexAttribArray(3);
            GL.EnableVertexAttribArray(4);
            GL.EnableVertexAttribArray(5);
            GL.EnableVertexAttribArray(6);
            GL.EnableVertexAttribArray(7);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, newBufs[8]);
            GL.BindVertexArray(0);
            // Move buffers to chunk VBO
            ch._VBOSolid?.Destroy();
            ch._VBOTransp?.Destroy();
            ChunkVBO vbo = new ChunkVBO()
            {
                generated = true,
                _VertexVBO = newBufs[0],
                _NormalVBO = newBufs[1],
                _TexCoordVBO = newBufs[2],
                _TangentVBO = newBufs[3],
                _ColorVBO = newBufs[4],
                _TCOLVBO = newBufs[5],
                _THVVBO = newBufs[6],
                _THWVBO = newBufs[7],
                _IndexVBO = newBufs[8],
                _VAO = vao,
                vC = resd,
                colors = true,
                tcols = true,
                reusable = false
            };
            ch._VBOSolid = vbo;
            ch._VBOTransp = vbo;
            // Clean up buffers
            GL.DeleteBuffer(VoxelBuffer);
            GL.DeleteBuffer(resBuf);
            View3D.CheckError("Compute - Finalize");
            return resd;
        }

        public void Destroy()
        {
            // Clean up shader
            GL.DeleteTexture(Texture_IDs);
            GL.DeleteProgram(Program_Counter);
            GL.DeleteProgram(Program_Cruncher);
            View3D.CheckError("Compute - Shutdown");
        }
    }
}
