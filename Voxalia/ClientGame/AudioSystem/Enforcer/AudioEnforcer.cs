//
// This file is part of the game Voxalia, created by Frenetic LLC.
// This code is Copyright (C) 2016-2017 Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;
using Voxalia.Shared;
using FreneticGameCore;

namespace Voxalia.ClientGame.AudioSystem.Enforcer
{
    public class AudioEnforcer
    {
        public static long AudioID = 1;

        public Thread AudioForcer;

        public const int PAUSE = 10;

        public const int SAMPLE_LOAD = 33;

        public const int FREQUENCY = 44100;

        public const int CHANNELS = 2;

        public const int BYTERATE = 2;

        public const int BUFFERS_AT_ONCE = 2;

        public const int ACTUAL_SAMPLES = (int)((FREQUENCY * SAMPLE_LOAD) / 1000.0) * CHANNELS * BYTERATE;

        public const float MINUS_THREE_DB = 0.707106781f;

        public bool Run = false;

        public float Volume = 0.5f;

        public List<LiveAudioInstance> Playing = new List<LiveAudioInstance>();

        public Object Locker = new Object();

        public AudioContext Context;

        public Location Position;

        public Location ForwardDirection;

        public Location UpDirection;

        public bool Left = true;

        public bool Right = true;

        public void Add(LiveAudioInstance inst)
        {
            lock (Locker)
            {
                if (inst.State == AudioState.PLAYING)
                {
                    return;
                }
                inst.State = AudioState.PLAYING;
                Playing.Add(inst);
            }
        }

        public void Init(AudioContext acontext)
        {
            Context = acontext;
            Run = true;
            AudioForcer = new Thread(new ThreadStart(ForceAudioLoop))
            {
                Name = "Audio_" + Interlocked.Increment(ref AudioID)
            };
            AudioForcer.Start();
        }

        public void Shutdown()
        {
            Run = false;
        }

        const float QUARTER_PI = (float)Math.PI * 0.25f;

        public Object CLelLock = new Object();

        public float CurrentLevel = 0.0f;

