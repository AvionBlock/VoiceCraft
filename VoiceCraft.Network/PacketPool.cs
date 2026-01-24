using System;
using System.Collections.Concurrent;
using VoiceCraft.Core;

namespace VoiceCraft.Network
{
    public static class PacketPool<T>
    {
        private static readonly ConcurrentBag<T> Packets = [];

        public static T GetPacket(Func<T> packetFactory)
        {
            return Packets.TryTake(out var packet) ? packet : packetFactory.Invoke();
        }

        public static void Return(T packet)
        {
            if (Packets.Count >= Constants.MaxPacketPoolSize) return;
            Packets.Add(packet);
        }
    }
}