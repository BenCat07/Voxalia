//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for contents of the license.
// If neither of these are not available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using OpenTK;
using OpenTK.Audio.OpenAL;
using Voxalia.Shared;
using Voxalia.ClientGame.OtherSystems;

namespace Voxalia.ClientGame.AudioSystem
{
    public class ActiveSound
    {
        public SoundEngine Engine;

        public SoundEffect Effect;

        public Location Position;

        public ActiveSound(SoundEffect sfx)
        {
            Effect = sfx;
        }

        public bool Loop = false;

        public float Pitch = 1f;

        public float Gain = 1f;

        public int Src = -1;

        public bool Exists = false;

        public bool IsBackground = false;

        public bool Backgrounded = false;

        public void Create()
        {
            if (!Exists)
            {
                Engine.CheckError("PRECREATE:" + Effect.Name);
                Src = AL.GenSource();
                if (Src < 0 || AL.GetError() != ALError.NoError)
                {
                    Src = -1;
                    return;
                }
                AL.Source(Src, ALSourcei.Buffer, Effect.Internal);
                AL.Source(Src, ALSourceb.Looping, Loop);
                Engine.CheckError("Preconfig:" + Effect.Name);
                if (Pitch != 1f)
                {
                    UpdatePitch();
                }
                if (Gain != 1f)
                {
                    UpdateGain();
                }
                Engine.CheckError("GP:" + Effect.Name);
                if (!Position.IsNaN())
                {
                    Vector3 zero = Vector3.Zero;
                    Vector3 vec = ClientUtilities.Convert(Position);
                    AL.Source(Src, ALSource3f.Direction, ref zero);
                    AL.Source(Src, ALSource3f.Velocity, ref zero);
                    AL.Source(Src, ALSource3f.Position, ref vec);
                    AL.Source(Src, ALSourceb.SourceRelative, false);
                    AL.Source(Src, ALSourcef.EfxAirAbsorptionFactor, 1f);
                    Engine.CheckError("Positioning:" + Effect.Name);
                }
                else
                {
                    AL.Source(Src, ALSourceb.SourceRelative, true);
                    Engine.CheckError("Relative:" + Effect.Name);
                }
                Exists = true;
            }
        }

        public void UpdatePitch()
        {
            AL.Source(Src, ALSourcef.Pitch, Pitch);
        }

        public bool IsDeafened = false;

        public void UpdateGain()
        {
            bool sel = Engine.Selected;
            if (sel)
            {
                AL.Source(Src, ALSourcef.Gain, Gain);
                Backgrounded = false;
            }
            else
            {
                AL.Source(Src, ALSourcef.Gain, 0.0001f);
                Backgrounded = true;
            }
        }

        public void Play()
        {
            if (Src < 0)
            {
                return;
            }
            AL.SourcePlay(Src);
        }

        public void Seek(float f)
        {
            if (Src < 0)
            {
                return;
            }
            AL.Source(Src, ALSourcef.SecOffset, f);
        }

        public void Pause()
        {
            if (Src < 0)
            {
                return;
            }
            AL.SourcePause(Src);
        }

        public void Stop()
        {
            if (Src < 0)
            {
                return;
            }
            AL.SourceStop(Src);
        }

        public bool IsPlaying()
        {
            if (Src < 0)
            {
                return false;
            }
            return (AL.GetSourceState(Src) == ALSourceState.Playing);
        }

        public void Destroy()
        {
            if (Src < 0)
            {
                return;
            }
            if (Exists)
            {
                AL.DeleteSource(Src);
                Exists = false;
            }
        }
    }
}
