using LiteNetLib;
using VoiceCraft.Core.Network.Packets;

namespace VoiceCraft.Server
{
    public class PacketEventHandler
    {
        private readonly VoiceCraftServer _server;
        
        public PacketEventHandler(VoiceCraftServer server)
        {
            _server = server;
            
            _server.Listener.NetworkReceiveEvent += ListenerOnNetworkReceiveEvent;
        }

        private void ListenerOnNetworkReceiveEvent(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliverymethod)
        {
            var packetType = reader.GetByte();
            var pt = (PacketType)packetType;
            switch (pt)
            {
                //Unused Packet Types.
                case PacketType.Login:
                case PacketType.Info:
                case PacketType.Audio:
                case PacketType.EntityCreated:
                case PacketType.EntityDestroyed:
                case PacketType.SetLocalEntity:
                default:
                    break;
            }

            reader.Recycle();
        }
    }
}