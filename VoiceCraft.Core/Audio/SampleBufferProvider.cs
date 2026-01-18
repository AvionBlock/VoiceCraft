using System;
using System.Collections.Concurrent;

namespace VoiceCraft.Core.Audio
{
    public class SampleBufferProvider<T>
    {
        private readonly ConcurrentQueue<T> _buffer;
        public int MaxLength { get; }
        public int PrefillSize { get; set; }
        public int Count => _buffer.Count; //Number of elements stored.
        
        public SampleBufferProvider(int size)
        {
            MaxLength = size;
            _buffer = new ConcurrentQueue<T>();
        }

        public int Write(Span<T> buffer)
        {
            var write = 0;
            while (Count <= MaxLength)
            {
                _buffer.Enqueue(buffer[write]);
                write++;
            }

            return write;
        }

        public int Read(Span<T> buffer)
        {
            if (Count < PrefillSize) return 0;
            var read = 0;
            while (_buffer.TryDequeue(out var element))
            {
                buffer[read] = element;
                read++;
            }
            
            return read;
        }

        public void Reset()
        {
            _buffer.Clear();
        }
    }
}