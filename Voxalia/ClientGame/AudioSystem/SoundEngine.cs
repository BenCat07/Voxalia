//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for contents of the license.
// If neither of these are not available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;
using Voxalia.Shared;
using OggDecoder;
using Voxalia.ClientGame.CommandSystem;
using Voxalia.Shared.Files;
using Voxalia.ClientGame.OtherSystems;
using Voxalia.ClientGame.ClientMainSystem;
using FreneticScript;
using System.Threading;

namespace Voxalia.ClientGame.AudioSystem
{
    public class SoundEngine
    {
        public SoundEffect Noise;

        public AudioContext Context;

        public MicrophoneHandler Microphone = null;

        public Client TheClient;

        public ClientCVar CVars;
        
        public void Init(Client tclient, ClientCVar cvar)
        {
            if (Context != null)
            {
                Context.Dispose();
            }
            TheClient = tclient;
            CVars = cvar;
            Context = new AudioContext(AudioContext.DefaultDevice, 0, 0, false, true);
            Context.MakeCurrent();
            try
            {
                if (Microphone != null)
                {
                    Microphone.StopEcho();
                }
                Microphone = new MicrophoneHandler(this);
            }
            catch (Exception ex)
            {
                SysConsole.Output("Loading microphone handling", ex);
            }
            if (Effects != null)
            {
                foreach (SoundEffect sfx in Effects.Values)
                {
                    sfx.Internal = -2;
                }
            }
            Effects = new Dictionary<string, SoundEffect>();
            PlayingNow = new List<ActiveSound>();
            Noise = LoadSound(new DataStream(Convert.FromBase64String(NoiseDefault.NoiseB64)), "noise");
            DeafStart = GetSound("sfx/ringing/earring_start");
            DeafLoop = GetSound("sfx/ringing/earring_loop");
            DeafEnd = GetSound("sfx/ringing/earring_stop");
        }

        public void StopAll()
        {
            for (int i = 0; i < PlayingNow.Count; i++)
            {
                PlayingNow[i].Stop();
            }
            PlayingNow.Clear();
        }

        public void Shutdown()
        {
            StopAll();
            Context.Dispose();
            Context = null;
        }

        public bool Selected;

        SoundEffect DeafStart;
        SoundEffect DeafLoop;
        SoundEffect DeafEnd;

        public ActiveSound DeafNoise = null;

        public void CheckError(string inp)
        {
#if !AUDIO_ERROR_CHECK
            ALError err = AL.GetError();
            if (err != ALError.NoError)
            {
                SysConsole.Output(OutputType.WARNING, "Found audio error " + err + " for " + inp);
                //init(TheClient, CVars);
                return;
            }
#endif
        }

        public Location CPosition = Location.Zero;

        public void Update(Location position, Location forward, Location up, Location velocity, bool selected)
        {
            CPosition = position;
            ALError err = AL.GetError();
            if (err != ALError.NoError)
            {
                SysConsole.Output(OutputType.WARNING, "Found audio error " + err + "!");
                //init(TheClient, CVars);
                return;
            }
            bool sel = CVars.a_quietondeselect.ValueB ? selected : true;
            Selected = sel;
            if (Deafness == DeafenState.NOT)
            {
                if (DeafNoise != null)
                {
                    DeafNoise.Stop();
                    DeafNoise.Destroy();
                    DeafNoise = null;
                }
                if (DeafenTime > 0)
                {
                    Deafness = DeafenState.START;
                }
            }
            else
            {
                TimeDeaf += TheClient.Delta;
                DeafenTime -= TheClient.Delta;
            }
            if (Deafness == DeafenState.START)
            {
                if (DeafNoise == null)
                {
                    DeafNoise = PlaySimpleInternal(DeafStart, false);
                }
                // TODO: Play 'loop' before 'start' is finished, to avoid a spike of silence.
                if (!DeafNoise.IsPlaying())
                {
                    Deafness = DeafenState.LOOP;
                    DeafNoise = PlaySimpleInternal(DeafLoop, true);
                }
            }
            else if (Deafness == DeafenState.LOOP)
            {
                if (DeafenTime < 1.0)
                {
                    DeafNoise.Stop();
                    DeafNoise.Destroy();
                    DeafNoise = PlaySimpleInternal(DeafEnd, false);
                    Deafness = DeafenState.STOP;
                }
            }
            else if (Deafness == DeafenState.STOP)
            {
                if (!DeafNoise.IsPlaying())
                {
                    DeafNoise.Destroy();
                    DeafNoise = null;
                    Deafness = DeafenState.NOT;
                }
            }
            for (int i = 0; i < PlayingNow.Count; i++)
            {
                if (!PlayingNow[i].Exists || PlayingNow[i].Src < 0 || AL.GetSourceState(PlayingNow[i].Src) == ALSourceState.Stopped)
                {
                    PlayingNow[i].Destroy();
                    CheckError("Destroy:" + PlayingNow[i].Effect.Name);
                    PlayingNow.RemoveAt(i);
                    i--;
                    continue;
                }
                if (Deafness != DeafenState.NOT && sel && !PlayingNow[i].IsBackground)
                {
                    PlayingNow[i].IsDeafened = true;
                    float lesser = (float)Math.Min(DeafenTime, TimeDeaf);
                    if (lesser < 0.999)
                    {
                        AL.Source(PlayingNow[i].Src, ALSourcef.Gain, PlayingNow[i].Gain * (1.0f - lesser));
                    }
                    else
                    {
                        AL.Source(PlayingNow[i].Src, ALSourcef.Gain, 0.0001f);
                    }
                }
                else if (Deafness == DeafenState.NOT && sel && !PlayingNow[i].IsBackground)
                {
                    AL.Source(PlayingNow[i].Src, ALSourcef.Gain, PlayingNow[i].Gain);
                    PlayingNow[i].IsDeafened = false;
                }
                if (Deafness == DeafenState.NOT && !sel && PlayingNow[i].IsBackground && !PlayingNow[i].Backgrounded)
                {
                    AL.Source(PlayingNow[i].Src, ALSourcef.Gain, 0.0001f);
                    PlayingNow[i].Backgrounded = true;
                }
                else if (Deafness == DeafenState.NOT && sel && PlayingNow[i].Backgrounded)
                {
                    AL.Source(PlayingNow[i].Src, ALSourcef.Gain, PlayingNow[i].Gain);
                    PlayingNow[i].Backgrounded = false;
                    PlayingNow[i].IsDeafened = false;
                }
            }
            CheckError("Setup");
            if (Microphone != null)
            {
                Microphone.Tick();
            }
            CheckError("Microphone");
            Vector3 pos = ClientUtilities.Convert(position);
            Vector3 forw = ClientUtilities.Convert(forward);
            Vector3 upvec = ClientUtilities.Convert(up);
            Vector3 vel = ClientUtilities.Convert(velocity);
            AL.Listener(ALListener3f.Position, ref pos);
            AL.Listener(ALListenerfv.Orientation, ref forw, ref upvec);
            AL.Listener(ALListener3f.Velocity, ref vel);
            CheckError("Positioning");
            float globvol = CVars.a_globalvolume.ValueF;
            AL.Listener(ALListenerf.Gain, globvol <= 0 ? 0.001f: (globvol > 1 ? 1: globvol));
            CheckError("Gain");
        }

