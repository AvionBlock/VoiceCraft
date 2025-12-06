using NAudio.Utils;
using NAudio.Wave;

namespace VoiceCraft.Core.Audio.Streams;

/// <summary>
/// Buffered wave provider that requires preloading before audio output starts.
/// Thread-safe for concurrent read/write operations.
/// </summary>
public class PreloadedBufferedWaveProvider : IWaveProvider
{
    private CircularBuffer? circularBuffer;
    private readonly WaveFormat waveFormat;
    private bool IsReady;
    private readonly object _lock = new();

    /// <summary>
    /// Creates a new buffered WaveProvider
    /// </summary>
    public PreloadedBufferedWaveProvider(WaveFormat waveFormat)
    {
        ArgumentNullException.ThrowIfNull(waveFormat);
        this.waveFormat = waveFormat;
        BufferLength = waveFormat.AverageBytesPerSecond * 5;
        ReadFully = true;
    }

    /// <summary>
    /// If true, always read the amount of data requested, padding with zeroes if necessary.
    /// </summary>
    public bool ReadFully { get; set; }

    /// <summary>
    /// Number of bytes to target before releasing data.
    /// </summary>
    public int PreloadedBytesTarget { get; set; }

    /// <summary>
    /// Buffer length in bytes.
    /// </summary>
    public int BufferLength { get; set; }

    /// <summary>
    /// Buffer duration.
    /// </summary>
    public TimeSpan BufferDuration
    {
        get => TimeSpan.FromSeconds((double)BufferLength / WaveFormat.AverageBytesPerSecond);
        set => BufferLength = (int)(value.TotalSeconds * WaveFormat.AverageBytesPerSecond);
    }

    /// <summary>
    /// If true, when the buffer is full, start throwing away data.
    /// </summary>
    public bool DiscardOnBufferOverflow { get; set; }

    /// <summary>
    /// The number of buffered bytes.
    /// </summary>
    public int BufferedBytes
    {
        get
        {
            lock (_lock)
            {
                return circularBuffer?.Count ?? 0;
            }
        }
    }

    /// <summary>
    /// Buffered Duration.
    /// </summary>
    public TimeSpan BufferedDuration => TimeSpan.FromSeconds((double)BufferedBytes / WaveFormat.AverageBytesPerSecond);

    /// <summary>
    /// Gets the WaveFormat.
    /// </summary>
    public WaveFormat WaveFormat => waveFormat;

    /// <summary>
    /// Adds samples. Takes a copy of buffer, so that buffer can be reused if necessary.
    /// </summary>
    public void AddSamples(byte[] buffer, int offset, int count)
    {
        lock (_lock)
        {
            circularBuffer ??= new CircularBuffer(BufferLength);

            var written = circularBuffer.Write(buffer, offset, count);

            if (circularBuffer.Count >= PreloadedBytesTarget && !IsReady)
            {
                IsReady = true;
            }

            if (written < count && !DiscardOnBufferOverflow)
            {
                throw new InvalidOperationException("Buffer full");
            }
        }
    }

    /// <summary>
    /// Reads from this WaveProvider.
    /// </summary>
    public int Read(byte[] buffer, int offset, int count)
    {
        int read = 0;
        lock (_lock)
        {
            if (circularBuffer != null && IsReady)
            {
                read = circularBuffer.Read(buffer, offset, count);
            }
            if (ReadFully && read < count)
            {
                Array.Clear(buffer, offset + read, count - read);
                read = count;
                IsReady = false;
            }
        }
        return read;
    }

    /// <summary>
    /// Discards all audio from the buffer.
    /// </summary>
    public void ClearBuffer()
    {
        lock (_lock)
        {
            circularBuffer?.Reset();
        }
    }
}
