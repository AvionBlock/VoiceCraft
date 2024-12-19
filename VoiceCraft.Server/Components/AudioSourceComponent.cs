using Arch.Core;

namespace VoiceCraft.Server.Components
{
    public class AudioSourceComponent(World world, ref Entity entity) : Core.Components.AudioSourceComponent(world, ref entity);
}