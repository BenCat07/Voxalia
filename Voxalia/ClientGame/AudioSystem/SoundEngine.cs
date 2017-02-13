//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for contents of the license.
// If neither of these are not available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Voxalia.ClientGame.AudioSystem.Enforcer;

namespace Voxalia.ClientGame.AudioSystem
{
    public class SoundEngine
    {
        public SoundEffect Noise;

        public AudioContext Context;

        public AudioEnforcer AudioInternal;

        public MicrophoneHandler Microphone = null;

        public Client TheClient;

        public ClientCVar CVars;
        
        public void Init(Client tclient, ClientCVar cvar)
        {
            if (AudioInternal != null)
            {
                AudioInternal.Shutdown();
            }
            if (Context != null)
            {
                Context.Dispose();
            }
            TheClient = tclient;
            CVars = cvar;
            Context = new AudioContext(AudioContext.DefaultDevice, 0, 0, false, true);
            if (TheClient.CVars.a_enforce.ValueB)
            {
                AudioInternal = new AudioEnforcer();
                AudioInternal.Init(Context);
                Context = null;
            }
            else
            {
                Context.MakeCurrent();
            }
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
            DeafLoop = GetSound("sfx/ringing/earring_loop");
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
            if (Context != null)
            {
                Context.Dispose();
            }
            Context = null;
        }

        public bool Selected;
        
        SoundEffect DeafLoop;

        public ActiveSound DeafNoise = null;

        public void CheckError(string inp)
        {
#if AUDIO_ERROR_CHECK
            if (AudioInternal == null)
            {
                ALError err = AL.GetError();
                if (err != ALError.NoError)
                {
                    SysConsole.Output(OutputType.WARNING, "Found audio error " + err + " for " + inp);
                    //init(TheClient, CVars);
                    return;
                }
            }
#endif
        }

        public Location CPosition = Location.Zero;

        public double TimeTowardsNextClean = 0.0;

