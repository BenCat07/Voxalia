//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016-2017 FreneticXYZ under the terms of a strict license.
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

namespace Voxalia.ClientGame.AudioSystem.Enforcer
{
    public class LiveAudioInstance
    {
        public LiveAudioClip Clip = null;

        public int CurrentSample = 0;

        public bool Loop = false;

        public Location Position = Location.Zero;

        public Location Velocity = Location.Zero;

        public float Gain = 1f;

        public float Pitch = 1f;

        public bool UsePosition = false;

        public AudioState State = AudioState.WAITING;
    }
}