        public void ForceAudioLoop()
        {
            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                Context.MakeCurrent();
                int src = AL.GenSource();
                AL.Source(src, ALSourceb.Looping, false);
                AL.Source(src, ALSourceb.SourceRelative, true);
                Queue<int> usable = new Queue<int>();
                List<LiveAudioInstance> dead = new List<LiveAudioInstance>();
                while (true)
                {
                    if (!Run)
                    {
                        Context.Dispose();
                        return;
                    }
                    sw.Stop();
                    double elSec = sw.ElapsedTicks / (double)Stopwatch.Frequency;
                    sw.Reset();
                    sw.Start();
                    AL.GetSource(src, ALGetSourcei.BuffersProcessed, out int proc);
                    while (proc > 0)
                    {
                        int buf = AL.SourceUnqueueBuffer(src);
                        usable.Enqueue(buf);
                        proc--;
                    }
                    AL.GetSource(src, ALGetSourcei.BuffersQueued, out int waiting);
                    BEPUutilities.Vector3 bforw = ForwardDirection.ToBVector();
                    BEPUutilities.Vector3 bup = UpDirection.ToBVector();
                    if (waiting < BUFFERS_AT_ONCE)
                    {
                        byte[] b = new byte[ACTUAL_SAMPLES];
                        lock (Locker)
                        {
                            foreach (LiveAudioInstance toAdd in Playing)
                            {
                                if (toAdd.State != AudioState.PLAYING)
                                {
                                    toAdd.CurrentSample = toAdd.Clip.Data.Length + 1;
                                }
                                if (toAdd.State != AudioState.PAUSED)
                                {
                                    int bpos = 0;
                                    int pos = 0;
                                    float tvol = 1f;
                                    float tvol_2 = 1f;
                                    if (toAdd.UsePosition)
                                    {
                                        float tempvol = 1.0f / Math.Max(1.0f, (float)toAdd.Position.DistanceSquared(Position));
                                        BEPUutilities.Vector3 rel = ((toAdd.Position - Position).Normalize().ToBVector());
                                        BEPUutilities.Quaternion.GetQuaternionBetweenNormalizedVectors(ref rel, ref bforw, out BEPUutilities.Quaternion quat);
                                        float angle = (float)quat.AxisAngleFor(bup);
                                        angle += (float)Math.PI; // TODO: Sanity!
                                        if (angle > (float)Math.PI)
                                        {
                                            angle -= (float)Math.PI * 2f;
                                        }
                                        else if (angle < -(float)Math.PI)
                                        {
                                            angle += (float)Math.PI * 2f;
                                        }

                                        bool any_left = true;
                                        bool any_right = true;

                                        if (angle > QUARTER_PI)
                                        {
                                            if (angle > QUARTER_PI * 3f)
                                            {
                                                angle = (float)Math.PI - angle;
                                            }
                                            else
                                            {
                                                any_right = false;
                                            }
                                        }
                                        else if (angle < -QUARTER_PI)
                                        {
                                            if (angle < -QUARTER_PI * 3f)
                                            {
                                                angle = (-(float)Math.PI) - angle;
                                            }
                                            else
                                            {
                                                any_left = false;
                                            }
                                        }
                                        
                                        tvol = Right && any_right ? (float)Math.Cos(angle + QUARTER_PI) : 0f;
                                        tvol_2 = Left && any_left ? (float)Math.Sin(angle + QUARTER_PI) : 0f;

                                        tvol *= tvol * tempvol;
                                        tvol_2 *= tvol_2 * tempvol;
                                    }
                                    float gain = toAdd.Gain * Volume;
                                    gain *= gain;
                                    int mod = (int)((tvol * gain) * ushort.MaxValue);
                                    int mod_2 = (int)((tvol_2 * gain) * ushort.MaxValue);
                                    int lim = Math.Min(toAdd.Clip.Data.Length - toAdd.CurrentSample, ACTUAL_SAMPLES);
                                    while (bpos < lim && bpos + 3 < ACTUAL_SAMPLES)
                                    {
                                        // TODO: pitch, velocity, etc.?
                                        int sample = (short)((toAdd.Clip.Data[pos + toAdd.CurrentSample + 1] << 8) | toAdd.Clip.Data[pos + toAdd.CurrentSample]);
                                        int bproc = (sample * mod_2) >> 16;
                                        int bsample = (short)((b[bpos + 1] << 8) | b[bpos]);
                                        bproc += bsample; // TODO: Better scaled adder
                                        bproc = Math.Max(short.MinValue, Math.Min(short.MaxValue, bproc));
                                        b[bpos] = (byte)bproc;
                                        b[bpos + 1] = (byte)(bproc >> 8);
                                        bpos += 2;
                                        if (toAdd.Clip.Channels == 2)
                                        {
                                            pos += 2;
                                            sample = (short)((toAdd.Clip.Data[pos + toAdd.CurrentSample + 1] << 8) | toAdd.Clip.Data[pos + toAdd.CurrentSample]);
                                            bproc = (sample * mod) >> 16;
                                            bsample = (short)((b[bpos + 1] << 8) | b[bpos]);
                                            bproc += bsample; // TODO: Better scaled adder
                                            b[bpos] = (byte)bproc;
                                            b[bpos + 1] = (byte)(bproc >> 8);
                                        }
                                        else
                                        {
                                            bproc = (sample * mod) >> 16;
                                            bsample = (short)((b[bpos + 1] << 8) | b[bpos]);
                                            bproc += bsample; // TODO: Better scaled adder
                                            b[bpos] = (byte)bproc;
                                            b[bpos + 1] = (byte)(bproc >> 8);
                                        }
                                        pos += 2;
                                        bpos += 2;
                                    }
                                    toAdd.CurrentSample += pos;
                                    if (toAdd.CurrentSample >= toAdd.Clip.Data.Length)
                                    {
                                        toAdd.CurrentSample = 0;
                                        if (toAdd.Loop)
                                        {
                                            // TODO: Append first few samples from the Data array.
                                        }
                                        else
                                        {
                                            toAdd.State = AudioState.DONE;
                                            dead.Add(toAdd);
                                        }
                                    }
                                }
                            }
                            foreach (LiveAudioInstance inst in dead)
                            {
                                Playing.Remove(inst);
                            }
                        }
                        float clevelval = 0.0f;
                        for (int i = 0; i < b.Length; i += BYTERATE)
                        {
                            int val = b[i] | (b[i + 1] << 8);
                            float tval = val / (float)ushort.MaxValue;
                            clevelval += tval;
                        }
                        clevelval /= (b.Length / 2) * Volume;
                        lock (CLelLock)
                        {
                            CurrentLevel = clevelval;
                        }
                        int buf = usable.Count > 0 ? usable.Dequeue() : AL.GenBuffer();
                        AL.BufferData(buf, ALFormat.Stereo16, b, ACTUAL_SAMPLES, FREQUENCY);
                        AL.SourceQueueBuffer(src, buf);
                        if (AL.GetSourceState(src) != ALSourceState.Playing)
                        {
                            AL.SourcePlay(src);
                        }
                    }
                    int ms = PAUSE - (int)(elSec * 1000.0);
                    if (ms > 0)
                    {
                        Thread.Sleep(ms);
                    }
                }
            }
            catch (Exception ex)
            {
                SysConsole.Output("Handling audio enforcer", ex);
            }
        }

    }
}
