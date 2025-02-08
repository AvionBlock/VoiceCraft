using LiteNetLib.Utils;
using VoiceCraft.Core.Data;
using VoiceCraft.Core.Data.Packets;

namespace VoiceCraft.Core.Network.Packets
{
    public class EntityAudioPacket : VoiceCraftPacket
    {
        public override PacketType PacketType => PacketType.EntityAudio;
        public override void Serialize(NetDataWriter writer)
        {
            throw new System.NotImplementedException();
        }

        public override void Deserialize(NetDataReader reader)
        {
            throw new System.NotImplementedException();
        }
    }
}