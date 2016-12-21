//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of the MIT license.
// See README.md or LICENSE.txt for contents of the MIT license.
// If these are not available, see https://opensource.org/licenses/MIT
//

using System;
using System.Collections.Generic;
using BEPUutilities;
using BEPUphysics.CollisionShapes;
using BEPUphysics.CollisionShapes.ConvexShapes;

namespace Voxalia.Shared.BlockShapes
{
    /// <summary>
    /// The default cubic block.
    /// </summary>
    public class BSD0: BlockShapeDetails
    {
        public BSD0()
        {
            BlockShapeCache = new BoxShape(1f, 1f, 1f);
            OffsetCache = new Location(0.5);
            ShrunkBlockShapeCache = new BoxShape(SHRINK_CONSTANT, SHRINK_CONSTANT, SHRINK_CONSTANT);
            ShrunkOffsetCache = new Location(SHRINK_CONSTANT * 0.5);
        }

        public override List<Vector3> GetVertices(Vector3 pos, bool XP, bool XM, bool YP, bool YM, bool TOP, bool BOTTOM)
        {
            List<Vector3> Vertices = new List<Vector3>();
            if (!TOP)
            {
                Vertices.Add(new Vector3(pos.X, pos.Y + 1, pos.Z + 1));
                Vertices.Add(new Vector3(pos.X + 1, pos.Y + 1, pos.Z + 1));
                Vertices.Add(new Vector3(pos.X, pos.Y, pos.Z + 1));
                Vertices.Add(new Vector3(pos.X + 1, pos.Y + 1, pos.Z + 1));
                Vertices.Add(new Vector3(pos.X + 1, pos.Y, pos.Z + 1));
                Vertices.Add(new Vector3(pos.X, pos.Y, pos.Z + 1));
            }
            if (!BOTTOM)
            {
                Vertices.Add(new Vector3(pos.X, pos.Y, pos.Z));
                Vertices.Add(new Vector3(pos.X + 1, pos.Y, pos.Z));
                Vertices.Add(new Vector3(pos.X + 1, pos.Y + 1, pos.Z));
                Vertices.Add(new Vector3(pos.X, pos.Y, pos.Z));
                Vertices.Add(new Vector3(pos.X + 1, pos.Y + 1, pos.Z));
                Vertices.Add(new Vector3(pos.X, pos.Y + 1, pos.Z));
            }
            if (!XP)
            {
                Vertices.Add(new Vector3(pos.X + 1, pos.Y + 1, pos.Z + 1));
                Vertices.Add(new Vector3(pos.X + 1, pos.Y + 1, pos.Z));
                Vertices.Add(new Vector3(pos.X + 1, pos.Y, pos.Z));
                Vertices.Add(new Vector3(pos.X + 1, pos.Y, pos.Z + 1));
                Vertices.Add(new Vector3(pos.X + 1, pos.Y + 1, pos.Z + 1));
                Vertices.Add(new Vector3(pos.X + 1, pos.Y, pos.Z));
            }
            if (!XM)
            {
                Vertices.Add(new Vector3(pos.X, pos.Y, pos.Z));
                Vertices.Add(new Vector3(pos.X, pos.Y + 1, pos.Z));
                Vertices.Add(new Vector3(pos.X, pos.Y + 1, pos.Z + 1));
                Vertices.Add(new Vector3(pos.X, pos.Y, pos.Z));
                Vertices.Add(new Vector3(pos.X, pos.Y + 1, pos.Z + 1));
                Vertices.Add(new Vector3(pos.X, pos.Y, pos.Z + 1));
            }
            if (!YP)
            {
                Vertices.Add(new Vector3(pos.X, pos.Y + 1, pos.Z));
                Vertices.Add(new Vector3(pos.X + 1, pos.Y + 1, pos.Z));
                Vertices.Add(new Vector3(pos.X + 1, pos.Y + 1, pos.Z + 1));
                Vertices.Add(new Vector3(pos.X, pos.Y + 1, pos.Z));
                Vertices.Add(new Vector3(pos.X + 1, pos.Y + 1, pos.Z + 1));
                Vertices.Add(new Vector3(pos.X, pos.Y + 1, pos.Z + 1));
            }
            if (!YM)
            {
                Vertices.Add(new Vector3(pos.X + 1, pos.Y, pos.Z + 1));
                Vertices.Add(new Vector3(pos.X + 1, pos.Y, pos.Z));
                Vertices.Add(new Vector3(pos.X, pos.Y, pos.Z));
                Vertices.Add(new Vector3(pos.X, pos.Y, pos.Z + 1));
                Vertices.Add(new Vector3(pos.X + 1, pos.Y, pos.Z + 1));
                Vertices.Add(new Vector3(pos.X, pos.Y, pos.Z));
            }
            return Vertices;
        }

