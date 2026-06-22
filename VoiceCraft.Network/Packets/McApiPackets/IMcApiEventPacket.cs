using LiteNetLib.Utils;
using VoiceCraft.Network.Interfaces;
using VoiceCraft.Network.Packets.McApiPackets.Event;

namespace VoiceCraft.Network.Packets.McApiPackets;

public interface IMcApiEventPacket : INetSerializable, IPooledPacket
{
    EventType EventType { get; }

    public static IMcApiEventPacket? FromReader(EventType eventType, NetDataReader reader)
    {
        IMcApiEventPacket? packet = null;
        switch (eventType)
        {
            case EventType.OnEffectUpdated:
                packet = PacketPool<McApiOnEffectUpdatedPacket>.GetPacket(() =>
                    new McApiOnEffectUpdatedPacket());
                break;
            case EventType.OnEntityCreated:
                packet = PacketPool<McApiOnEntityCreatedPacket>.GetPacket(() =>
                    new McApiOnEntityCreatedPacket());
                break;
            case EventType.OnNetworkEntityCreated:
                packet = PacketPool<McApiOnNetworkEntityCreatedPacket>.GetPacket(() =>
                    new McApiOnNetworkEntityCreatedPacket());
                break;
            case EventType.OnEntityDestroyed:
                packet = PacketPool<McApiOnEntityDestroyedPacket>.GetPacket(() =>
                    new McApiOnEntityDestroyedPacket());
                break;
            case EventType.OnEntityVisibilityUpdated:
                packet = PacketPool<McApiOnEntityVisibilityUpdatedPacket>.GetPacket(() =>
                    new McApiOnEntityVisibilityUpdatedPacket());
                break;
            case EventType.OnEntityWorldIdUpdated:
                packet = PacketPool<McApiOnEntityWorldIdUpdatedPacket>.GetPacket(() =>
                    new McApiOnEntityWorldIdUpdatedPacket());
                break;
            case EventType.OnEntityNameUpdated:
                packet = PacketPool<McApiOnEntityNameUpdatedPacket>.GetPacket(() =>
                    new McApiOnEntityNameUpdatedPacket());
                break;
            case EventType.OnEntityMuteUpdated:
                packet = PacketPool<McApiOnEntityMuteUpdatedPacket>.GetPacket(() =>
                    new McApiOnEntityMuteUpdatedPacket());
                break;
            case EventType.OnEntityDeafenUpdated:
                packet = PacketPool<McApiOnEntityDeafenUpdatedPacket>.GetPacket(() =>
                    new McApiOnEntityDeafenUpdatedPacket());
                break;
            case EventType.OnEntityServerMuteUpdated:
                packet = PacketPool<McApiOnEntityServerMuteUpdatedPacket>.GetPacket(() =>
                    new McApiOnEntityServerMuteUpdatedPacket());
                break;
            case EventType.OnEntityServerDeafenUpdated:
                packet = PacketPool<McApiOnEntityServerDeafenUpdatedPacket>.GetPacket(() =>
                    new McApiOnEntityServerDeafenUpdatedPacket());
                break;
            case EventType.OnEntityTalkBitmaskUpdated:
                packet = PacketPool<McApiOnEntityTalkBitmaskUpdatedPacket>.GetPacket(() =>
                    new McApiOnEntityTalkBitmaskUpdatedPacket());
                break;
            case EventType.OnEntityListenBitmaskUpdated:
                packet = PacketPool<McApiOnEntityListenBitmaskUpdatedPacket>.GetPacket(() =>
                    new McApiOnEntityListenBitmaskUpdatedPacket());
                break;
            case EventType.OnEntityEffectBitmaskUpdated:
                packet = PacketPool<McApiOnEntityEffectBitmaskUpdatedPacket>.GetPacket(() =>
                    new McApiOnEntityEffectBitmaskUpdatedPacket());
                break;
            case EventType.OnEntityPositionUpdated:
                packet = PacketPool<McApiOnEntityPositionUpdatedPacket>.GetPacket(() =>
                    new McApiOnEntityPositionUpdatedPacket());
                break;
            case EventType.OnEntityRotationUpdated:
                packet = PacketPool<McApiOnEntityRotationUpdatedPacket>.GetPacket(() =>
                    new McApiOnEntityRotationUpdatedPacket());
                break;
            case EventType.OnEntityPropertyUpdated:
                packet = PacketPool<McApiOnEntityPropertyUpdatedPacket>.GetPacket(() =>
                    new McApiOnEntityPropertyUpdatedPacket());
                break;
            case EventType.OnEntityAudioReceived:
                packet = PacketPool<McApiOnEntityAudioReceivedPacket>.GetPacket(() =>
                    new McApiOnEntityAudioReceivedPacket());
                break;
            case EventType.OnEntityAudioDataReceived:
                packet = PacketPool<McApiOnEntityAudioDataReceivedPacket>.GetPacket(() =>
                    new McApiOnEntityAudioDataReceivedPacket());
                break;
            case EventType.None:
            default:
                break;
        }

        packet?.Deserialize(reader);
        return packet;
    }
}