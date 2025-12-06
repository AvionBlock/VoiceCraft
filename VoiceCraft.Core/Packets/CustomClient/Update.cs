using System.Collections.Generic;
using System;
using System.Numerics;
using System.Text;
using System.Buffers.Binary;

namespace VoiceCraft.Core.Packets.CustomClient
{
    public class Update : CustomClientPacket
    {
        public override byte PacketId => (byte)CustomClientTypes.Update;

        public Vector3 Position { get; set; }
        public float Rotation { get; set; }
        public float CaveDensity { get; set; }
        public bool IsUnderwater { get; set; }
        public string DimensionId { get; set; } = string.Empty;
        public string LevelId { get; set; } = string.Empty;
        public string ServerId { get; set; } = string.Empty;

        public override void Read(ReadOnlySpan<byte> buffer)
        {
            base.Read(buffer);
            int offset = 0;

            float x = BinaryPrimitives.ReadSingleLittleEndian(buffer.Slice(offset));
            offset += sizeof(float);
            float y = BinaryPrimitives.ReadSingleLittleEndian(buffer.Slice(offset));
            offset += sizeof(float);
            float z = BinaryPrimitives.ReadSingleLittleEndian(buffer.Slice(offset));
            offset += sizeof(float);
            Position = new Vector3(x, y, z);

            Rotation = BinaryPrimitives.ReadSingleLittleEndian(buffer.Slice(offset));
            offset += sizeof(float);

            CaveDensity = BinaryPrimitives.ReadSingleLittleEndian(buffer.Slice(offset));
            offset += sizeof(float);

            IsUnderwater = buffer[offset++] != 0;

            int dimLen = BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(offset));
            offset += sizeof(int);
            if (dimLen > 0)
            {
                DimensionId = Encoding.UTF8.GetString(buffer.Slice(offset, dimLen));
                offset += dimLen;
            }

            int levelLen = BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(offset));
            offset += sizeof(int);
            if (levelLen > 0)
            {
                LevelId = Encoding.UTF8.GetString(buffer.Slice(offset, levelLen));
                offset += levelLen;
            }

            int serverLen = BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice(offset));
            offset += sizeof(int);
            if (serverLen > 0)
            {
                ServerId = Encoding.UTF8.GetString(buffer.Slice(offset, serverLen));
                offset += serverLen; // Unnecessary for last field, but good practice
            }
        }

        public override void Write(Span<byte> buffer)
        {
            base.Write(buffer);
            int offset = 1;

            BinaryPrimitives.WriteSingleLittleEndian(buffer.Slice(offset), Position.X);
            offset += sizeof(float);
            BinaryPrimitives.WriteSingleLittleEndian(buffer.Slice(offset), Position.Y);
            offset += sizeof(float);
            BinaryPrimitives.WriteSingleLittleEndian(buffer.Slice(offset), Position.Z);
            offset += sizeof(float);

            BinaryPrimitives.WriteSingleLittleEndian(buffer.Slice(offset), Rotation);
            offset += sizeof(float);

            BinaryPrimitives.WriteSingleLittleEndian(buffer.Slice(offset), CaveDensity);
            offset += sizeof(float);

            buffer[offset++] = IsUnderwater ? (byte)1 : (byte)0;

            int dimBytes = Encoding.UTF8.GetByteCount(DimensionId);
            BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(offset), dimBytes);
            offset += sizeof(int);
            if (dimBytes > 0)
            {
                Encoding.UTF8.GetBytes(DimensionId, buffer.Slice(offset));
                offset += dimBytes;
            }

            int levelBytes = Encoding.UTF8.GetByteCount(LevelId);
            BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(offset), levelBytes);
            offset += sizeof(int);
            if (levelBytes > 0)
            {
                Encoding.UTF8.GetBytes(LevelId, buffer.Slice(offset));
                offset += levelBytes;
            }

            int serverBytes = Encoding.UTF8.GetByteCount(ServerId);
            BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice(offset), serverBytes);
            offset += sizeof(int);
            if (serverBytes > 0)
            {
                Encoding.UTF8.GetBytes(ServerId, buffer.Slice(offset));
                // offset += serverBytes;
            }
        }
    }
}
