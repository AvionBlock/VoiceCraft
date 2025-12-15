using System;
using System.Collections.Generic;
using System.Linq;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Models.Settings;

public class ServersSettings : Setting<ServersSettings>
{
    private bool _hideServerAddresses;
    private List<Server> _servers = [];

    public bool HideServerAddresses
    {
        get => _hideServerAddresses;
        set
        {
            _hideServerAddresses = value;
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
            throw new Exception("Server name cannot be empty or whitespace!");
        if (string.IsNullOrWhiteSpace(server.Ip))
            throw new Exception("Server IP cannot be empty or whitespace!");
        if (server.Name.Length > Server.NameLimit)
            throw new Exception($"Server name cannot be longer than {Server.NameLimit} characters!");
        if (server.Ip.Length > Server.IpLimit)
            throw new Exception($"Server IP cannot be longer than {Server.IpLimit} characters!");

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
    private string _ip = string.Empty;

    private string _name = string.Empty;
    private ushort _port = 9050;

    public string Name
    {
        get => _name;
        set
        {
            if (value.Length > NameLimit)
                throw new ArgumentException($"Name cannot be longer than {NameLimit} characters!");
            _name = value;
            OnUpdated?.Invoke(this);
        }
    }

    public string Ip
    {
        get => _ip;
        set
        {
            if (value.Length > IpLimit)
                throw new ArgumentException($"IP address cannot be longer than {IpLimit} characters!");
            _ip = value;
            OnUpdated?.Invoke(this);
        }
    }

    public ushort Port
    {
        get => _port;
        set
        {
            _port = value;
            OnUpdated?.Invoke(this);
        }
    }

    public override event Action<Server>? OnUpdated;

    public override object Clone()
    {
        var clone = (Server)MemberwiseClone();
        clone.OnUpdated = null;
        return clone;
    }
}