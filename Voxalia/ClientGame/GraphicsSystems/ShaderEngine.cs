﻿using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;
using Voxalia.Shared;
using FreneticScript;
using Voxalia.Shared.Files;
using Voxalia.ClientGame.ClientMainSystem;

namespace Voxalia.ClientGame.GraphicsSystems
{
    public class ShaderEngine
    {
        /// <summary>
        /// A full list of currently loaded shaders.
        /// TODO: List->Dictionary?
        /// </summary>
        public List<Shader> LoadedShaders;

        /// <summary>
        /// A common shader that multiplies colors.
        /// </summary>
        public Shader ColorMultShader;

        /// <summary>
        /// A common shader that removes black color.
        /// </summary>
        public Shader TextCleanerShader;

        public Client TheClient;

        /// <summary>
        /// Starts or restarts the shader system.
        /// </summary>
        public void InitShaderSystem(Client tclient)
        {
            TheClient = tclient;
            // Reset shader list
            LoadedShaders = new List<Shader>();
            // Pregenerate a few needed shader
            ColorMultShader = GetShader("color_mult");
            TextCleanerShader = GetShader("text_cleaner");
        }

        public bool MCM_GOOD_GRAPHICS = true;

        public void Update(double time)
        {
            cTime = time;
        }

        public void Clear()
        {
            for (int i = 0; i < LoadedShaders.Count; i++)
            {
                LoadedShaders[i].Original_Program = -1;
                LoadedShaders[i].Internal_Program = -1;
                LoadedShaders[i].Destroy();
            }
            LoadedShaders.Clear();
        }

        public double cTime = 0;

        /// <summary>
        /// Gets the shader object for a specific shader name.
        /// </summary>
        /// <param name="shadername">The name of the shader.</param>
        /// <returns>A valid shader object.</returns>
        public Shader GetShader(string shadername)
        {
            for (int i = 0; i < LoadedShaders.Count; i++)
            {
                if (LoadedShaders[i].Name == shadername)
                {
                    return LoadedShaders[i];
                }
            }
            Shader Loaded = LoadShader(shadername);
            if (Loaded == null)
            {
                Loaded = new Shader();
                Loaded.Name = shadername;
                Loaded.Internal_Program = ColorMultShader.Original_Program;
                Loaded.Original_Program = ColorMultShader.Original_Program;
                Loaded.LoadedProperly = false;
                Loaded.Engine = this;
            }
            LoadedShaders.Add(Loaded);
            return Loaded;
        }

        /// <summary>
        /// Loads a shader from file.
        /// </summary>
        /// <param name="filename">The name of the file to use.</param>
        /// <returns>The loaded shader, or null if it does not exist.</returns>
        public Shader LoadShader(string filename)
        {
            try
            {
                string[] dat1 = filename.SplitFast('#', 1);
                string[] vars = new string[0];
                if (dat1.Length == 2)
                {
                    vars = dat1[1].SplitFast(',');
                }
                filename = FileHandler.CleanFileName(dat1[0]);
                if (!TheClient.Files.Exists("shaders/" + filename + ".vs"))
                {
                    SysConsole.Output(OutputType.ERROR, "Cannot load shader, file '" +
                        TextStyle.Color_Standout + "shaders/" + filename + ".vs" + TextStyle.Color_Error +
                        "' does not exist.");
                    return null;
                }
                if (!TheClient.Files.Exists("shaders/" + filename + ".fs"))
                {
                    SysConsole.Output(OutputType.ERROR, "Cannot load shader, file '" +
                        TextStyle.Color_Standout + "shaders/" + filename + ".fs" + TextStyle.Color_Error +
                        "' does not exist.");
                    return null;
                }
                string VS = TheClient.Files.ReadText("shaders/" + filename + ".vs");
                string FS = TheClient.Files.ReadText("shaders/" + filename + ".fs");
                return CreateShader(VS, FS, filename, vars);
            }
            catch (Exception ex)
            {
                SysConsole.Output(OutputType.ERROR, "Failed to load shader from filename '" +
                    TextStyle.Color_Standout + "shaders/" + filename + ".fs or .vs" + TextStyle.Color_Error + "': " + ex.ToString());
                return null;
            }
        }

        /// <summary>
        /// Creates a full Shader object for a VS/FS input.
        /// </summary>
        /// <param name="VS">The input VertexShader code.</param>
        /// <param name="FS">The input FragmentShader code.</param>
        /// <param name="name">The name of the shader.</param>
        /// <param name="vars">The variables to use.</param>
        /// <returns>A valid Shader object.</returns>
        public Shader CreateShader(string VS, string FS, string name, string[] vars)
        {
            int Program = CompileToProgram(VS, FS, vars);
            Shader generic = new Shader();
            generic.Name = name;
            generic.LoadedProperly = true;
            generic.Internal_Program = Program;
            generic.Original_Program = Program;
            generic.Vars = vars;
            generic.Engine = this;
            return generic;
        }

