using System;
using VoiceCraft.Client.Services;
using VoiceCraft.Server;

namespace VoiceCraft.Client.Models.Settings;

public class HostServerSettings : Setting<HostServerSettings>
{
    public ServerPropertiesStructure ServerProperties
    {
        get;
        set
        {
            field = value;
            OnUpdated?.Invoke(this);
        }
    } = new();

    public override event Action<HostServerSettings>? OnUpdated;

    public override object Clone()
    {
        throw new NotSupportedException();
    }
}