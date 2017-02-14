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

        public bool Run = false;

        public float Volume = 0.5f;

        public List<LiveAudioInstance> Playing = new List<LiveAudioInstance>();

        public Object Locker = new Object();

        public AudioContext Context;

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
            AudioForcer = new Thread(new ThreadStart(ForceAudioLoop));
            AudioForcer.Name = "Audio_" + Interlocked.Increment(ref AudioID);
            AudioForcer.Start();
        }

        public void Shutdown()
        {
            Run = false;
        }

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
                        return;
                    }
                    sw.Stop();
                    double elSec = sw.ElapsedTicks / (double)Stopwatch.Frequency;
                    int proc;
                    AL.GetSource(src, ALGetSourcei.BuffersProcessed, out proc);
                    while (proc > 0)
                    {
                        int buf = AL.SourceUnqueueBuffer(src);
                        usable.Enqueue(buf);
                        proc--;
                    }
                    int waiting;
                    AL.GetSource(src, ALGetSourcei.BuffersQueued, out waiting);
                    long blast = 0;
                    long vol = 0;
                    long samps = 0;
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
                                    int mod = (int)(toAdd.Gain * Volume * ushort.MaxValue);
                                    vol += mod;
                                    int lim = Math.Min(toAdd.Clip.Data.Length - toAdd.CurrentSample, ACTUAL_SAMPLES);
                                    //SysConsole.Output(OutputType.DEBUG, "Sample / " + lim + ", " + toAdd.CurrentSample);
                                    while (bpos < lim && bpos + 3 < ACTUAL_SAMPLES)
                                    {
                                        // TODO: pitch, rate, position, velocity, direction, etc.?
                                        int sample = (short)((toAdd.Clip.Data[pos + toAdd.CurrentSample + 1] << 8) | toAdd.Clip.Data[pos + toAdd.CurrentSample]);
                                        int bproc = (sample * mod) >> 16;
                                        int bsample = (short)((b[bpos + 1] << 8) | b[bpos]);
                                        bproc += bsample; // TODO: Better scaled adder
                                        bproc = Math.Max(short.MinValue, Math.Min(short.MaxValue, bproc));
                                        b[bpos] = (byte)bproc;
                                        b[bpos + 1] = (byte)(bproc >> 8);
                                        blast += Math.Abs(b[bpos + 1]);
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
                                            b[bpos] = (byte)bproc;
                                            b[bpos + 1] = (byte)(bproc >> 8);
                                        }
                                        pos += 2;
                                        bpos += 2;
                                        samps += 4;
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
                        int buf = usable.Count > 0 ? usable.Dequeue() : AL.GenBuffer();
                        AL.BufferData(buf, ALFormat.Stereo16, b, ACTUAL_SAMPLES, FREQUENCY);
                        AL.SourceQueueBuffer(src, buf);
                        if (AL.GetSourceState(src) != ALSourceState.Playing)
                        {
                            AL.SourcePlay(src);
                        }
                    }
                    //SysConsole.Output(OutputType.DEBUG, "blasted: " + blast + " at " + vol + " across " + samps + "/" + ACTUAL_SAMPLES);
                    sw.Reset();
                    sw.Start();
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
