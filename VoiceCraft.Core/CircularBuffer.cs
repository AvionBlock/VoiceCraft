using System;

//Credits https://github.com/joaoportela/CircularBuffer-CSharp/blob/master/CircularBuffer/CircularBuffer.cs
namespace VoiceCraft.Core
{
    /// <summary>
    /// A very basic circular buffer implementation
    /// </summary>
    public class CircularBuffer<T>
    {
        private readonly T[] _buffer;
        private int _writePosition;
        private int _readPosition;
        private int _count;
        private readonly object _lockObject;

        /// <summary>
        /// Create a new circular buffer
        /// </summary>
        /// <param name="size">Max buffer size in bytes</param>
        public CircularBuffer(int size)
        {
            _buffer = new T[size];
            _lockObject = new object();
        }

        /// <summary>
        /// Write data to the buffer
        /// </summary>
        /// <param name="data">Data to write</param>
        /// <param name="offset">Offset into data</param>
        /// <param name="count">Number of bytes to write</param>
        /// <returns>number of bytes written</returns>
        public int Write(Span<T> data, int offset, int count)
        {
            lock (_lockObject)
            {
                var elementsWritten = 0;
                if (count > _buffer.Length - _count)
                    count = _buffer.Length - _count;
                // write to end
                var writeToEnd = Math.Min(_buffer.Length - _writePosition, count);
                data.Slice(offset, writeToEnd).CopyTo(_buffer.AsSpan(_writePosition));
                _writePosition += writeToEnd;
                _writePosition %= _buffer.Length;
                elementsWritten += writeToEnd;
                if (elementsWritten < count)
                {
                    // must have wrapped round. Write to start
                    data.Slice(offset + elementsWritten, count - elementsWritten).CopyTo(_buffer.AsSpan(_writePosition));
                    _writePosition += count - elementsWritten;
                    elementsWritten = count;
                }
                _count += elementsWritten;
                return elementsWritten;
            }
        }

        /// <summary>
        /// Read from the buffer
        /// </summary>
        /// <param name="data">Buffer to read into</param>
        /// <param name="offset">Offset into read buffer</param>
        /// <param name="count">Bytes to read</param>
        /// <returns>Number of bytes actually read</returns>
        public int Read(Span<T> data, int offset, int count)
        {
            lock (_lockObject)
            {
                if (count > _count)
                {
                    count = _count;
                }
                var elementsRead = 0;
                var readToEnd = Math.Min(_buffer.Length - _readPosition, count);
                _buffer.AsSpan(_readPosition, readToEnd).CopyTo(data[offset..]);
                elementsRead += readToEnd;
                _readPosition += readToEnd;
                _readPosition %= _buffer.Length;

                if (elementsRead < count)
                {
                    // must have wrapped round. Read from start
                    _buffer.AsSpan(_readPosition, count - elementsRead).CopyTo(data[(offset + elementsRead)..]);
                    _readPosition += count - elementsRead;
                    elementsRead = count;
                }

                _count -= elementsRead;
                return elementsRead;
            }
        }

        /// <summary>
        /// Maximum length of this circular buffer
        /// </summary>
        public int MaxLength => _buffer.Length;

        /// <summary>
        /// Number of elements currently stored in the circular buffer
        /// </summary>
        public int Count => _count;

        /// <summary>
        /// Resets the buffer
        /// </summary>
        public void Reset()
        {
            _count = 0;
            _readPosition = 0;
            _writePosition = 0;
        }

        /// <summary>
        /// Advances the buffer, discarding bytes
        /// </summary>
        /// <param name="count">Bytes to advance</param>
        public void Advance(int count)
        {
            if (count >= _count)
            {
                Reset();
            }
            else
            {
                _count -= count;
                _readPosition += count;
                _readPosition %= MaxLength;
            }

        }
    }
}