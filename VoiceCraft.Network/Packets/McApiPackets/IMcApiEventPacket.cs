using System;
using LiteNetLib.Utils;
using VoiceCraft.Network.Packets.McApiPackets.Event;

namespace VoiceCraft.Network.Packets.McApiPackets;

public interface IMcApiEventPacket : INetSerializable
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

    public static void ReturnPacket(IMcApiEventPacket packet)
    {
        switch (packet)
        {
            case McApiOnEffectUpdatedPacket packetType:
                PacketPool<McApiOnEffectUpdatedPacket>.Return(packetType);
                break;
            case McApiOnEntityCreatedPacket packetType:
                if (packetType is McApiOnNetworkEntityCreatedPacket networkPacketType)
                {
                    PacketPool<McApiOnNetworkEntityCreatedPacket>.Return(networkPacketType);
                    break;
                }

                PacketPool<McApiOnEntityCreatedPacket>.Return(packetType);
                break;
            case McApiOnEntityDestroyedPacket packetType:
                PacketPool<McApiOnEntityDestroyedPacket>.Return(packetType);
                break;
            case McApiOnEntityVisibilityUpdatedPacket packetType:
                PacketPool<McApiOnEntityVisibilityUpdatedPacket>.Return(packetType);
                break;
            case McApiOnEntityWorldIdUpdatedPacket packetType:
                PacketPool<McApiOnEntityWorldIdUpdatedPacket>.Return(packetType);
                break;
            case McApiOnEntityNameUpdatedPacket packetType:
                PacketPool<McApiOnEntityNameUpdatedPacket>.Return(packetType);
                break;
            case McApiOnEntityMuteUpdatedPacket packetType:
                PacketPool<McApiOnEntityMuteUpdatedPacket>.Return(packetType);
                break;
            case McApiOnEntityDeafenUpdatedPacket packetType:
                PacketPool<McApiOnEntityDeafenUpdatedPacket>.Return(packetType);
                break;
            case McApiOnEntityServerMuteUpdatedPacket packetType:
                PacketPool<McApiOnEntityServerMuteUpdatedPacket>.Return(packetType);
                break;
            case McApiOnEntityServerDeafenUpdatedPacket packetType:
                PacketPool<McApiOnEntityServerDeafenUpdatedPacket>.Return(packetType);
                break;
            case McApiOnEntityTalkBitmaskUpdatedPacket packetType:
                PacketPool<McApiOnEntityTalkBitmaskUpdatedPacket>.Return(packetType);
                break;
            case McApiOnEntityListenBitmaskUpdatedPacket packetType:
                PacketPool<McApiOnEntityListenBitmaskUpdatedPacket>.Return(packetType);
                break;
            case McApiOnEntityEffectBitmaskUpdatedPacket packetType:
                PacketPool<McApiOnEntityEffectBitmaskUpdatedPacket>.Return(packetType);
                break;
            case McApiOnEntityPositionUpdatedPacket packetType:
                PacketPool<McApiOnEntityPositionUpdatedPacket>.Return(packetType);
                break;
            case McApiOnEntityRotationUpdatedPacket packetType:
                PacketPool<McApiOnEntityRotationUpdatedPacket>.Return(packetType);
                break;
            case McApiOnEntityPropertyUpdatedPacket packetType:
                PacketPool<McApiOnEntityPropertyUpdatedPacket>.Return(packetType);
                break;
            case McApiOnEntityAudioReceivedPacket packetType:
                PacketPool<McApiOnEntityAudioReceivedPacket>.Return(packetType);
                break;
            case McApiOnEntityAudioDataReceivedPacket packetType:
                PacketPool<McApiOnEntityAudioDataReceivedPacket>.Return(packetType);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(packet));
        }
    }
}