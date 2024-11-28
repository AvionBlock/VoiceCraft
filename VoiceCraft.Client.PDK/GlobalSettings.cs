using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace VoiceCraft.Client.PDK
{
    public static class GlobalSettings
    {
        public static int FILE_WRITING_DELAY = 2000;

        private static bool _writing = false;
        private static bool _queueWrite = false;
        private static string SettingsPath = $"{AppContext.BaseDirectory}/GlobalSettings.json";
        private static JsonObject _settings = Load();

        public static void Set<T>(string key, T value) where T : notnull
        {
            _settings[key] = JsonValue.Create(value);
        }

        public static T Get<T>(string key, T? defaultValue = default) where T : notnull
        {
            try
            {
                var value = _settings[key];
                if (value != null) return value.GetValue<T>();
            }
            catch (FormatException) { }
            return defaultValue != null ? defaultValue : (T)Activator.CreateInstance(typeof(T))!;
        }

        public static async Task SaveImmediate()
        {
#if DEBUG
            Debug.WriteLine("Saving immediately. Only use this function if necessary!");
#endif
            await File.WriteAllTextAsync(SettingsPath, JsonSerializer.Serialize(_settings, new JsonSerializerOptions() { WriteIndented = true }));
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
                    await File.WriteAllTextAsync(SettingsPath, JsonSerializer.Serialize(_settings, new JsonSerializerOptions() { WriteIndented = true }));
                }
                _writing = false;
            }
        }

        public static JsonObject Load()
        {
            try
            {
                if (!File.Exists(SettingsPath)) { return new JsonObject(); }

                var loadedSettings = JsonObject.Parse(File.ReadAllText(SettingsPath));
                if (loadedSettings == null) { return new JsonObject(); }

                return (JsonObject)loadedSettings;
            }
            catch (JsonException)
            {
                return new JsonObject();
            }
        }
    }
}
