using System;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Models.Settings;

public class HostServerSettings : Setting<HostServerSettings>
{
    public Server.Runtime.ServerPropertiesStructure ServerProperties
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