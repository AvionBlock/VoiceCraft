namespace VoiceCraft.Server
{
    public static class IdGenerator
    {
        private static readonly List<uint> AllocatedIds = [];

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

        public static bool Return(uint id)
        {
            return AllocatedIds.Remove(id);
        }
    }
}