        public override List<Vector3> GetNormals(Vector3 blockPos, bool XP, bool XM, bool YP, bool YM, bool TOP, bool BOTTOM)
        {
            List<Vector3> Norms = new List<Vector3>();
            if (!TOP)
            {
                for (int i = 0; i < 6; i++)
                {
                    Norms.Add(new Vector3(0, 0, 1));
                }
            }
            if (!BOTTOM)
            {
                for (int i = 0; i < 6; i++)
                {
                    Norms.Add(new Vector3(0, 0, -1));
                }
            }
            if (!XP)
            {
                for (int i = 0; i < 6; i++)
                {
                    Norms.Add(new Vector3(1, 0, 0));
                }
            }
            if (!XM)
            {
                for (int i = 0; i < 6; i++)
                {
                    Norms.Add(new Vector3(-1, 0, 0));
                }
            }
            if (!YP)
            {
                for (int i = 0; i < 6; i++)
                {
                    Norms.Add(new Vector3(0, 1, 0));
                }
            }
            if (!YM)
            {
                for (int i = 0; i < 6; i++)
                {
                    Norms.Add(new Vector3(0, -1, 0));
                }
            }
            return Norms;
        }

        public override List<Vector3> GetTCoords(Vector3 blockPos, Material mat, bool XP, bool XM, bool YP, bool YM, bool TOP, bool BOTTOM)
        {
            List<Vector3> TCoords = new List<Vector3>();
            if (!TOP)
            {
                int tID_TOP = mat.TextureID(MaterialSide.TOP);
                TCoords.Add(new Vector3(0, 1, tID_TOP));
                TCoords.Add(new Vector3(1, 1, tID_TOP));
                TCoords.Add(new Vector3(0, 0, tID_TOP));
                TCoords.Add(new Vector3(1, 1, tID_TOP));
                TCoords.Add(new Vector3(1, 0, tID_TOP));
                TCoords.Add(new Vector3(0, 0, tID_TOP));
            }
            if (!BOTTOM)
            {
                int tID_BOTTOM = mat.TextureID(MaterialSide.BOTTOM);
                TCoords.Add(new Vector3(0, 0, tID_BOTTOM));
                TCoords.Add(new Vector3(1, 1, tID_BOTTOM));
                TCoords.Add(new Vector3(0, 1, tID_BOTTOM));
                TCoords.Add(new Vector3(0, 0, tID_BOTTOM));
                TCoords.Add(new Vector3(1, 0, tID_BOTTOM));
                TCoords.Add(new Vector3(1, 1, tID_BOTTOM));
            }
            if (!XP)
            {
                int tID_XP = mat.TextureID(MaterialSide.XP);
                TCoords.Add(new Vector3(1, 0, tID_XP));
                TCoords.Add(new Vector3(1, 1, tID_XP));
                TCoords.Add(new Vector3(0, 1, tID_XP));
                TCoords.Add(new Vector3(0, 0, tID_XP));
                TCoords.Add(new Vector3(1, 0, tID_XP));
                TCoords.Add(new Vector3(0, 1, tID_XP));
            }
            if (!XM)
            {
                int tID_XM = mat.TextureID(MaterialSide.XM);
                TCoords.Add(new Vector3(1, 1, tID_XM));
                TCoords.Add(new Vector3(0, 1, tID_XM));
                TCoords.Add(new Vector3(0, 0, tID_XM));
                TCoords.Add(new Vector3(1, 1, tID_XM));
                TCoords.Add(new Vector3(0, 0, tID_XM));
                TCoords.Add(new Vector3(1, 0, tID_XM));
            }
            if (!YP)
            {
                int tID_YP = mat.TextureID(MaterialSide.YP);
                TCoords.Add(new Vector3(1, 1, tID_YP));
                TCoords.Add(new Vector3(0, 1, tID_YP));
                TCoords.Add(new Vector3(0, 0, tID_YP));
                TCoords.Add(new Vector3(1, 1, tID_YP));
                TCoords.Add(new Vector3(0, 0, tID_YP));
                TCoords.Add(new Vector3(1, 0, tID_YP));
            }
            if (!YM)
            {
                int tID_YM = mat.TextureID(MaterialSide.YM);
                TCoords.Add(new Vector3(1, 0, tID_YM));
                TCoords.Add(new Vector3(1, 1, tID_YM));
                TCoords.Add(new Vector3(0, 1, tID_YM));
                TCoords.Add(new Vector3(0, 0, tID_YM));
                TCoords.Add(new Vector3(1, 0, tID_YM));
                TCoords.Add(new Vector3(0, 1, tID_YM));
            }
            return TCoords;
        }