        public Dictionary<string, SoundEffect> Effects;

        public List<ActiveSound> PlayingNow;

        public bool CanClean()
        {
            bool cleaned = false;
            for (int i = 0; i < PlayingNow.Count; i++)
            {
                ActiveSound sound = PlayingNow[i];
                if (sound.Gain < 0.05 || (sound.Position.DistanceSquared(CPosition) > 30 * 30))
                {
                    sound.Destroy();
                    PlayingNow.RemoveAt(i);
                    i--;
                    cleaned = true;
                }
            }
            return cleaned;
        }

        /// <summary>
        /// NOTE: *NOT* guaranteed to play a sound effect immediately, regardless of input! Some sound effects will be delayed! If too many audible sounds are already playing, this will refuse to play.
        /// </summary>
        public void Play(SoundEffect sfx, bool loop, Location pos, float pitch = 1, float volume = 1, float seek_seconds = 0, Action<ActiveSound> callback = null)
        {
            if (PlayingNow.Count > 200)
            {
                if (!CanClean())
                {
                    return;
                }
            }
            if (sfx.Internal == -2)
            {
                Play(GetSound(sfx.Name), loop, pos, pitch, volume, seek_seconds, callback);
                return;
            }
            if (pitch <= 0 || pitch > 2)
            {
                throw new ArgumentException("Must be between 0 and 2", "pitch");
            }
            if (volume == 0)
            {
                return;
            }
            if (volume <= 0 || volume > 1)
            {
                throw new ArgumentException("Must be between 0 and 1", "volume");
            }
            Action playSound = () =>
            {
                ActiveSound actsfx = new ActiveSound(sfx);
                actsfx.Engine = this;
                actsfx.Position = pos;
                actsfx.Pitch = pitch * CVars.a_globalpitch.ValueF;
                actsfx.Gain = volume;
                actsfx.Loop = loop;
                actsfx.Create();
                if (actsfx.Src < 0)
                {
                    return;
                }
                CheckError("Create:" + sfx.Name);
                if (Deafness != DeafenState.NOT)
                {
                    actsfx.IsDeafened = true;
                    AL.Source(actsfx.Src, ALSourcef.Gain, 0.0001f);
                }
                if (seek_seconds != 0)
                {
                    actsfx.Seek(seek_seconds);
                }
                CheckError("Preconfig:" + sfx.Name);
                actsfx.Play();
                CheckError("Play:" + sfx.Name);
                PlayingNow.Add(actsfx);
                if (callback != null)
                {
                    callback(actsfx);
                }
            };
            lock (sfx)
            {
                if (sfx.Internal == -1)
                {
                    sfx.Loaded += (o, e) =>
                    {
                        playSound();
                    };
                    return;
                }
            }
            playSound();
        }

