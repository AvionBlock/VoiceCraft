using System;

namespace VoiceCraft.Core.Audio.Filters
{
    public class NullAudioFilter : IAudioTwoWayFilter
    {
        readonly byte[] buffer = new byte[10000];
        int writePosition;
        int readPosition;
        readonly int outputBytesPerFrame;

        public NullAudioFilter(int outputBytesPerFrame)
        {
            this.outputBytesPerFrame = outputBytesPerFrame;
        }

        public bool Read(Array outBuffer, out bool moreFrames)
        {
            if (writePosition - readPosition >= outputBytesPerFrame)
            {
                // Return the next available frame.
                Buffer.BlockCopy(buffer, readPosition, outBuffer, 0, outputBytesPerFrame);
                readPosition += outputBytesPerFrame;
                moreFrames = (writePosition - readPosition >= outputBytesPerFrame);

                // If there are no more remaining bytes, move the pointer to the start of the buffer.
                if (writePosition == readPosition)
                {
                    writePosition = 0;
                    readPosition = 0;
                }
                return true;
            }
            moreFrames = false;
            return false;
        }

        public void Write(byte[] sampleData)
        {
            // If there's not enough room left in the buffer, move any unprocessed data back to the beginning.
            int sampleLength = sampleData.Length;
            if (sampleLength > buffer.Length - writePosition)
            {
                writePosition -= readPosition;
                if (writePosition > buffer.Length - sampleLength)
                {
                    writePosition = 0;
                    sampleLength = Math.Min(sampleLength, buffer.Length); // Fix to correct scale issues when app has been paused for debugging.
                }
                else
                {
                    // Otherwise, move any unprocessed data back to the beginning.
                    Buffer.BlockCopy(buffer, readPosition, buffer, 0, writePosition);
                }
                readPosition = 0;
            }

            Buffer.BlockCopy(sampleData, 0, buffer, writePosition, sampleLength);
            writePosition += sampleLength;
        }

        public string InstanceName { get; set; }

        public void RegisterFramePlayed(byte[] speakerSample)
        {
            // No-op
        }
    }
}
