namespace VoiceCraft.Core.Interfaces
{
    public interface IVisible
    {
        bool VisibleTo(VoiceCraftEntity fromEntity, VoiceCraftEntity toEntity, ulong bitmask);
    }
}