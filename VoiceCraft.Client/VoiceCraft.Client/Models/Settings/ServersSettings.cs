using System;
using System.Collections.Generic;
using System.Linq;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Models.Settings;

public class ServersSettings : Setting<ServersSettings>
{
    private List<Server> _servers = [];

    public bool HideServerAddresses
    {
        get;
        set
        {
            field = value;
            OnUpdated?.Invoke(this);
        }
    }

    public IEnumerable<Server> Servers
    {
        get => _servers;
        set
        {
            _servers = value.ToList();
            OnUpdated?.Invoke(this);
        }
    }

    public override event Action<ServersSettings>? OnUpdated;

    public void AddServer(Server server)
    {
        if (string.IsNullOrWhiteSpace(server.Name))
            throw new ArgumentException("Settings.Servers.Validation.Name");
        if (string.IsNullOrWhiteSpace(server.Ip))
            throw new ArgumentException("Settings.Servers.Validation.Ip");
        if (server.Port < 1)
            throw new ArgumentException("Settings.Servers.Validation.Port");
        if (server.Name.Length > Server.NameLimit)
            throw new ArgumentException($"Settings.Servers.Validation.NameLimit:{Server.NameLimit}");
        if (server.Ip.Length > Server.IpLimit)
            throw new ArgumentException($"Settings.Servers.Validation.IpLimit:{Server.IpLimit}");

        _servers.Insert(0, server);
        OnUpdated?.Invoke(this);
    }

    public void RemoveServer(Server server)
    {
        _servers.Remove(server);
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

public class Server : Setting<Server>
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

    public override event Action<Server>? OnUpdated;

    public override object Clone()
    {
        var clone = (Server)MemberwiseClone();
        clone.OnUpdated = null;
        return clone;
    }
}
