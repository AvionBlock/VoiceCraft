using System;
using System.Collections.Generic;
using System.Linq;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Models.Settings;

public class ServersSettings : Setting<ServersSettings>
{
    private List<ServerSettings> _servers = [];

    public bool HideServerAddresses
    {
        get;
        set
        {
            field = value;
            OnUpdated?.Invoke(this);
        }
    }

    public IEnumerable<ServerSettings> Servers
    {
        get => _servers;
        set
        {
            _servers = value.ToList();
            OnUpdated?.Invoke(this);
        }
    }

    public override event Action<ServersSettings>? OnUpdated;

    public void AddServer(ServerSettings serverSettings)
    {
        if (string.IsNullOrWhiteSpace(serverSettings.Name))
            throw new ArgumentException("Settings.Servers.Validation.Name");
        if (string.IsNullOrWhiteSpace(serverSettings.Ip))
            throw new ArgumentException("Settings.Servers.Validation.Ip");
        if (serverSettings.Port < 1)
            throw new ArgumentException("Settings.Servers.Validation.Port");
        if (serverSettings.Name.Length > ServerSettings.NameLimit)
            throw new ArgumentException($"Settings.Servers.Validation.NameLimit:{ServerSettings.NameLimit}");
        if (serverSettings.Ip.Length > ServerSettings.IpLimit)
            throw new ArgumentException($"Settings.Servers.Validation.IpLimit:{ServerSettings.IpLimit}");

        _servers.Insert(0, serverSettings);
        OnUpdated?.Invoke(this);
    }

    public void RemoveServer(ServerSettings serverSettings)
    {
        _servers.Remove(serverSettings);
        OnUpdated?.Invoke(this);
    }

    public void ClearServers()
    {
        _servers.Clear();
        OnUpdated?.Invoke(this);
    }

    public override object Clone()
    {
        var clone = (ServersSettings)MemberwiseClone();
        clone.OnUpdated = null;
        return clone;
    }
}

public class ServerSettings : Setting<ServerSettings>
{
    public const int NameLimit = 12;
    public const int IpLimit = 30;

    public string Name
    {
        get;
        set
        {
            if (value.Length > NameLimit)
                throw new ArgumentException($"Settings.Servers.Validation.NameLimit:{NameLimit}");
            field = value;
            OnUpdated?.Invoke(this);
        }
    } = string.Empty;

    public string Ip
    {
        get;
        set
        {
            if (value.Length > IpLimit)
                throw new ArgumentException($"Settings.Servers.Validation.IpLimit:{IpLimit}");
            field = value;
            OnUpdated?.Invoke(this);
        }
    } = string.Empty;

    public ushort Port
    {
        get;
        set
        {
            if (value < 1)
                throw new ArgumentException("Settings.Servers.Validation.Port");
            field = value;
            OnUpdated?.Invoke(this);
        }
    } = 9050;

    public override event Action<ServerSettings>? OnUpdated;

    public override object Clone()
    {
        var clone = (ServerSettings)MemberwiseClone();
        clone.OnUpdated = null;
        return clone;
    }
}
