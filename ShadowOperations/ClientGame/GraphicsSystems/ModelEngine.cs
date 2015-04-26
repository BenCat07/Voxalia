﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ShadowOperations.Shared;
using Frenetic;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;

namespace ShadowOperations.ClientGame.GraphicsSystems
{
    public class ModelEngine
    {
        /// <summary>
        /// All currently loaded models.
        /// </summary>
        public List<Model> LoadedModels;

        public Model Cube;

        /// <summary>
        /// Prepares the model system.
        /// </summary>
        public void Init()
        {
            LoadedModels = new List<Model>();
            Cube = FromString("cube", CubeData);
            LoadedModels.Add(Cube);
        }

        static string CubeData = "o Cube\nv 1.000000 0.000000 0.000000\nv 1.000000 0.000000 1.000000\nv 0.000000 0.000000 1.000000\n" +
            "v 0.000000 0.000000 0.000000\nv 1.000000 1.000000 0.000000\nv 1.000000 1.000000 1.000000\nv 0.000000 1.000000 1.000000\n" +
            "v 0.000000 1.000000 0.000000\nvt 1.000000 0.000000\nvt 1.000000 1.000000\nvt 0.000000 1.000000\nvt 0.000000 0.000000\n" +
            "f 2/1 3/2 4/3\nf 8/1 7/2 6/3\nf 1/4 5/1 6/2\nf 2/4 6/1 7/2\nf 7/1 8/2 4/3\nf 1/1 4/2 8/3\nf 1/4 2/1 4/3\nf 5/4 8/1 6/3\n" +
            "f 2/3 1/4 6/2\nf 3/3 2/4 7/2\nf 3/4 7/1 4/3\nf 5/4 1/1 8/3\n";

        public Model LoadModel(string filename)
        {
            try
            {
                filename = FileHandler.CleanFileName(filename);
                if (!FileHandler.Exists("models/" + filename + ".obj"))
                {
                    SysConsole.Output(OutputType.WARNING, "Cannot load model, file '" +
                        TextStyle.Color_Standout + "models/" + filename + ".obj" + TextStyle.Color_Warning +
                        "' does not exist.");
                    return null;
                }
                return FromString(filename, FileHandler.ReadText("models/" + filename + ".obj"));
            }
            catch (Exception ex)
            {
                SysConsole.Output(OutputType.ERROR, "Failed to load model from filename '" +
                    TextStyle.Color_Standout + "models/" + filename + ".obj" + TextStyle.Color_Error + "'" + ex.ToString());
                return null;
            }
        }

        /// <summary>
        /// Gets the texture object for a specific texture name.
        /// </summary>
        /// <param name="texturename">The name of the texture</param>
        /// <returns>A valid texture object</returns>
        public Model GetModel(string modelname)
        {
            modelname = FileHandler.CleanFileName(modelname);
            for (int i = 0; i < LoadedModels.Count; i++)
            {
                if (LoadedModels[i].Name == modelname)
                {
                    return LoadedModels[i];
                }
            }
            Model Loaded = LoadModel(modelname);
            if (Loaded == null)
            {
                Loaded = FromString(modelname, CubeData);
            }
            LoadedModels.Add(Loaded);
            return Loaded;
        }

