using System;
using System.Collections.Concurrent;
using VoiceCraft.Core;
using VoiceCraft.Network.Interfaces;

namespace VoiceCraft.Network
{
    public static class PacketPool<T> where T : IPooledPacket
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