//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using Voxalia.Shared;
using Voxalia.Shared.Files;
using FreneticScript;
using Voxalia.ServerGame.ServerMainSystem;
using FreneticGameCore;

namespace Voxalia.ServerGame.OtherSystems
{
    public class ModelEngine
    {
        public Dictionary<string, Model> Models = new Dictionary<string, Model>();

        public ModelHandler handler = new ModelHandler();

        public Server TheServer;

        public ModelEngine(Server tserver)
        {
            TheServer = tserver;
        }

        public Model GetModel(string name)
        {
            string nl = name.ToLowerFast();
            Model temp;
            if (Models.TryGetValue(nl, out temp))
            {
                return temp;
            }
            temp = LoadModel(nl);
            if (temp != null)
            {
                Models.Add(nl, temp);
                return temp;
            }
            temp = LoadModel("cube");
            temp.Name = nl;
            Models.Add(nl, temp);
            return temp;
        }

        Model LoadModel(string name)
        {
            string n = "models/" + name + ".vmd";
            try
            {
                if (TheServer.Files.Exists(n))
                {
                    byte[] dat = TheServer.Files.ReadBytes(n);
                    Model3D scene = handler.LoadModel(dat);
                    return new Model() { Name = name, Original = scene };
                }
            }
            catch (Exception ex)
            {
                Utilities.CheckException(ex);
                SysConsole.Output(OutputType.ERROR, ex.ToString());
            }
            return null;
        }
    }

    public class Model
    {
        public string Name;
        public Model3D Original;
    }
}
