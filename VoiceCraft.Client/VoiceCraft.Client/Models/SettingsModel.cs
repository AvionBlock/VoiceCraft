using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace VoiceCraft.Client.Models
{
    public partial class SettingsModel : ObservableObject
    {
        private const byte NameLimit = 12;
        private const byte IPLimit = 20;

        private static string SettingsPath = $"{AppContext.BaseDirectory}/Settings.json";
        public event EventHandler<ServerModel>? OnServerAdded;
        public event EventHandler<ServerModel>? OnServerRemoved;

        [ObservableProperty]
        private ObservableCollection<ServerModel> _servers = new ObservableCollection<ServerModel>();

        public void AddServer(ServerModel server)
        {
            if (string.IsNullOrWhiteSpace(server.Name))
                throw new Exception("Server name cannot be empty or whitespace!");
            if (string.IsNullOrWhiteSpace(server.Ip))
                throw new Exception("Server IP cannot be empty or whitespace!");
            if (server.Name.Length > 12)
                throw new Exception("Server name cannot be longer than 12 characters!");
            if (server.Ip.Length > 20)
                throw new Exception("Server name cannot be longer than 20 characters!");

            Servers.Add(server);
            OnServerAdded?.Invoke(this, server);
        }

        public void RemoveServer(ServerModel server)
        {
            Servers.Remove(server);
            OnServerRemoved?.Invoke(this, server);
        }

        public async Task SaveAsync()
        {
            await File.WriteAllTextAsync(SettingsPath, JsonSerializer.Serialize(this));
        }

        public void Load()
        {
            if (File.Exists(SettingsPath))
            {
                var result = File.ReadAllText(SettingsPath);
                var settings = JsonSerializer.Deserialize<SettingsModel>(result);
                if (settings == null) return;
                Servers = settings.Servers;
            }
        }
    }
}
