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

        public const int ACTUAL_SAMPLES = (int)((FREQUENCY * CHANNELS * BYTERATE) * (1000.0 / SAMPLE_LOAD));

        public bool Run = false;

        public List<LiveAudioInstance> Playing = new List<LiveAudioInstance>();

        public Object Locker = new Object();

        public AudioContext Context;

        public void Add(LiveAudioInstance inst)
        {
            lock (Locker)
            {
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
            Stopwatch sw = new Stopwatch();
            sw.Start();
            Context.MakeCurrent();
            int src = AL.GenSource();
            AL.Source(src, ALSourceb.Looping, false);
            AL.Source(src, ALSourceb.SourceRelative, true);
            Queue<int> buffers = new Queue<int>(50);
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
                    int buf = buffers.Dequeue();
                    usable.Enqueue(buf);
                    proc--;
                }
                if (buffers.Count < BUFFERS_AT_ONCE)
                {
                    byte[] b = new byte[ACTUAL_SAMPLES];
                    lock (Locker)
                    {
                        foreach (LiveAudioInstance toAdd in Playing)
                        {
                            int bpos = 0;
                            int pos = 0;
                            int lim = Math.Min(toAdd.Clip.Data.Length, toAdd.CurrentSample + ACTUAL_SAMPLES) - toAdd.CurrentSample;
                            while (bpos < lim)
                            {
                                // TODO: Volume, pitch, etc.?
                                b[bpos] = toAdd.Clip.Data[pos + toAdd.CurrentSample];
                                b[bpos + 1] = toAdd.Clip.Data[pos + toAdd.CurrentSample + 1];
                                bpos += 2;
                                if (toAdd.Clip.Channels == 2)
                                {
                                    pos += 2;
                                    b[bpos] = toAdd.Clip.Data[pos + toAdd.CurrentSample];
                                    b[bpos + 1] = toAdd.Clip.Data[pos + toAdd.CurrentSample + 1];
                                }
                                else
                                {
                                    b[bpos] = toAdd.Clip.Data[pos + toAdd.CurrentSample];
                                    b[bpos + 1] = toAdd.Clip.Data[pos + toAdd.CurrentSample + 1];
                                }
                                pos += 2;
                                bpos += 2;
                            }
                            toAdd.CurrentSample += pos;
                            if (toAdd.CurrentSample > toAdd.Clip.Data.Length)
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
                sw.Reset();
                sw.Start();
                int ms = PAUSE - (int)(elSec * 1000.0);
                if (ms > 0)
                {
                    Thread.Sleep(ms);
                }
            }
        }

    }
}
