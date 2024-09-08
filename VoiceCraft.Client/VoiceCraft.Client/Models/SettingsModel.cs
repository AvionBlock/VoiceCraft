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
        private static string SettingsPath = $"{AppContext.BaseDirectory}/Settings.json";

        [ObservableProperty]
        private ObservableCollection<ServerModel> _servers = new ObservableCollection<ServerModel>();

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
