using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Settings
{
    public partial class ServersSettings : Setting
    {
        [ObservableProperty]
        private bool _hideServerAddresses = false;
        [ObservableProperty]
        private ObservableCollection<Server> _servers = new ObservableCollection<Server>();

        public void AddServer(Server server)
        {
            if (string.IsNullOrWhiteSpace(server.Name))
                throw new Exception("Server name cannot be empty or whitespace!");
            if (string.IsNullOrWhiteSpace(server.Ip))
                throw new Exception("Server IP cannot be empty or whitespace!");
            if (server.Name.Length > Server.NameLimit)
                throw new Exception($"Server name cannot be longer than {Server.NameLimit} characters!");
            if (server.Ip.Length > Server.IPLimit)
                throw new Exception($"Server IP cannot be longer than {Server.IPLimit} characters!");

            Servers.Insert(0, server);
        }

        public void RemoveServer(Server server)
        {
            Servers.Remove(server);
        }

        public override object Clone()
        {
            return MemberwiseClone();
        }
    }

    public partial class Server : ObservableObject, ICloneable
    {
        public const int NameLimit = 12;
        public const int IPLimit = 30;

        [ObservableProperty]
        private string _name = string.Empty;
        [ObservableProperty]
        private string _ip = string.Empty;
        [ObservableProperty]
        private ushort _port = 9050;
        [ObservableProperty]
        private ushort _key = 0;

        partial void OnNameChanged(string value)
        {
            if (Name.Length > NameLimit)
                Name = Name.Substring(0, NameLimit);
        }

        partial void OnIpChanged(string value)
        {
            if (Ip.Length > IPLimit)
                Ip = Ip.Substring(0, IPLimit);
        }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
