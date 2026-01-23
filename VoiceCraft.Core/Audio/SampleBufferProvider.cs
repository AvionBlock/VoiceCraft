using System;
using System.Collections.Concurrent;

namespace VoiceCraft.Core.Audio
{
    public class SampleBufferProvider<T>(int size)
    {
        private readonly ConcurrentQueue<T> _buffer = new();
        private bool _isReading;
        public int MaxLength { get; } = size;
        public int PrefillSize { get; set; }
        public int Count => _buffer.Count; //Number of elements stored.

        public int Write(Span<T> buffer)
        {
            var write = 0;
            while (Count <= MaxLength && write < buffer.Length)
            {
                _buffer.Enqueue(buffer[write]);
                write++;
            }

            return write;
        }

        public int Read(Span<T> buffer)
        {
            if (Count < PrefillSize && !_isReading) return 0;
            _isReading = true;
            var read = 0;
            while (_buffer.TryDequeue(out var element) && read < buffer.Length)
            {
                buffer[read] = element;
                read++;
            }
            if(read <= 0)
                _isReading = false;
            
            return read;
        }

        public void Reset()
        {
            _buffer.Clear();
        }
    }
}