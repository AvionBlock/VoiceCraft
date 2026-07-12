using System;
using System.Collections.Generic;
using System.Threading;

namespace VoiceCraft.Core.Audio
{
    public class SampleBufferProvider<T>(int size)
    {
        private readonly Lock _lock = new();
        private readonly Queue<T> _buffer = new();
        private bool _isReading;
        public int MaxLength => size;
        public int PrefillSize { get; set; }

        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _buffer.Count;
                }
            }
        }

        public void Write(Span<T> buffer)
        {
            lock (_lock)
            {
                if (_buffer.Count + buffer.Length > size) return;
                foreach (var sample in buffer)
                {
                    _buffer.Enqueue(sample);
                }
            }
        }

        public int Read(Span<T> buffer)
        {
            if (!_isReading && Count < PrefillSize) return 0;
            _isReading = true;
            var read = 0;
            
            lock (_lock)
            {
                for (var i = 0; i < buffer.Length; i++)
                {
                    if (_buffer.TryDequeue(out var sample))
                    {
                        buffer[i] = sample;
                        read++;
                    }
                    else
                    {
                        _isReading = false;
                        break; // Queue is empty.
                    }
                }
            }
            return read;
        }

        public void Reset()
        {
            lock (_lock)
            {
                _buffer.Clear();
            }
        }
    }
}