using NAudio.Wave;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace VoiceCraft.Core.Audio.Streams
{
    public class VoiceCraftStream : IWaveProvider, IDisposable
    {
        public WaveFormat WaveFormat { get; set; }
        private PreloadedBufferedWaveProvider DecodedAudio { get; set; }
        private VoiceCraftJitterBuffer JitterBuffer { get; set; }
        private Task DecodeThread { get; set; }
        private CancellationTokenSource TokenSource { get; set; }
        private CancellationToken Token { get; set; }

        public VoiceCraftStream(WaveFormat WaveFormat, VoiceCraftJitterBuffer JitterBuffer)
        {
            this.WaveFormat = WaveFormat;
            DecodedAudio = new PreloadedBufferedWaveProvider(WaveFormat) { ReadFully = true, BufferDuration = TimeSpan.FromSeconds(2), DiscardOnBufferOverflow = true, PreloadedBytesTarget = WaveFormat.ConvertLatencyToByteSize(60) };
            this.JitterBuffer = JitterBuffer;
            TokenSource = new CancellationTokenSource();
            Token = TokenSource.Token;

            DecodeThread = Run();
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            return DecodedAudio.Read(buffer, offset, count);
        }

        private Task Run()
        {
            return Task.Run(async() =>
            {
                // Allocated once, reused loop-to-loop
                var buffer = new byte[WaveFormat.ConvertLatencyToByteSize(JitterBuffer.FrameSizeMS)];
                long nextTick = Environment.TickCount64;
                int frameMs = JitterBuffer.FrameSizeMS;

                while (!Token.IsCancellationRequested)
                {
                    try
                    {
                        long now = Environment.TickCount64;
                        long wait = nextTick - now;
                        
                        // If we are ahead of time, wait.
                        if (wait > 0)
                        {
                            await Task.Delay((int)wait, Token).ConfigureAwait(false);
                        }
                        
                        // Advance expected time for next frame, maintaining average pace
                        nextTick += frameMs;

                        // If we fell extremely behind (e.g. system sleep), reset pace to prevent packet burst
                        if (now - nextTick > 1000) 
                        {
                            nextTick = now + frameMs;
                        }

                        var count = JitterBuffer.Get(buffer);
                        if (count > 0)
                        {
                            DecodedAudio.AddSamples(buffer, 0, count);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex);
                    }
                }
            }, Token);
        }

        public void Dispose()
        {
            if (!TokenSource.IsCancellationRequested)
            {
                TokenSource.Cancel();
                try 
                {
                    DecodeThread.Wait(1000); // Wait up to 1 second
                }
                catch (AggregateException) {}
                
                TokenSource.Dispose();
                // DecodeThread.Dispose() is not necessary and incorrect for Task
            }
        }
    }
}
