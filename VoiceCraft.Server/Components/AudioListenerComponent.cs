using Arch.Core;

namespace VoiceCraft.Server.Components
{
    public class AudioListenerComponent(World world, ref Entity entity) : Core.Components.AudioListenerComponent(world, ref entity);
}