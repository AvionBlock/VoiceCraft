using VoiceCraft.Core.World;

namespace VoiceCraft.Core.Interfaces
{
    public interface IVisible
    {
        bool Visibility(VoiceCraftEntity from, VoiceCraftEntity to, uint effectBitmask);
    }
}