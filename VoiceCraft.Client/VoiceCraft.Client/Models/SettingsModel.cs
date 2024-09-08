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
            using (var fs = File.OpenWrite(SettingsPath))
            {
                await SaveToStreamAsync(this, fs);
            }
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

        private static async Task SaveToStreamAsync(SettingsModel data, Stream stream)
        {
            await JsonSerializer.SerializeAsync(stream, data).ConfigureAwait(false);
        }
    }
}
