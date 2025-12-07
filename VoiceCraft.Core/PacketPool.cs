using System;
using System.Collections.Concurrent;

namespace VoiceCraft.Core
{
    public static class PacketPool<T>
    {
        private static readonly ConcurrentBag<T> Packets = new ConcurrentBag<T>();
        
        public static T GetPacket()
        {
            return Packets.TryTake(out var packet) ? packet : Activator.CreateInstance<T>();
        }

        public static void Return(T packet)
        {
            Packets.Add(packet);
        }
    }
}