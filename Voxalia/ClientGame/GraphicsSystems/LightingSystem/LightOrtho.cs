//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using OpenTK;
using Voxalia.ClientGame.ClientMainSystem;
using Voxalia.ClientGame.OtherSystems;

namespace Voxalia.ClientGame.GraphicsSystems.LightingSystem
{
    class LightOrtho: Light
    {
        public override Matrix4 GetMatrix()
        {
            Vector3d c = ClientUtilities.ConvertD(Client.Central.MainWorldView.RenderRelative);
            Vector3d e = eye - c;
            Vector3d d = target - c;
            return Matrix4.LookAt(new Vector3((float)e.X, (float)e.Y, (float)e.Z), new Vector3((float)d.X, (float)d.Y, (float)d.Z), up) * Matrix4.CreateOrthographicOffCenter(-FOV * 0.5f, FOV * 0.5f, -FOV * 0.5f, FOV * 0.5f, 1, maxrange);
        }
    }
}
