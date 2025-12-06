using System;
using System.Collections.Generic;

namespace VoiceCraft.Core.Packets
{
    public abstract class VoiceCraftPacket
    {
        public abstract byte PacketId { get; }
        public abstract bool IsReliable { get; }
        public uint Sequence { get; set; }
        public long Id { get; set; } = long.MinValue;
        public long ResendTime { get; set; }
        /// <summary>
        /// Number of retry attempts for reliable packets. Used atomically with Interlocked.
        /// </summary>
        public int Retries;

        public virtual int ReadPacket(ref byte[] dataStream, int offset = 0)
        {
            Id = BitConverter.ToInt64(dataStream, offset);
            offset += sizeof(long);

            if (IsReliable)
            {
                Sequence = BitConverter.ToUInt32(dataStream, offset);
                offset += sizeof(uint);
            }
            return offset; //Returns the amount of data read.
        }

        public virtual void WritePacket(ref List<byte> dataStream)
        {
            dataStream.Clear(); //Clear the packet stream.
            dataStream.Add(PacketId);
            dataStream.AddRange(BitConverter.GetBytes(Id));
            if(IsReliable)
                dataStream.AddRange(BitConverter.GetBytes(Sequence));
        }

        public VoiceCraftPacket Clone()
        {
            return (VoiceCraftPacket)MemberwiseClone();
        }
    }

    public enum VoiceCraftPacketTypes : byte
    {
        //SYSTEM/PROTOCOL PACKETS
        Login,
        Logout,
        Accept,
        Deny,
        Ack,
        Ping,
        PingInfo,

        //USER PACKETS
        Binded,
        Unbinded,
        ParticipantJoined,
        ParticipantLeft,
        Mute,
        Unmute,
        Deafen,
        Undeafen,
        JoinChannel,
        LeaveChannel,
        AddChannel,
        RemoveChannel,

        //VOICE PACKETS
        UpdatePosition,
        FullUpdatePosition,
        UpdateEnvironmentId,
        ClientAudio,
        ServerAudio
    }
}