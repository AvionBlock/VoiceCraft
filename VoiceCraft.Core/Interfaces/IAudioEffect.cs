using Arch.Core;

namespace VoiceCraft.Core.Interfaces
{
    public interface IAudioEffect
    {
        public void Process(byte[] buffer, int offset, int count, ref Entity entity);
    }
}