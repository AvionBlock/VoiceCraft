using System;
using System.Collections.Generic;
using System.Buffers;

namespace VoiceCraft.Core.Packets.VoiceCraft
{
    public class ClientAudio : VoiceCraftPacket, IDisposable
    {
        public override byte PacketId => (byte)VoiceCraftPacketTypes.ClientAudio;
        public override bool IsReliable => false;

        public uint PacketCount { get; set; }
        public byte[] Audio { get; set; } = Array.Empty<byte>();
        
        /// <summary>
        /// The actual length of valid audio data in the Audio buffer.
        /// If 0 or not set, Audio.Length is used (backward compatibility).
        /// </summary>
        public int DataLength { get; set; }

        public override int ReadPacket(ref byte[] dataStream, int offset = 0)
        {
            offset = base.ReadPacket(ref dataStream, offset);

            PacketCount = BitConverter.ToUInt32(dataStream, offset); //Read Packet Count - 4 bytes.
            offset += sizeof(uint);

            var audioLength = BitConverter.ToInt32(dataStream, offset); //Read Audio Length - 4 bytes.
            offset += sizeof(int);

            if (audioLength > 0)
            {
                // On receive, we just allocate exactly as before. Pooling on receive is complex due to buffer ownership passing to UI.
                Audio = new byte[audioLength];
                Buffer.BlockCopy(dataStream, offset, Audio, 0, audioLength);
                DataLength = audioLength;
            }

            offset += audioLength;

            return offset;
        }

        public override void WritePacket(ref List<byte> dataStream)
        {
            base.WritePacket(ref dataStream);
            
            var len = DataLength > 0 ? DataLength : Audio.Length;
            
            dataStream.AddRange(BitConverter.GetBytes(PacketCount));
            dataStream.AddRange(BitConverter.GetBytes(len));
            
            // Optimized AddRange for part of array
            if (len == Audio.Length)
            {
                dataStream.AddRange(Audio);
            }
            else
            {
                // Optimized usage of AddRange with ArraySegment
                dataStream.AddRange(new ArraySegment<byte>(Audio, 0, len));
            }
        }

        public void Dispose()
        {
            // If the buffer is from the pool, return it.
            // Assumption: If the array is > 0 length, it *might* be pooled.
            // However, on the RECEIVE side, it is allocated by 'new'. Returning 'new' array to Pool is allowed but might pollute the pool or effective just act as a new buffer provider.
            // BUT, on the SEND side (VoiceCraftClient), we will explicitly use ArrayPool.
            // To be safe, we only return if we are sure, or we just return it anyway. 
            // Returning a non-pooled array to Shared pool is safe (it just accepts it or drops it).
            if (Audio != null && Audio.Length > 0)
            {
                ArrayPool<byte>.Shared.Return(Audio);
                Audio = Array.Empty<byte>();
            }
        }
    }
}