        /// <summary>
        /// Compiles a VertexShader and FragmentShader to a usable shader program.
        /// </summary>
        /// <param name="VS">The input VertexShader code.</param>
        /// <param name="FS">The input FragmentShader code.</param>
        /// <param name="vars">All variables to include.</param>
        /// <returns>The internal OpenGL program ID.</returns>
        public int CompileToProgram(string VS, string FS, string[] vars)
        {
            for (int i = 0; i < vars.Length; i++)
            {
                if (vars[i].Length > 0)
                {
                    VS = VS.Replace("#define " + vars[i] + " 0", "#define " + vars[i] + " 1");
                    FS = FS.Replace("#define " + vars[i] + " 0", "#define " + vars[i] + " 1");
                }
            }
            int VertexObject = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(VertexObject, VS);
            GL.CompileShader(VertexObject);
            string VS_Info = GL.GetShaderInfoLog(VertexObject);
            int VS_Status = 0;
            GL.GetShader(VertexObject, ShaderParameter.CompileStatus, out VS_Status);
            if (VS_Status != 1)
            {
                throw new Exception("Error creating VertexShader. Error status: " + VS_Status + ", info: " + VS_Info);
            }
            int FragmentObject = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(FragmentObject, FS);
            GL.CompileShader(FragmentObject);
            string FS_Info = GL.GetShaderInfoLog(FragmentObject);
            int FS_Status = 0;
            GL.GetShader(FragmentObject, ShaderParameter.CompileStatus, out FS_Status);
            if (FS_Status != 1)
            {
                throw new Exception("Error creating FragmentShader. Error status: " + FS_Status + ", info: " + FS_Info);
            }
            int Program = GL.CreateProgram();
            GL.AttachShader(Program, FragmentObject);
            GL.AttachShader(Program, VertexObject);
            GL.LinkProgram(Program);
            string str = GL.GetProgramInfoLog(Program);
            if (str.Length != 0)
            {
                SysConsole.Output(OutputType.INFO, "Linked shader with message: '" + str + "'" + " -- FOR -- " + VS + " -- -- " + FS);
            }
            GL.DeleteShader(FragmentObject);
            GL.DeleteShader(VertexObject);
            return Program;
        }
    }

    /// <summary>
    /// Wraps an OpenGL shader.
    /// </summary>
    public class Shader
    {
        public Shader()
        {
            NewVersion = this;
        }

        public ShaderEngine Engine;

        /// <summary>
        /// The name of the shader
        /// </summary>
        public string Name;

        /// <summary>
        /// The shader this shader was remapped to.
        /// </summary>
        public Shader RemappedTo;

        /// <summary>
        /// The internal OpenGL ID for the shader program.
        /// </summary>
        public int Internal_Program;

        /// <summary>
        /// The original OpenGL ID that formed this shader program.
        /// </summary>
        public int Original_Program;

        /// <summary>
        /// All variables on this shader.
        /// </summary>
        public string[] Vars;

        /// <summary>
        /// Whether the shader loaded properly.
        /// </summary>
        public bool LoadedProperly = false;

        /// <summary>
        /// Destroys the OpenGL program that this shader wraps.
        /// </summary>
        public void Destroy()
        {
            if (Original_Program > -1 && GL.IsProgram(Original_Program))
            {
                GL.DeleteProgram(Original_Program);
                Original_Program = -1;
            }
        }

        /// <summary>
        /// Removes the shader from the system.
        /// </summary>
        public void Remove()
        {
            Destroy();
            Engine.LoadedShaders.Remove(this);
        }
        
        public double LastBindTime = 0;

        private Shader NewVersion = null;

        public void CheckValid()
        {
            if (Internal_Program == -1)
            {
                Shader temp = Engine.GetShader(Name);
                Original_Program = temp.Original_Program;
                Internal_Program = Original_Program;
                RemappedTo = temp;
                NewVersion = temp;
            }
            else if (RemappedTo != null)
            {
                RemappedTo.CheckValid();
                Internal_Program = RemappedTo.Original_Program;
            }
        }

        /// <summary>
        /// Binds this shader to OpenGL.
        /// </summary>
        public Shader Bind()
        {
            if (NewVersion != this)
            {
                return NewVersion.Bind();
            }
            LastBindTime = Engine.cTime;
            CheckValid();
            GL.UseProgram(Internal_Program);
            return NewVersion;
        }
    }
}
