using NAudio.Wave;
using System.Diagnostics;

namespace VoiceCraft.Core.Audio.Streams;

/// <summary>
/// Audio stream that provides decoded audio from a jitter buffer.
/// Handles buffering and pacing of audio frames for smooth playback.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", Justification = "Kept for compatibility, though it implements IWaveProvider.")]
public class VoiceCraftStream : IWaveProvider, IDisposable
{
    /// <summary>
    /// Gets or sets the wave format for audio output.
    /// </summary>
    public WaveFormat WaveFormat { get; set; }

    private PreloadedBufferedWaveProvider DecodedAudio { get; set; }
    private VoiceCraftJitterBuffer JitterBuffer { get; set; }
    private Task DecodeThread { get; set; }
    private CancellationTokenSource TokenSource { get; set; }
    private CancellationToken Token { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="VoiceCraftStream"/> class.
    /// </summary>
    /// <param name="waveFormat">The audio format.</param>
    /// <param name="jitterBuffer">The jitter buffer to read from.</param>
    public VoiceCraftStream(WaveFormat waveFormat, VoiceCraftJitterBuffer jitterBuffer)
    {
        ArgumentNullException.ThrowIfNull(waveFormat);
        WaveFormat = waveFormat;
        DecodedAudio = new PreloadedBufferedWaveProvider(waveFormat)
        {
            ReadFully = true,
            BufferDuration = TimeSpan.FromSeconds(2),
            DiscardOnBufferOverflow = true,
            PreloadedBytesTarget = waveFormat.ConvertLatencyToByteSize(60)
        };
        JitterBuffer = jitterBuffer;
        TokenSource = new CancellationTokenSource();
        Token = TokenSource.Token;

        DecodeThread = Run();
    }

    /// <inheritdoc/>
    public int Read(byte[] buffer, int offset, int count)
    {
        return DecodedAudio.Read(buffer, offset, count);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Background loop resilience")]
    private Task Run()
    {
        return Task.Run(async () =>
        {
            var buffer = new byte[WaveFormat.ConvertLatencyToByteSize(JitterBuffer.FrameSizeMS)];
            long nextTick = Environment.TickCount64;
            int frameMs = JitterBuffer.FrameSizeMS;

            while (!Token.IsCancellationRequested)
            {
                try
                {
                    long now = Environment.TickCount64;
                    long wait = nextTick - now;

                    if (wait > 0)
                    {
                        await Task.Delay((int)wait, Token).ConfigureAwait(false);
                    }

                    nextTick += frameMs;

                    // Reset pace if we fell extremely behind (e.g. system sleep)
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

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            JitterBuffer?.Dispose();
            if (TokenSource != null && !TokenSource.IsCancellationRequested)
            {
                TokenSource.Cancel();
                try
                {
                    DecodeThread.Wait(1000);
                }
                catch (AggregateException) { }

                TokenSource.Dispose();
            }
        }
    }
}