        public override Tuple<List<Vector4>, List<Vector4>, List<Vector4>, List<Vector4>> GetStretchData(Vector3 blockpos, List<Vector3> vertices, BlockInternal XP, BlockInternal XM,
            BlockInternal YP, BlockInternal YM, BlockInternal ZP, BlockInternal ZM, bool bxp, bool bxm, bool byp, bool bym, bool bzp, bool bzm,
            BlockInternal ZPXP, BlockInternal ZPXM, BlockInternal ZPYP, BlockInternal ZPYM, BlockInternal XPYP, BlockInternal XPYM, BlockInternal XMYP, BlockInternal XMYM)
        {
            List<Vector4> sdat = new List<Vector4>();
            List<Vector4> swei = new List<Vector4>();
            List<Vector4> sdat2 = new List<Vector4>();
            List<Vector4> swei2 = new List<Vector4>();
            if (!bzp)
            {
                double txp = XP.Material.TextureID(MaterialSide.TOP);
                double txm = XM.Material.TextureID(MaterialSide.TOP);
                double typ = YP.Material.TextureID(MaterialSide.TOP);
                double tym = YM.Material.TextureID(MaterialSide.TOP);
                double vxp = XP.IsOpaque() ? 1 : 0;
                double vxm = XM.IsOpaque() ? 1 : 0;
                double vyp = YP.IsOpaque() ? 1 : 0;
                double vym = YM.IsOpaque() ? 1 : 0;
                double txpyp = XPYP.Material.TextureID(MaterialSide.TOP);
                double txpym = XPYM.Material.TextureID(MaterialSide.TOP);
                double txmyp = XPYP.Material.TextureID(MaterialSide.TOP);
                double txmym = XMYM.Material.TextureID(MaterialSide.TOP);
                double vxpyp = XPYP.IsOpaque() ? 1 : 0;
                double vxpym = XPYM.IsOpaque() ? 1 : 0;
                double vxmyp = XMYP.IsOpaque() ? 1 : 0;
                double vxmym = XMYM.IsOpaque() ? 1 : 0;
                if (XP.BlockShareTex)
                {
                    vxp *= 0.5;
                }
                if (XM.BlockShareTex)
                {
                    vxm *= 0.5;
                }
                if (YP.BlockShareTex)
                {
                    vyp *= 0.5;
                }
                if (YM.BlockShareTex)
                {
                    vym *= 0.5;
                }
                if (XPYP.BlockShareTex)
                {
                    vxpyp *= 0.5;
                }
                if (XPYM.BlockShareTex)
                {
                    vxpym *= 0.5;
                }
                if (XMYP.BlockShareTex)
                {
                    vxmyp *= 0.5;
                }
                if (XMYM.BlockShareTex)
                {
                    vxmym *= 0.5;
                }
                for (int i = 0; i < 6; i++)
                {
                    sdat.Add(new Vector4(txp, txm, typ, tym));
                    swei.Add(new Vector4(vxp, vxm, vyp, vym));
                    sdat2.Add(new Vector4(txpyp, txpym, txmyp, txmym));
                    swei2.Add(new Vector4(vxpyp, vxpym, vxmyp, vxmym));
                }
            }
            if (!bzm)
            {
                for (int i = 0; i < 6; i++)
                {
                    sdat.Add(new Vector4(5, 5, 5, 5));
                    swei.Add(new Vector4(1, 1, 1, 1));
                    sdat2.Add(new Vector4(5, 5, 5, 5));
                    swei2.Add(new Vector4(1, 1, 1, 1));
                }
            }
            if (!bxp)
            {
                for (int i = 0; i < 6; i++)
                {
                    sdat.Add(new Vector4(5, 5, 5, 5));
                    swei.Add(new Vector4(1, 1, 1, 1));
                    sdat2.Add(new Vector4(5, 5, 5, 5));
                    swei2.Add(new Vector4(1, 1, 1, 1));
                }
            }
            if (!bxm)
            {
                for (int i = 0; i < 6; i++)
                {
                    sdat.Add(new Vector4(5, 5, 5, 5));
                    swei.Add(new Vector4(1, 1, 1, 1));
                    sdat2.Add(new Vector4(5, 5, 5, 5));
                    swei2.Add(new Vector4(1, 1, 1, 1));
                }
            }
            if (!byp)
            {
                for (int i = 0; i < 6; i++)
                {
                    sdat.Add(new Vector4(5, 5, 5, 5));
                    swei.Add(new Vector4(1, 1, 1, 1));
                    sdat2.Add(new Vector4(5, 5, 5, 5));
                    swei2.Add(new Vector4(1, 1, 1, 1));
                }
            }
            if (!bym)
            {
                for (int i = 0; i < 6;i++)
                {
                    sdat.Add(new Vector4(5, 5, 5, 5));
                    swei.Add(new Vector4(1, 1, 1, 1));
                    sdat2.Add(new Vector4(5, 5, 5, 5));
                    swei2.Add(new Vector4(1, 1, 1, 1));
                }
            }
            return new Tuple<List<Vector4>, List<Vector4>, List<Vector4>, List<Vector4>>(sdat, swei, sdat2, swei2);
        }

        public override bool OccupiesXP()
        {
            return true;
        }

        public override bool OccupiesYP()
        {
            return true;
        }

        public override bool OccupiesXM()
        {
            return true;
        }

        public override bool OccupiesYM()
        {
            return true;
        }

        public override bool OccupiesTOP()
        {
            return true;
        }

        public override bool OccupiesBOTTOM()
        {
            return true;
        }
    }
}
