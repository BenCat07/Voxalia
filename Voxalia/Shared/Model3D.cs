//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System.Collections.Generic;
using BEPUutilities;

namespace Voxalia.Shared
{
    /// <summary>
    /// Represents an abstract 3D model.
    /// </summary>
    public class Model3D
    {
        public List<Model3DMesh> Meshes;
        public Model3DNode RootNode;
        public Matrix MatrixA;
    }

    /// <summary>
    /// Represents a single mesh of an abstract 3D model.
    /// </summary>
    public class Model3DMesh
    {
        public List<Vector3> Vertices;
        public List<int> Indices;
        public List<Vector3> Normals;
        public List<Vector2> TexCoords;
        public List<Model3DBone> Bones;
        public string Name;
    }

    /// <summary>
    /// Represents a single bone in an abstract 3D model mesh.
    /// </summary>
    public class Model3DBone
    {
        public string Name;
        public List<int> IDs;
        public List<double> Weights;
        public Matrix MatrixA;
    }

    /// <summary>
    /// Represents a single node in an abstract 3D model mesh.
    /// </summary>
    public class Model3DNode
    {
        public string Name;
        public Matrix MatrixA;
        public Model3DNode Parent;
        public List<Model3DNode> Children;
    }
}
