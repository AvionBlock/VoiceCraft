using System;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Network.World;

public class VoiceCraftClientNetworkEntity(int id, IAudioDecoder decoder, Guid userGuid)
    : VoiceCraftClientEntity(id, decoder)
{
    public Guid UserGuid { get; private set; } = userGuid;

    public bool ServerMuted
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            OnServerMuteUpdated?.Invoke(field, this);
        }
    }

    public bool ServerDeafened
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            OnServerDeafenUpdated?.Invoke(field, this);
        }
    }

    public event Action<bool, VoiceCraftClientNetworkEntity>? OnServerMuteUpdated;
    public event Action<bool, VoiceCraftClientNetworkEntity>? OnServerDeafenUpdated;

    public override void Destroy()
    {
        base.Destroy();
        OnServerMuteUpdated = null;
        OnServerDeafenUpdated = null;
    }
}