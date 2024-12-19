using System.Collections.Generic;
using Arch.Core;

namespace VoiceCraft.Core.Interfaces
{
    public interface IAudioSource
    {
        public int Read(byte[] buffer, int offset, int count);

        public void GetTrackableEntities(List<Entity> entities);
    }
}