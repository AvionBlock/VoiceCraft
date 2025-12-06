using Newtonsoft.Json;
using VoiceCraft.Maui.Interfaces;
using VoiceCraft.Maui.Models;

namespace VoiceCraft.Maui.Services
{
    /// <summary>
    /// Service for managing local data persistence.
    /// </summary>
    public class Database : IDatabaseService
    {
        private const string ServerDb = "Servers.json";
        private const string SettingsDb = "Settings.json";

        private readonly string ServersDbPath = Path.Combine(FileSystem.Current.AppDataDirectory, ServerDb);
        private readonly string SettingsDbPath = Path.Combine(FileSystem.Current.AppDataDirectory, SettingsDb);
        private readonly object _lock = new();

        public event Action<ServerModel>? OnServerAdded;
        public event Action<ServerModel>? OnServerRemoved;

        public SettingsModel Settings { get; private set; } = new SettingsModel();
        public List<ServerModel> Servers { get; private set; } = new List<ServerModel>();

        public Task Initialization { get; private set; }

        public Database()
        {
            Initialization = Initialize();
        }

        private async Task Initialize()
        {
            await Task.Run(() =>
            {
                lock (_lock)
                {
                    try
                    {
                        LoadServers();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error loading servers: {ex}");
                        // Backup corrupt file
                        if (File.Exists(ServersDbPath))
                            File.Copy(ServersDbPath, ServersDbPath + ".bak", true);
                    }

                    try
                    {
                        LoadSettings();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex}");
                        if (File.Exists(SettingsDbPath))
                            File.Copy(SettingsDbPath, SettingsDbPath + ".bak", true);
                    }
                }
            });
        }

        private void LoadServers()
        {
            if (!File.Exists(ServersDbPath))
            {
                // Create empty file
                File.WriteAllText(ServersDbPath, JsonConvert.SerializeObject(Servers));
            }
            else
            {
                var content = File.ReadAllText(ServersDbPath);
                var readDBData = JsonConvert.DeserializeObject<List<ServerModel>>(content);
                if (readDBData != null)
                    Servers = readDBData;
            }
        }

        private void LoadSettings()
        {
            if (!File.Exists(SettingsDbPath))
            {
                // Create default file
                File.WriteAllText(SettingsDbPath, JsonConvert.SerializeObject(Settings));
            }
            else
            {
                var content = File.ReadAllText(SettingsDbPath);
                var readDBData = JsonConvert.DeserializeObject<SettingsModel>(content);
                if (readDBData != null)
                    Settings = readDBData;
            }
        }

        public async Task AddServer(ServerModel server)
        {
            await Initialization; // Ensure loaded
            
            if (string.IsNullOrWhiteSpace(server.Name)) throw new ArgumentException("Name cannot be empty!");
            if (string.IsNullOrEmpty(server.IP)) throw new ArgumentException("IP cannot be empty!");
            if (server.Port < 1025) throw new ArgumentOutOfRangeException(nameof(server.Port), "Port cannot be lower than 1025");
            if (server.Port > 65535) throw new ArgumentOutOfRangeException(nameof(server.Port), "Port cannot be higher than 65535");
            lock (_lock)
            {
                if (Servers.Exists(x => x.Name == server.Name)) throw new InvalidOperationException("Name already exists! Name must be unique!");
                Servers.Add(server);
            }
            OnServerAdded?.Invoke(server);
            await SaveServers();
        }

        public async Task EditServer(ServerModel server)
        {
            await Initialization;

            lock (_lock)
            {
                var foundServer = Servers.FirstOrDefault(x => x.Name == server.Name);
                if (foundServer != null)
                {
                    if (string.IsNullOrWhiteSpace(server.Name)) throw new ArgumentException("Name cannot be empty!");
                    if (string.IsNullOrEmpty(server.IP)) throw new ArgumentException("IP cannot be empty!");
                    if (server.Port < 1025) throw new ArgumentOutOfRangeException(nameof(server.Port), "Port cannot be lower than 1025");
                    if (server.Port > 65535) throw new ArgumentOutOfRangeException(nameof(server.Port), "Port cannot be higher than 65535");

                    foundServer.IP = server.IP;
                    foundServer.Port = server.Port;
                    foundServer.Key = server.Key;
                    goto Save;
                }
            }
            throw new KeyNotFoundException("Server not found!");
            
            Save:
            await SaveServers();
            return;
        }

        public async Task RemoveServer(ServerModel server)
        {
            await Initialization; 

            lock (_lock)
            {
                Servers.Remove(server);
            }
            OnServerRemoved?.Invoke(server);
            await SaveServers();
        }

        public async Task SaveAllAsync()
        {
            // Initialization check handled in respective methods
            await SaveServers();
            await SaveSettings();
        }

        public async Task SaveServers()
        {
            await Initialization;
            
            await Task.Run(() =>
            {
                lock (_lock)
                {
                    using var stream = File.Create(ServersDbPath);
                    using var writer = new StreamWriter(stream);
                    using var jsonWriter = new JsonTextWriter(writer);
                    var serializer = new JsonSerializer { Formatting = Formatting.Indented };
                    serializer.Serialize(jsonWriter, Servers);
                }
            });
        }

        public async Task SaveSettings()
        {
            await Initialization;

            await Task.Run(() =>
            {
                lock (_lock)
                {
                    using var stream = File.Create(SettingsDbPath);
                    using var writer = new StreamWriter(stream);
                    using var jsonWriter = new JsonTextWriter(writer);
                    var serializer = new JsonSerializer { Formatting = Formatting.Indented };
                    serializer.Serialize(jsonWriter, Settings);
                }
            });
        }
    }
}