        public ActiveSound PlaySimpleInternal(SoundEffect sfx, bool loop)
        {
            Func<ActiveSound> playSound = () =>
            {
                ActiveSound actsfx = new ActiveSound(sfx);
                actsfx.Engine = this;
                actsfx.Position = Location.NaN;
                actsfx.Pitch = 1.0f;
                actsfx.Gain = 1.0f;
                actsfx.Loop = loop;
                actsfx.Create();
                actsfx.Play();
                return actsfx;
            };
            lock (sfx)
            {
                if (sfx.Internal == -1)
                {
                    return null; // TODO: Enforce load-NOW?
                }
            }
            return playSound();
        }

        public SoundEffect GetSound(string name)
        {
            string namelow = name.ToLowerFast();
            SoundEffect sfx;
            if (Effects.TryGetValue(namelow, out sfx))
            {
                return sfx;
            }
            sfx = LoadSound(namelow);
            if (sfx != null)
            {
                return sfx;
            }
            sfx = new SoundEffect();
            sfx.Name = namelow;
            sfx.Internal = Noise.Internal;
            return sfx;
        }

        ALFormat GetSoundFormat(int channels, int bits)
        {
            switch (channels)
            {
                case 1:
                    return bits == 8 ? ALFormat.Mono8 : ALFormat.Mono16;
                default: // 2
                    return bits == 8 ? ALFormat.Stereo8 : ALFormat.Stereo16;
            }
        }

        public SoundEffect LoadSound(string name)
        {
            try
            {
                string newname = "sounds/" + name + ".ogg";
                if (!TheClient.Files.Exists(newname))
                {
                    return null;
                }
                SoundEffect tsfx = new SoundEffect();
                tsfx.Name = name;
                tsfx.Internal = -1;
                TheClient.Schedule.StartAsyncTask(() =>
                {
                    SoundEffect ts = LoadVorbisSound(TheClient.Files.ReadToStream(newname), name);
                    lock (tsfx)
                    {
                        tsfx.Internal = ts.Internal;
                    }
                    if (tsfx.Loaded != null)
                    {
                        TheClient.Schedule.ScheduleSyncTask(() =>
                        {
                            if (tsfx.Loaded != null)
                            {
                                tsfx.Loaded.Invoke(tsfx, null);
                            }
                        });
                    }
                });
                return tsfx;
            }
            catch (Exception ex)
            {
                SysConsole.Output(OutputType.ERROR, "Reading sound file '" + name + "': " + ex.ToString());
                return null;
            }
        }

        public void Deafen(double time)
        {
            DeafenTime = time;
        }

        public enum DeafenState
        {
            NOT = 0,
            START = 1,
            LOOP = 2,
            STOP = 3
        }

        public DeafenState Deafness;

        public double DeafenTime = 0.0;

        public double TimeDeaf = 0.0;

        public SoundEffect LoadVorbisSound(DataStream stream, string name)
        {
            OggDecodeStream oggds = new OggDecodeStream(stream);
            return LoadSound(new DataStream(oggds.decodedStream.ToArray()), name);
        }

        public SoundEffect LoadSound(DataStream stream, string name)
        {
            SoundEffect sfx = new SoundEffect();
            sfx.Name = name;
            int channels;
            int bits;
            int rate;
            byte[] data = LoadWAVE(stream, out channels, out bits, out rate);
            sfx.Internal = AL.GenBuffer();
            AL.BufferData(sfx.Internal, GetSoundFormat(channels, bits), data, data.Length, rate);
            return sfx;
        }

        public byte[] LoadWAVE(DataStream stream, out int channels, out int bits, out int rate)
        {
            DataReader dr = new DataReader(stream);
            string signature = new string(dr.ReadChars(4));
            if (signature != "RIFF")
            {
                throw new NotSupportedException("Not a RIFF .wav file: " + signature);
            }
            /*int riff_chunk_size = */dr.ReadInt32();
            string format = new string(dr.ReadChars(4));
            if (format != "WAVE")
            {
                throw new NotSupportedException("Not a WAVE .wav file: " + format);
            }
            string format_signature = new string(dr.ReadChars(4));
            if (format_signature != "fmt ")
            {
                throw new NotSupportedException("Not a 'fmt ' .wav file: " + format_signature);
            }
            /*int format_chunk_size = */dr.ReadInt32();
            /*int audio_format = */dr.ReadInt16();
            int num_channels = dr.ReadInt16();
            if (num_channels != 1 && num_channels != 2)
            {
                throw new NotSupportedException("Invalid number of channels: " + num_channels);
            }
            int sample_rate = dr.ReadInt32();
            /*int byte_rate = */dr.ReadInt32();
            /*int block_align = */dr.ReadInt16();
            int bits_per_sample = dr.ReadInt16();
            string data_signature = new string(dr.ReadChars(4));
            if (data_signature != "data")
            {
                throw new NotSupportedException("Not a DATA .wav file: " + data_signature);
            }
            int data_chunk_size = dr.ReadInt32();
            channels = num_channels;
            bits = bits_per_sample;
            rate = sample_rate;
            return dr.ReadBytes(data_chunk_size);
        }
    }
}
