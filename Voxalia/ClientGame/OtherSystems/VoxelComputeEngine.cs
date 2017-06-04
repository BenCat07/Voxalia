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
using FreneticGameCore.Collision;
using System.Diagnostics;

namespace Voxalia.ClientGame.OtherSystems
{
    public class VoxelComputeEngine
    {
        public int Texture_IDs;

        public Client TheClient;

        public static readonly int[] Reppers = new int[] { 30, 15, 6, 5, 2 };

        public int[] Program_Counter = new int[Reppers.Length];

        public int[] Program_Cruncher1 = new int[Reppers.Length];
        public int[] Program_Cruncher2 = new int[Reppers.Length];
        public int[] Program_Cruncher3 = new int[Reppers.Length];

        public int[] Program_Combo = new int[Reppers.Length];

        public static readonly Dictionary<int, int> lookuper = new Dictionary<int, int>(128)
        {
            { 30, 0 },
            { 15, 1 },
            { 6, 2 },
            { 5, 3 },
            { 2, 4 }
        };

        public int[] EmptyChunkRep = new int[Reppers.Length];

        public void Init(Client tclient)
        {
            TheClient = tclient;
            for (int i = 0; i < Reppers.Length; i++)
            {
                Program_Counter[i] = TheClient.Shaders.CompileCompute("vox_count", "#define MCM_VOX_COUNT " + Reppers[i] + "\n");
                Program_Cruncher1[i] = TheClient.Shaders.CompileCompute("vox_crunch", "#define MCM_VOX_COUNT " + Reppers[i] + "\n#define MODE_ONE 1\n");
                Program_Cruncher2[i] = TheClient.Shaders.CompileCompute("vox_crunch", "#define MCM_VOX_COUNT " + Reppers[i] + "\n#define MODE_TWO 1\n");
                Program_Cruncher3[i] = TheClient.Shaders.CompileCompute("vox_crunch", "#define MCM_VOX_COUNT " + Reppers[i] + "\n#define MODE_THREE 1\n");
                Program_Combo[i] = TheClient.Shaders.CompileCompute("vox_combo", "#define MCM_VOX_COUNT " + Reppers[i] + "\n");
            }
            View3D.CheckError("Compute - Startup - Shaders");
            float[] df = new float[MaterialHelpers.ALL_MATS.Count * 7 * 6];
            for (int i = 0; i < MaterialHelpers.ALL_MATS.Count; i++)
            {
                for (int x = 0; x < 6; x++)
                {
                    int cnt = Math.Min(6, MaterialHelpers.ALL_MATS[i].TID[x].Length);
                    for (int y = 0; y < cnt; y++)
                    {
                        df[(1 + y + x * 7) * MaterialHelpers.ALL_MATS.Count + i] = MaterialHelpers.ALL_MATS[i].TID[x][y];
                    }
                    df[(x * 7) * MaterialHelpers.ALL_MATS.Count + i] = cnt;
                }
            }
            Texture_IDs = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, Texture_IDs);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R32f, MaterialHelpers.ALL_MATS.Count, 6 * 7, 0, PixelFormat.Red, PixelType.Float, df);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.Finish();
            GL.MemoryBarrier(MemoryBarrierFlags.AllBarrierBits);
            View3D.CheckError("Compute - Startup - Texture");
            for (int i = 0; i < Reppers.Length; i++)
            {
                int csize = Reppers[i];
                int[] btemp = new int[csize * csize * csize * 4];
                BlockInternal bi = new BlockInternal((ushort)Material.STONE, 0, 0, 0);
                for (int rz = 0; rz < csize; rz++)
                {
                    for (int ry = 0; ry < csize; ry++)
                    {
                        for (int rx = 0; rx < csize; rx++)
                        {
                            int ind = (rz * (csize * csize) + ry * csize + rx) * 4;
                            btemp[ind + 0] = bi._BlockMaterialInternal;
                            btemp[ind + 1] = bi.BlockLocalData;
                            btemp[ind + 2] = bi.BlockData;
                            btemp[ind + 3] = bi._BlockPaintInternal;
                        }
                    }
                }
                EmptyChunkRep[i] = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, EmptyChunkRep[i]);
                GL.BufferData(BufferTarget.ShaderStorageBuffer, btemp.Length * sizeof(int), btemp, BufferUsageHint.StaticDraw);
            }
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
        }

        public static readonly Vector3i[] Relatives = new Vector3i[]
        {
            new Vector3i(0, 0, 0),
            new Vector3i(1, 0, 0), new Vector3i(-1, 0, 0),
            new Vector3i(0,-1, 0), new Vector3i(0, -1, 0),
            new Vector3i(0, 0, 1), new Vector3i(0, 0, -1)
        };

        const BufferUsageHint hintter = BufferUsageHint.DynamicRead;

        public Stopwatch sw1 = new Stopwatch(), sw2 = new Stopwatch(), sw3 = new Stopwatch(), sw1a = new Stopwatch();

        public void Calc(params Chunk[] chs)
        {
            sw1.Start();
            // Create voxel buffer data
            for (int chz = 0; chz < chs.Length; chz++)
            {
                Chunk ch = chs[chz];
                ch.Render_BufsRel = new int[7];
                int len = ch.CSize * ch.CSize * ch.CSize * 4;
                for (int x = 0; x < Relatives.Length; x++)
                {
                    Chunk rel = x == 0 ? ch : TheClient.TheRegion.GetChunk(ch.WorldPosition + Relatives[x]);
                    int[] btemp;
                    if (rel == null)
                    {
                        ch.Render_BufsRel[x] = EmptyChunkRep[lookuper[ch.CSize]];
                    }
                    else if (rel.Render_VoxelBuffer == null || rel.Render_VoxelBuffer[lookuper[ch.CSize]] <= 0)
                    {
                        btemp = new int[len];
                        for (int rz = 0; rz < ch.CSize; rz++)
                        {
                            for (int ry = 0; ry < ch.CSize; ry++)
                            {
                                for (int rx = 0; rx < ch.CSize; rx++)
                                {
                                    BlockInternal bi = ch.GetLODRelative(rel, rx, ry, rz);
                                    int ind = (rz * (ch.CSize * ch.CSize) + ry * ch.CSize + rx) * 4;
                                    btemp[ind + 0] = bi._BlockMaterialInternal;
                                    btemp[ind + 1] = bi.BlockLocalData;
                                    btemp[ind + 2] = bi.BlockData;
                                    btemp[ind + 3] = bi._BlockPaintInternal;
                                }
                            }
                        }
                        int VoxelBuffer = GL.GenBuffer();
                        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, VoxelBuffer);
                        GL.BufferData(BufferTarget.ShaderStorageBuffer, btemp.Length * sizeof(int), btemp, BufferUsageHint.StaticDraw);
                        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
                        if (rel.Render_VoxelBuffer == null)
                        {
                            rel.Render_VoxelBuffer = new int[Reppers.Length];
                        }
                        rel.Render_VoxelBuffer[lookuper[ch.CSize]] = VoxelBuffer;
                        ch.Render_BufsRel[x] = VoxelBuffer;
                    }
                    else
                    {
                        ch.Render_BufsRel[x] = rel.Render_VoxelBuffer[lookuper[ch.CSize]];
                    }
                }
                View3D.CheckError("Compute - Prep -1");
                // Combine buffers
                sw1a.Start();
                int fbufVoxels = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, fbufVoxels);
                GL.BufferData(BufferTarget.ShaderStorageBuffer, len * 7 * sizeof(uint), IntPtr.Zero, hintter);
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
                View3D.CheckError("Compute - Prep -0.6");
                GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, fbufVoxels);
                View3D.CheckError("Compute - Prep -0.5");
                for (int i = 0; i < 7; i++)
                {
                    GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1 + i, ch.Render_BufsRel[i]);
                }
                View3D.CheckError("Compute - Prep -0.4");
                GL.UseProgram(Program_Combo[lookuper[ch.CSize]]);
                View3D.CheckError("Compute - Prep -0.37");
                GL.DispatchCompute(1, ch.CSize, 1);
                View3D.CheckError("Compute - Prep -0.35");
                GL.UseProgram(0);
                View3D.CheckError("Compute - Prep -0.3");
                for (int i = 0; i < 8; i++)
                {
                    GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, i, 0);
                }
                ch.Render_FBB = fbufVoxels;
                View3D.CheckError("Compute - Prep -0.25");
                sw1a.Stop();
            }
            sw1.Stop();
            sw2.Start();
            View3D.CheckError("Compute - Prep 0");
            // Create a results buffer
            for (int chz = 0; chz < chs.Length; chz++)
            {
                Chunk ch = chs[chz];
                ch.Render_IndBuf = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, ch.Render_IndBuf);
                GL.BufferData(BufferTarget.ShaderStorageBuffer, ch.CSize * ch.CSize * ch.CSize * sizeof(uint), IntPtr.Zero, hintter);
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
                GL.UseProgram(Program_Counter[lookuper[ch.CSize]]);
                int resBuf = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, resBuf);
                uint[] resses = new uint[1];
                GL.BufferData(BufferTarget.ShaderStorageBuffer, sizeof(uint), resses, hintter);
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
                // Run the shader
                GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, ch.Render_FBB);
                GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 2, resBuf);
                GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 3, ch.Render_IndBuf);
                GL.DispatchCompute(1, ch.CSize, 1);
                ch.Render_ResBuf = resBuf;
            }
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, 0);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 2, 0);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 3, 0);
            GL.UseProgram(0);
            View3D.CheckError("Compute - Run");
            // Gather results
            GL.MemoryBarrier(MemoryBarrierFlags.ShaderStorageBarrierBit);
            for (int chz = 0; chz < chs.Length; chz++)
            {
                uint[] resses = new uint[1];
                Chunk ch = chs[chz];
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, ch.Render_ResBuf);
                GL.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, sizeof(uint), resses);
                GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
                ch.CountForRender = (int)resses[0];
            }
            sw2.Stop();
            sw3.Start();
            View3D.CheckError("Compute - Read Count");
            // Start new buffers
            GL.BindImageTexture(0, Texture_IDs, 0, false, 0, TextureAccess.ReadOnly, SizedInternalFormat.R32f);
            for (int chz = 0; chz < chs.Length; chz++)
            {
                Chunk ch = chs[chz];
                byte[] minimum_needed = null;// new byte[resd * Vector4.SizeInBytes];
                int resd = ch.CountForRender;
                uint[] newBufs = new uint[9];
                GL.GenBuffers(9, newBufs);
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
                GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, ch.Render_FBB);
                GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 2, ch.Render_IndBuf);
                // Run computation MODE_ONE
                GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 3, newBufs[0]);
                GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 4, newBufs[1]);
                GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 5, newBufs[2]);
                GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 6, newBufs[3]);
                GL.UseProgram(Program_Cruncher1[lookuper[ch.CSize]]);
                GL.DispatchCompute(1, ch.CSize, 1);
                // Run computation MODE_TWO
                GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 3, newBufs[4]);
                GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 4, newBufs[5]);
                GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 5, newBufs[8]);
                GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 6, 0);
                GL.UseProgram(Program_Cruncher2[lookuper[ch.CSize]]);
                GL.DispatchCompute(1, ch.CSize, 1);
                // Run computation MODE_THREE
                GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 3, newBufs[6]);
                GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 4, newBufs[7]);
                GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 5, 0);
                GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 6, 0);
                GL.UseProgram(Program_Cruncher3[lookuper[ch.CSize]]);
                GL.DispatchCompute(1, ch.CSize, 1);
                GL.UseProgram(0);
                View3D.CheckError("Compute - Crunch");
                // Unbind new buffers
                GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, 0);
                GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 2, 0);
                GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 3, 0);
                GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 4, 0);
                GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 5, 0);
                GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 6, 0);
                GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 7, 0);
                View3D.CheckError("Compute - End Buffs");
                // OPTIONAL, REMOVE ME:
#if EXTRA_DEBUG
                {
                    GL.Finish();
                    GL.MemoryBarrier(MemoryBarrierFlags.AllBarrierBits);
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
                }
#endif
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
                GL.DeleteBuffer(ch.Render_ResBuf);
                GL.DeleteBuffer(ch.Render_FBB);
                GL.DeleteBuffer(ch.Render_IndBuf);
                ch.Render_ResBuf = 0;
                ch.Render_IndBuf = 0;
                ch.Render_BufsRel = null;
            }
            GL.BindImageTexture(0, 0, 0, false, 0, TextureAccess.ReadOnly, SizedInternalFormat.R32f);
            View3D.CheckError("Compute - Finalize");
            sw3.Stop();
        }

        public void Destroy()
        {
            // Clean up shader
            GL.DeleteTexture(Texture_IDs);
            for (int i = 0; i < Reppers.Length; i++)
            {
                GL.DeleteProgram(Program_Counter[i]);
                GL.DeleteProgram(Program_Cruncher1[i]);
                GL.DeleteProgram(Program_Cruncher2[i]);
                GL.DeleteProgram(Program_Cruncher3[i]);
            }
            View3D.CheckError("Compute - Shutdown");
        }
    }
}
