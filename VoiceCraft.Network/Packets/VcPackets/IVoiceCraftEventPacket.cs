using LiteNetLib.Utils;
using VoiceCraft.Network.Interfaces;
using VoiceCraft.Network.Packets.VcPackets.Event;

namespace VoiceCraft.Network.Packets.VcPackets;

public interface IVoiceCraftEventPacket : INetSerializable, IPooledPacket
{
    EventType EventType { get; }

    public static IVoiceCraftEventPacket? FromReader(EventType eventType, NetDataReader reader)
    {
        IVoiceCraftEventPacket? packet = null;
        switch (eventType)
        {
            case EventType.OnEffectUpdated:
                packet = PacketPool<VcOnEffectUpdatedPacket>.GetPacket(() =>
                    new VcOnEffectUpdatedPacket());
                break;
            case EventType.OnEntityCreated:
                packet = PacketPool<VcOnEntityCreatedPacket>.GetPacket(() =>
                    new VcOnEntityCreatedPacket());
                break;
            case EventType.OnNetworkEntityCreated:
                packet = PacketPool<VcOnNetworkEntityCreatedPacket>.GetPacket(() =>
                    new VcOnNetworkEntityCreatedPacket());
                break;
            case EventType.OnEntityDestroyed:
                packet = PacketPool<VcOnEntityDestroyedPacket>.GetPacket(() =>
                    new VcOnEntityDestroyedPacket());
                break;
            case EventType.OnEntityNameUpdated:
                packet = PacketPool<VcOnEntityNameUpdatedPacket>.GetPacket(() =>
                    new VcOnEntityNameUpdatedPacket());
                break;
            case EventType.OnEntityMuteUpdated:
                packet = PacketPool<VcOnEntityMuteUpdatedPacket>.GetPacket(() =>
                    new VcOnEntityMuteUpdatedPacket());
                break;
            case EventType.OnEntityDeafenUpdated:
                packet = PacketPool<VcOnEntityDeafenUpdatedPacket>.GetPacket(() =>
                    new VcOnEntityDeafenUpdatedPacket());
                break;
            case EventType.OnEntityServerMuteUpdated:
                packet = PacketPool<VcOnEntityServerMuteUpdatedPacket>.GetPacket(() =>
                    new VcOnEntityServerMuteUpdatedPacket());
                break;
            case EventType.OnEntityServerDeafenUpdated:
                packet = PacketPool<VcOnEntityServerDeafenUpdatedPacket>.GetPacket(() =>
                    new VcOnEntityServerDeafenUpdatedPacket());
                break;
            case EventType.OnEntityTalkBitmaskUpdated:
                packet = PacketPool<VcOnEntityTalkBitmaskUpdatedPacket>.GetPacket(() =>
                    new VcOnEntityTalkBitmaskUpdatedPacket());
                break;
            case EventType.OnEntityListenBitmaskUpdated:
                packet = PacketPool<VcOnEntityListenBitmaskUpdatedPacket>.GetPacket(() =>
                    new VcOnEntityListenBitmaskUpdatedPacket());
                break;
            case EventType.OnEntityEffectBitmaskUpdated:
                packet = PacketPool<VcOnEntityEffectBitmaskUpdatedPacket>.GetPacket(() =>
                    new VcOnEntityEffectBitmaskUpdatedPacket());
                break;
            case EventType.OnEntityPositionUpdated:
                packet = PacketPool<VcOnEntityPositionUpdatedPacket>.GetPacket(() =>
                    new VcOnEntityPositionUpdatedPacket());
                break;
            case EventType.OnEntityRotationUpdated:
                packet = PacketPool<VcOnEntityRotationUpdatedPacket>.GetPacket(() =>
                    new VcOnEntityRotationUpdatedPacket());
                break;
            case EventType.OnEntityPropertyUpdated:
                packet = PacketPool<VcOnEntityPropertyUpdatedPacket>.GetPacket(() =>
                    new VcOnEntityPropertyUpdatedPacket());
                break;
            case EventType.OnEntityAudioDataReceived:
                packet = PacketPool<VcOnEntityAudioDataReceivedPacket>.GetPacket(() =>
                    new VcOnEntityAudioDataReceivedPacket());
                break;
            case EventType.None:
            case EventType.OnEntityVisibilityUpdated:
            case EventType.OnEntityWorldIdUpdated:
            case EventType.OnEntityAudioReceived:
            default:
                break;
        }

        packet?.Deserialize(reader);
        return packet;
    }
}