        public void Update(Location position, Location forward, Location up, Location velocity, bool selected)
        {
            CPosition = position;
            if (AudioInternal == null)
            {
                ALError err = AL.GetError();
                if (err != ALError.NoError)
                {
                    SysConsole.Output(OutputType.WARNING, "Found audio error " + err + "!");
                    //init(TheClient, CVars);
                    return;
                }
            }
            bool sel = CVars.a_quietondeselect.ValueB ? selected : true;
            Selected = sel;
            if (DeafenTime > 0.0)
            {
                TimeDeaf += TheClient.Delta;
                DeafenTime -= TheClient.Delta;
                if (DeafNoise == null)
                {
                    DeafNoise = PlaySimpleInternal(DeafLoop, true);
                    if (DeafNoise == null)
                    {
                        DeafenTime = 0;
                        TimeDeaf = 0;
                    }
                }
                if (DeafenTime < 0)
                {
                    TimeDeaf = 0;
                    DeafenTime = 0;
                    DeafNoise.Stop();
                    DeafNoise.Destroy();
                    DeafNoise = null;
                }
            }
            if (TimeDeaf > 0.001 && DeafenTime > 0.001)
            {
                float weaken = (float)Math.Min(DeafenTime, TimeDeaf);
                if (weaken < 1.0)
                {
                    DeafNoise.Gain = (float)weaken * 0.5f;
                    DeafNoise.UpdateGain();
                }
                else
                {
                    DeafNoise.Gain = 0.5f;
                    DeafNoise.UpdateGain();
                }
            }
            DeafLoop.LastUse = TheClient.GlobalTickTimeLocal;
            for (int i = 0; i < PlayingNow.Count; i++)
            {
                if (!PlayingNow[i].Exists || PlayingNow[i].Src < 0 || (AudioInternal == null ? AL.GetSourceState(PlayingNow[i].Src) == ALSourceState.Stopped : PlayingNow[i].AudioInternal.State == AudioState.DONE))
                {
                    PlayingNow[i].Destroy();
                    if (AudioInternal == null)
                    {
                        CheckError("Destroy:" + PlayingNow[i].Effect.Name);
                    }
                    PlayingNow.RemoveAt(i);
                    i--;
                    continue;
                }
                PlayingNow[i].Effect.LastUse = TheClient.GlobalTickTimeLocal;
                if ((TimeDeaf > 0.0) && sel && !PlayingNow[i].IsBackground)
                {
                    PlayingNow[i].IsDeafened = true;
                    float lesser = (float)Math.Min(DeafenTime, TimeDeaf);
                    if (lesser < 0.999)
                    {
                        if (AudioInternal == null)
                        {
                            AL.Source(PlayingNow[i].Src, ALSourcef.Gain, PlayingNow[i].Gain * (1.0f - lesser));
                        }
                        else
                        {
                            PlayingNow[i].AudioInternal.Gain = PlayingNow[i].Gain * (1.0f - lesser);
                        }
                    }
                    else
                    {
                        if (AudioInternal == null)
                        {
                            AL.Source(PlayingNow[i].Src, ALSourcef.Gain, 0.0001f);
                        }
                        else
                        {
                            PlayingNow[i].AudioInternal.Gain = 0.0001f;
                        }
                    }
                }
                else if ((TimeDeaf <= 0.0) && sel && !PlayingNow[i].IsBackground)
                {
                    if (AudioInternal == null)
                    {
                        AL.Source(PlayingNow[i].Src, ALSourcef.Gain, PlayingNow[i].Gain);
                    }
                    else
                    {

                    }
                    PlayingNow[i].IsDeafened = false;
                }
                if ((TimeDeaf <= 0.0) && !sel && PlayingNow[i].IsBackground && !PlayingNow[i].Backgrounded)
                {
                    if (AudioInternal == null)
                    {
                        AL.Source(PlayingNow[i].Src, ALSourcef.Gain, 0.0001f);
                    }
                    else
                    {
                        PlayingNow[i].AudioInternal.Gain = 0.0001f;
                    }
                    PlayingNow[i].Backgrounded = true;
                }
                else if ((TimeDeaf <= 0.0) && sel && PlayingNow[i].Backgrounded)
                {
                    if (AudioInternal == null)
                    {
                        AL.Source(PlayingNow[i].Src, ALSourcef.Gain, PlayingNow[i].Gain);
                    }
                    else
                    {
                        PlayingNow[i].AudioInternal.Gain = PlayingNow[i].Gain;
                    }
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
            float globvol = CVars.a_globalvolume.ValueF;
            globvol = globvol <= 0 ? 0.001f : (globvol > 1 ? 1 : globvol);
            if (AudioInternal == null)
            {
                AL.Listener(ALListener3f.Position, ref pos);
                AL.Listener(ALListenerfv.Orientation, ref forw, ref upvec);
                AL.Listener(ALListener3f.Velocity, ref vel);
                CheckError("Positioning");
                AL.Listener(ALListenerf.Gain, globvol);
                CheckError("Gain");
            }
            else
            {
                // TODO: pos, vel, dir
                AudioInternal.Volume = globvol;
            }
            TimeTowardsNextClean += TheClient.Delta;
            if (TimeTowardsNextClean > 10.0)
            {
                CleanTick();
                TimeTowardsNextClean = 0.0;
            }
        }

        List<string> ToRemove = new List<string>();

        public void CleanTick()
        {
            foreach (KeyValuePair<string, SoundEffect> effect in Effects)
            {
                if (effect.Value.LastUse + 30.0 < TheClient.GlobalTickTimeLocal)
                {
                    if (effect.Value.Internal > -1)
                    {
                        AL.DeleteBuffer(effect.Value.Internal);
                    }
                    effect.Value.Internal = -2;
                    ToRemove.Add(effect.Key);
                }
            }
            foreach (string rem in ToRemove)
            {
                Effects.Remove(rem);
            }
            ToRemove.Clear();
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
            if (sfx == null)
            {
                SysConsole.Output(OutputType.DEBUG, "Audio / null");
                return;
            }
            if (PlayingNow.Count > 200)
            {
                if (!CanClean())
                {
                    SysConsole.Output(OutputType.DEBUG, "Audio / count");
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
                SysConsole.Output(OutputType.DEBUG, "Audio / volume");
                return;
            }
            if (volume <= 0 || volume > 1)
            {
                throw new ArgumentException("Must be between 0 and 1", "volume");
            }
            Action playSound = () =>
            {
                if (sfx.Clip == null && sfx.Internal < 0)
                {
                    SysConsole.Output(OutputType.DEBUG, "Audio / clip");
                    return;
                }
                ActiveSound actsfx = new ActiveSound(sfx);
                actsfx.Engine = this;
                actsfx.Position = pos;
                actsfx.Pitch = pitch * CVars.a_globalpitch.ValueF;
                actsfx.Gain = volume;
                actsfx.Loop = loop;
                actsfx.Create();
                if (actsfx.AudioInternal == null && actsfx.Src < 0)
                {
                    SysConsole.Output(OutputType.DEBUG, "Audio / src");
                    return;
                }
                CheckError("Create:" + sfx.Name);
                if (TimeDeaf > 0.0)
                {
                    actsfx.IsDeafened = true;
                    if (AudioInternal == null)
                    {
                        AL.Source(actsfx.Src, ALSourcef.Gain, 0.0001f);
                    }
                    else
                    {
                        actsfx.AudioInternal.Gain = 0.0001f;
                    }
                }
                if (seek_seconds != 0)
                {
                    actsfx.Seek(seek_seconds);
                }
                CheckError("Preconfig:" + sfx.Name);
                actsfx.Play();
                CheckError("Play:" + sfx.Name);
                SysConsole.Output(OutputType.DEBUG, "Audio / sucess");
                PlayingNow.Add(actsfx);
                if (callback != null)
                {
                    callback(actsfx);
                }
            };
            lock (sfx)
            {
                if (sfx.Clip == null && sfx.Internal == -1)
                {
                    SysConsole.Output(OutputType.DEBUG, "Audio / delay");
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
                Effects.Add(namelow, sfx);
                return sfx;
            }
            sfx = new SoundEffect();
            sfx.Name = namelow;
            sfx.Internal = -1;
            sfx.LastUse = TheClient.GlobalTickTimeLocal;
            Effects.Add(namelow, sfx);
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
                    SysConsole.Output(OutputType.DEBUG, "Audio / nullsource");
                    return null;
                }
                SoundEffect tsfx = new SoundEffect();
                tsfx.Name = name;
                tsfx.Internal = -1;
                tsfx.LastUse = TheClient.GlobalTickTimeLocal;
                TheClient.Schedule.StartAsyncTask(() =>
                {
                    try
                    {
                        SoundEffect ts = LoadVorbisSound(TheClient.Files.ReadToStream(newname), name);
                        lock (tsfx)
                        {
                            tsfx.Internal = ts.Internal;
                            tsfx.Clip = ts.Clip;
                        }
                        SysConsole.Output(OutputType.DEBUG, "Audio / valid1: " + tsfx.Internal + ", " + tsfx.Clip);
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
                    }
                    catch (Exception ex)
                    {
                        SysConsole.Output("loading audio", ex);
                    }
                });
                SysConsole.Output(OutputType.DEBUG, "Audio / valid: " + tsfx);
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
            if (DeafenTime == 0.0 &&  time < 2.0)
            {
                time = 2.0;
            }
            DeafenTime = time;
        }
        
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
            sfx.LastUse = TheClient.GlobalTickTimeLocal;
            int channels;
            int bits;
            int rate;
            byte[] data = LoadWAVE(stream, out channels, out bits, out rate);
            if (AudioInternal != null)
            {
                LiveAudioClip clip = new LiveAudioClip();
                clip.Data = data;
                if (bits == 1)
                {
                    clip.Data = new byte[data.Length * 2];
                    for (int i = 0; i < data.Length; i++)
                    {
                        // TODO: Sanity?
                        clip.Data[i] = data[i + 1];
                        clip.Data[i + 1] = (byte)0;
                    }
                    data = clip.Data;
                }
                // TODO: clip.Rate = rate;
                clip.Channels = (byte)channels;
                sfx.Clip = clip;
            }
            else
            {
                sfx.Internal = AL.GenBuffer();
                AL.BufferData(sfx.Internal, GetSoundFormat(channels, bits), data, data.Length, rate);
            }
            SysConsole.Output(OutputType.DEBUG, "Audio / prepped: " + AudioInternal);
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
