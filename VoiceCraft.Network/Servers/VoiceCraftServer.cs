using System;
using VoiceCraft.Core;
using VoiceCraft.Core.World;
using VoiceCraft.Network.Systems;

namespace VoiceCraft.Network.Servers;

public abstract class VoiceCraftServer
{
    protected bool Disposed;
    
    public static Version Version { get; } = new(Constants.Major, Constants.Minor, Constants.Patch);
    
    private readonly AudioEffectSystem _audioEffectSystem;
    private readonly VoiceCraftWorld _world;

    protected VoiceCraftServer(VoiceCraftWorld world, AudioEffectSystem audioEffectSystem)
    {
        _world = world;
        _audioEffectSystem = audioEffectSystem;
    }
}