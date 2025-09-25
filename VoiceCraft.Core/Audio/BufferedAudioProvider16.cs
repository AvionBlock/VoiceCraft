using System;

namespace VoiceCraft.Core.Audio
{
    public class BufferedAudioProvider16
    {
        private readonly CircularBuffer<short> _circularBuffer;

        public BufferedAudioProvider16(int size)
        {
            _circularBuffer = new CircularBuffer<short>(size);
        }

        public int BufferSize => _circularBuffer.MaxLength;

        public int BufferedCount => _circularBuffer.Count;

        public bool DiscardOnOverflow { get; set; }

        public bool ReadFully { get; set; }

        public int Write(short[] buffer, int count)
        {
            return Write(buffer.AsSpan(), count);
        }

        public int Write(Span<short> buffer, int count)
        {
            var written = _circularBuffer.Write(buffer, 0, count);
            if (written < count && !DiscardOnOverflow)
                throw new InvalidOperationException("Buffer full!");
            return written;
        }

        public int Read(short[] buffer, int count)
        {
            return Read(buffer.AsSpan(), count);
        }

        public int Read(Span<short> buffer, int count)
        {
            var read = _circularBuffer.Read(buffer, 0, count);
            if (read >= count || !ReadFully) return read;
            buffer.Slice(read, count - read).Clear(); //Zero the end of the buffer.
            return count;
        }

        public void Clear()
        {
            _circularBuffer.Reset();
        }
    }
}