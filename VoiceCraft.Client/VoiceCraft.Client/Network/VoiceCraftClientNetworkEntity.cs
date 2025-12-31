using System;
using VoiceCraft.Core.World;

namespace VoiceCraft.Client.Network;

public class VoiceCraftClientNetworkEntity(int id, VoiceCraftWorld world, Guid userGuid)
    : VoiceCraftClientEntity(id, world)
{
    private bool _serverMuted;
    private bool _serverDeafened;

    public event Action<bool, VoiceCraftClientNetworkEntity>? OnServerMuteUpdated;
    public event Action<bool, VoiceCraftClientNetworkEntity>? OnServerDeafenUpdated;

    public Guid UserGuid { get; private set; } = userGuid;

    public override void Destroy()
    {
        base.Destroy();
        OnServerMuteUpdated = null;
        OnServerDeafenUpdated = null;
    }

    public bool ServerMuted
    {
        get => _serverMuted;
        set
        {
            if (_serverMuted == value) return;
            _serverMuted = value;
            OnServerMuteUpdated?.Invoke(_serverMuted, this);
        }
    }

    public bool ServerDeafened
    {
        get => _serverDeafened;
        set
        {
            if (_serverDeafened == value) return;
            _serverDeafened = value;
            OnServerDeafenUpdated?.Invoke(_serverDeafened, this);
        }
    }
}