using System;
using System.Collections.Generic;

namespace VoiceCraft.Core
{
    public static class IdGenerator
    {
        private static readonly List<uint> AllocatedIds = new List<uint>();

        public static uint Generate()
        {
            for (uint i = 0; i < uint.MaxValue; i++)
            {
                if(AllocatedIds.Contains(i)) continue;
                AllocatedIds.Add(i);
                return i;
            }
            
            throw new Exception("No IDs available!");
        }

        public static bool Allocate(uint id)
        {
            if (AllocatedIds.Contains(id)) return false;
            AllocatedIds.Add(id);
            return true;
        }

        public static bool Return(uint id)
        {
            return AllocatedIds.Remove(id);
        }
    }
}