using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Models.Settings
{
    public class ServersSettings : Setting
    {
        public bool HideServerAddresses = false;
        public ObservableCollection<Server> Servers = new ObservableCollection<Server>();

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

    public class Server : ICloneable
    {
        public const int NameLimit = 12;
        public const int IPLimit = 30;
        
        public string Name = string.Empty;
        public string Ip = string.Empty;
        public ushort Port = 9050;
        public ushort Key = 0;

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}