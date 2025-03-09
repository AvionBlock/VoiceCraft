using Arch.Core;
using LiteNetLib.Utils;

namespace VoiceCraft.Core
{
    public interface IAudioEffect
    {
        public uint Bitmask { get; set; }
    }

    public interface IAudioInput
    {
    }

    public interface IAudioOutput
    {
    }

    public interface IComponentSerializable : INetSerializable
    {
        World World { get; }
        Entity Entity { get; }
    }
}