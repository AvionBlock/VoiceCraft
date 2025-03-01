namespace VoiceCraft.Server
{
    public static class IDGenerator
    {
        private static List<uint> _allocatedIds = [];

        public static uint Generate()
        {
            for (uint i = 0; i < uint.MaxValue; i++)
            {
                if(_allocatedIds.Contains(i)) continue;
                _allocatedIds.Add(i);
                return i;
            }
            
            throw new Exception("No IDs available!");
        }

        public static bool Return(uint id)
        {
            return _allocatedIds.Remove(id);
        }
    }
}