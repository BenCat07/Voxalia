using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voxalia.ClientGame.AudioSystem.Enforcer
{
    public enum AudioState : byte
    {
        WAITING = 0,
        PLAYING = 1,
        DONE = 2,
        STOP = 3,
        PAUSED = 4
    }
}
