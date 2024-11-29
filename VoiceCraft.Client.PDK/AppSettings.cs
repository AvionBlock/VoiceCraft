using System.Text.Json;

namespace VoiceCraft.Client.PDK
{
    public static class AppSettings
    {
        private const int FILE_WRITING_DELAY = 2000;
        private static string _appSettingsPath = Path.Combine(AppContext.BaseDirectory, "AppSettings.json");
        private static bool _writing = false;
        private static bool _queueWrite = false;
        private static AppSettingStructure _settings = Load();

        public static List<string> GetDeletedPlugins()
        {
            return _settings.DeletePlugins;
        }

        public static async Task SaveImmediate()
        {
            await File.WriteAllTextAsync(_appSettingsPath, JsonSerializer.Serialize(_settings, new JsonSerializerOptions() { WriteIndented = true }));
        }

        public static async Task SaveAsync()
        {
            _queueWrite = true;
            //Writing boolean is so we don't get multiple loop instances.
            if (!_writing)
            {
                _writing = true;
                while (_queueWrite)
                {
                    _queueWrite = false;
                    await Task.Delay(FILE_WRITING_DELAY);
                    await File.WriteAllTextAsync(_appSettingsPath, JsonSerializer.Serialize(_settings, new JsonSerializerOptions() { WriteIndented = true }));
                }
                _writing = false;
            }
        }

        private static AppSettingStructure Load()
        {
            if (!File.Exists(_appSettingsPath)) { return new AppSettingStructure(); }

            var result = File.ReadAllText(_appSettingsPath);
            var settings = JsonSerializer.Deserialize<AppSettingStructure>(result);
            if (settings == null) return new AppSettingStructure();
            return _settings;
        }

        private class AppSettingStructure
        {
            public List<string> DeletePlugins { get; set; } = new List<string>();
        }
    }
}