        /// <summary>
        /// loads a model from a .obj file string
        /// </summary>
        /// <param name="name">The name of the model</param>
        /// <param name="data">The .obj file string</param>
        /// <returns>A valid model</returns>
        public Model FromString(string name, string data)
        {
            Model result = new Model(name);
            ModelMesh currentMesh = null;
            string[] lines = data.Replace("\r", "").Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains('#'))
                {
                    int index = lines[i].IndexOf('#');
                    if (index == 0)
                    {
                        continue;
                    }
                    lines[i] = lines[i].Substring(0, index);
                }
                string[] args = lines[i].Split(' ');
                if (args.Length <= 1)
                {
                    continue;
                }
                switch (args[0])
                {
                    case "mtllib":
                        break; // TODO: Maybe calculate basic materials?
                    case "usemtl":
                        break;
                    case "s": // Disregard, shading style is assumed by the engine, no need for the models to individually enable/disable it
                        break;
                    case "l": // Disregard, no need to render lines
                        break;
                    case "o":
                        currentMesh = new ModelMesh(args[1]);
                        result.Meshes.Add(currentMesh);
                        break;
                    case "v":
                        result.Vertices.Add(new Location(Utilities.StringToDouble(args[1]),
                            Utilities.StringToDouble(args[2]), Utilities.StringToDouble(args[3])));
                        break;
                    case "vt":
                        result.TextureCoords.Add(new Location(Utilities.StringToDouble(args[1]),
                            -Utilities.StringToDouble(args[2]), 0));
                        break;
                    case "f":
                        string[] a1s = args[1].Split('/');
                        string[] a2s = args[2].Split('/');
                        string[] a3s = args[3].Split('/');
                        int v1 = Utilities.StringToInt(a3s[0]);
                        int v2 = Utilities.StringToInt(a2s[0]);
                        int v3 = Utilities.StringToInt(a1s[0]);
                        int t1 = Utilities.StringToInt(a1s[1]);
                        int t2 = Utilities.StringToInt(a2s[1]);
                        int t3 = Utilities.StringToInt(a3s[1]);
                        // TODO: Handle missing texture coords gently?
                        Plane plane = new Plane(result.Vertices[v1 - 1], result.Vertices[v2 - 1], result.Vertices[v3 - 1]);
                        currentMesh.Faces.Add(new ModelFace(v1, v2, v3, t1, t2, t3, -plane.Normal));
                        break;
                    default:
                        SysConsole.Output(OutputType.WARNING, "Invalid model key '" + args[0] + "'");
                        break;
                }
            }
            result.GenerateVBO();
            return result;
        }
    }

    /// <summary>
    /// Represents a 3D model.
    /// </summary>
    public class Model
    {
        public Model(string _name)
        {
            Name = _name;
            Meshes = new List<ModelMesh>();
            Vertices = new List<Location>();
            TextureCoords = new List<Location>();
        }

        /// <summary>
        /// The name of  this model.
        /// </summary>
        public string Name;

        /// <summary>
        /// All the meshes this model has.
        /// </summary>
        public List<ModelMesh> Meshes;

        public void GenerateVBO()
        {
            for (int i = 0; i < Meshes.Count; i++)
            {
                List<Vector3> Vecs = new List<Vector3>(10);
                List<Vector3> Norms = new List<Vector3>(10);
                List<Vector3> Texs = new List<Vector3>(10);
                List<uint> Inds = new List<uint>(10);
                List<Vector4> Cols = new List<Vector4>(10);
                for (int x = 0; x < Meshes[i].Faces.Count; x++)
                {
                    Location normal = Meshes[i].Faces[x].Normal;
                    Norms.Add(new Vector3((float)normal.X, (float)normal.Y, (float)normal.Z));
                    Location vec1 = Vertices[Meshes[i].Faces[x].L1 - 1];
                    Vecs.Add(new Vector3((float)vec1.X, (float)vec1.Y, (float)vec1.Z));
                    Inds.Add((uint)(Vecs.Count - 1));
                    Location tex1 = TextureCoords[Meshes[i].Faces[x].T1 - 1];
                    Texs.Add(new Vector3((float)tex1.X, (float)tex1.Y, 0));
                    Norms.Add(new Vector3((float)normal.X, (float)normal.Y, (float)normal.Z));
                    Location vec2 = Vertices[Meshes[i].Faces[x].L2 - 1];
                    Vecs.Add(new Vector3((float)vec2.X, (float)vec2.Y, (float)vec2.Z));
                    Inds.Add((uint)(Vecs.Count - 1));
                    Location tex2 = TextureCoords[Meshes[i].Faces[x].T2 - 1];
                    Texs.Add(new Vector3((float)tex2.X, (float)tex2.Y, 0));
                    Norms.Add(new Vector3((float)normal.X, (float)normal.Y, (float)normal.Z));
                    Location vec3 = Vertices[Meshes[i].Faces[x].L3 - 1];
                    Vecs.Add(new Vector3((float)vec3.X, (float)vec3.Y, (float)vec3.Z));
                    Inds.Add((uint)(Vecs.Count - 1));
                    Location tex3 = TextureCoords[Meshes[i].Faces[x].T3 - 1];
                    Texs.Add(new Vector3((float)tex3.X, (float)tex3.Y, 0));
                    Cols.Add(new Vector4(1, 1, 1, 1));
                    Cols.Add(new Vector4(1, 1, 1, 1));
                    Cols.Add(new Vector4(1, 1, 1, 1));
                }
                Meshes[i].vbo.Vertices = Vecs;
                Meshes[i].vbo.Normals = Norms;
                Meshes[i].vbo.TexCoords = Texs;
                Meshes[i].vbo.Indices = Inds;
                Meshes[i].vbo.Colors = Cols;
                Meshes[i].GenerateVBO();
            }
        }
        
        /// <summary>
        /// Draws the model.
        /// </summary>
        public void Draw()
        {
            for (int i = 0; i < Meshes.Count; i++)
            {
                Meshes[i].Draw();
            }
        }

        /// <summary>
        /// Alll the models's vertices.
        /// </summary>
        public List<Location> Vertices;

        /// <summary>
        /// Alll the models's texture coords.
        /// </summary>
        public List<Location> TextureCoords;
    }

    public class ModelMesh
    {
        /// <summary>
        /// The name of this mesh.
        /// </summary>
        public string Name;

        public ModelMesh(string _name)
        {
            Name = _name;
            Faces = new List<ModelFace>();
            vbo = new VBO();
        }

        /// <summary>
        /// All the mesh's faces.
        /// </summary>
        public List<ModelFace> Faces;

        /// <summary>
        /// The VBO for this mesh.
        /// </summary>
        public VBO vbo;

        public void DestroyVBO()
        {
            vbo.Destroy();
        }

        public void GenerateVBO()
        {
            vbo.GenerateVBO();
        }

        /// <summary>
        /// Renders the mesh.
        /// </summary>
        public void Draw()
        {
            vbo.Render(false);
        }
    }

    public class ModelFace
    {
        public ModelFace(int _l1, int _l2, int _l3, int _t1, int _t2, int _t3, Location _normal)
        {
            L1 = _l1;
            L2 = _l2;
            L3 = _l3;
            T1 = _t1;
            T2 = _t2;
            T3 = _t3;
            Normal = _normal;
        }

        public Location Normal;

        public int L1;
        public int L2;
        public int L3;

        public int T1;
        public int T2;
        public int T3;
    }
}
