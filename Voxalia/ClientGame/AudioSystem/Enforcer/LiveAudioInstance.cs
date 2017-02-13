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
