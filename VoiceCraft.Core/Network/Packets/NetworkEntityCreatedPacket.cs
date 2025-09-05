using System;
using LiteNetLib.Utils;

namespace VoiceCraft.Core.Network.Packets
{
    public class NetworkEntityCreatedPacket : EntityCreatedPacket
    {
        public NetworkEntityCreatedPacket(int id = 0, string name = "", bool muted = false, bool deafened = false,
            Guid userGuid = new Guid()) :
            base(id, name, muted, deafened)
        {
            UserGuid = userGuid;
        }

        public Guid UserGuid { get; private set; }

        public override void Serialize(NetDataWriter writer)
        {
            base.Serialize(writer);
            writer.Put(UserGuid);
        }

        public override void Deserialize(NetDataReader reader)
        {
            base.Deserialize(reader);
            UserGuid = reader.GetGuid();
        }
    }
}