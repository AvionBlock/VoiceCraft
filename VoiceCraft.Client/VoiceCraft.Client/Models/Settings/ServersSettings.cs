using System;
using System.Collections.Generic;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Models.Settings
{
    public class ServersSettings : Setting<ServersSettings>
    {
        public override event Action<ServersSettings>? OnUpdated;

        public bool HideServerAddresses
        {
            get => _hideServerAddresses;
            set
            {
                _hideServerAddresses = value;
                OnUpdated?.Invoke(this);
            }
        }
        public IEnumerable<Server> Servers => _servers.ToArray();

        private bool _hideServerAddresses;
        private readonly List<Server> _servers = [];

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
            return MemberwiseClone();
        }
    }

    public class Server : Setting<Server>
    {
        public override event Action<Server>? OnUpdated;
        
        public const int NameLimit = 12;
        public const int IpLimit = 30;
        
        private string _name = string.Empty;
        private string _ip = string.Empty;
        private ushort _port;
        private string _token = string.Empty;
        
        public string Name
        {
            get => _name;
            set
            {
                if(value.Length > NameLimit)
                    throw new ArgumentException("Name cannot be longer than {NameLimit} characters!");
                _name = value;
                OnUpdated?.Invoke(this);
            }
        }

        public string Ip
        {
            get => _ip;
            set
            {
                if(value.Length > IpLimit)
                    throw new ArgumentException("IP address cannot be longer than {IPLimit} characters!");
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

        public string Token
        {
            get => _token;
            set
            {
                _token = value;
                OnUpdated?.Invoke(this);
            }
        }

        public override object Clone()
        {
            return MemberwiseClone();
        }
    }
}