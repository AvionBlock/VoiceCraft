using System;
using System.Collections.Concurrent;
using VoiceCraft.Core;

namespace VoiceCraft.Network
{
    public static class PacketPool<T>
    {
        private static readonly ConcurrentBag<T> Packets = [];

        public static T GetPacket()
        {
            return Packets.TryTake(out var packet) ? packet : Activator.CreateInstance<T>();
        }

        public static void Return(T packet)
        {
            if (Packets.Count >= Constants.MaxPacketPoolSize) return;
            Packets.Add(packet);
        }
    }
}