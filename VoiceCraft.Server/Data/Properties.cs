using Newtonsoft.Json;
using VoiceCraft.Core;

namespace VoiceCraft.Server.Data;

/// <summary>
/// Server configuration properties loaded from JSON configuration files.
/// Handles loading and saving of server properties and banlist.
/// </summary>
public class Properties
{
    private const string ConfigFolder = "config";
    private const string PropertiesFile = "ServerProperties.json";
    private const string BanlistFile = "Banlist.json";
    private const string PropertiesDirectory = $"{ConfigFolder}/{PropertiesFile}";
    private const string BanlistDirectory = $"{ConfigFolder}/{BanlistFile}";

    #region Properties
    /// <summary>
    /// Gets or sets the UDP port for VoiceCraft voice communication.
    /// </summary>
    public ushort VoiceCraftPortUDP { get; set; } = 9050;

    /// <summary>
    /// Gets or sets the TCP port for MCComm plugin communication.
    /// </summary>
    public ushort MCCommPortTCP { get; set; } = 9050;

    /// <summary>
    /// Gets or sets the permanent server authentication key.
    /// </summary>
    public string PermanentServerKey { get; set; } = "";

    /// <summary>
    /// Gets or sets the connection type for positioning data.
    /// </summary>
    public ConnectionTypes ConnectionType { get; set; } = ConnectionTypes.Server;

    /// <summary>
    /// Gets or sets the timeout for external server connections in milliseconds.
    /// </summary>
    public int ExternalServerTimeoutMS { get; set; } = 8000;

    /// <summary>
    /// Gets or sets the timeout for client connections in milliseconds.
    /// </summary>
    public int ClientTimeoutMS { get; set; } = 8000;

    /// <summary>
    /// Gets or sets the default channel settings.
    /// </summary>
    public ChannelOverride DefaultSettings { get; set; } = new();

    /// <summary>
    /// Gets or sets the list of available channels.
    /// </summary>
    public List<Channel> Channels { get; set; } = [];

    /// <summary>
    /// Gets the default channel (first channel in the list).
    /// </summary>
    [JsonIgnore]
    public Channel DefaultChannel => Channels[0];

    /// <summary>
    /// Gets or sets the server message of the day.
    /// </summary>
    public string ServerMOTD { get; set; } = "VoiceCraft Proximity Chat!";

    /// <summary>
    /// Gets or sets the debug logging properties.
    /// </summary>
    public DebugProperties Debugger { get; set; } = new();
    #endregion


        public static Properties LoadProperties()
        {
            var ServerProperties = new Properties();

            if (!Directory.Exists(ConfigFolder))
            {
                Directory.CreateDirectory(ConfigFolder);
            }

            //Load properties files and create if not exists.
            if (File.Exists(PropertiesFile))
            {
                Logger.LogToConsole(LogType.Info, $"Loading properties from {PropertiesFile}...", "Properties");
                string jsonString = File.ReadAllText(PropertiesFile);
                var properties = JsonConvert.DeserializeObject<Properties>(jsonString, new JsonSerializerSettings());
                if (properties != null)
                    ServerProperties = properties;
                else
                    Logger.LogToConsole(LogType.Warn, $"Failed to parse {PropertiesFile}. Falling back to default properties.", "Properties");
            }
            else if (File.Exists(PropertiesDirectory))
            {
                Logger.LogToConsole(LogType.Info, $"Loading properties from {PropertiesDirectory}...", "Properties");
                string jsonString = File.ReadAllText(PropertiesDirectory);
                var properties = JsonConvert.DeserializeObject<Properties>(jsonString);
                if (properties != null)
                    ServerProperties = properties;
                else
                    Logger.LogToConsole(LogType.Warn, $"Failed to parse {PropertiesDirectory}. Falling back to default properties.", "Properties");
            }
            else
            {
                Logger.LogToConsole(LogType.Warn, $"{PropertiesFile} file cannot be found. Creating file at {PropertiesDirectory}...", "Properties");
                ServerProperties.Channels.Add(new Channel() { Name = "Main", Hidden = true });

                string jsonString = JsonConvert.SerializeObject(ServerProperties, Formatting.Indented);
                File.WriteAllText(PropertiesDirectory, jsonString);
                Logger.LogToConsole(LogType.Success, $"Successfully created file {PropertiesDirectory}.", "Properties");
            }

            if (ServerProperties.VoiceCraftPortUDP < 1025 || ServerProperties.MCCommPortTCP < 1025)
                throw new Exception("One of the ports is lower than the minimum port 1025!");
            if (ServerProperties.VoiceCraftPortUDP > 65535 || ServerProperties.MCCommPortTCP > 65535)
                throw new Exception("One of the ports is higher than the maximum port 65535!");
            if (ServerProperties.ServerMOTD.Length > 30)
                throw new Exception("Server MOTD cannot be longer than 30 characters!");
            if (ServerProperties.DefaultSettings.ProximityDistance > Constants.MaxProximityDistance || ServerProperties.DefaultSettings.ProximityDistance < Constants.MinProximityDistance)
                throw new Exception($"Default proximity distance can only be between {Constants.MinProximityDistance} and {Constants.MaxProximityDistance}!");
            if (ServerProperties.Channels.Count >= byte.MaxValue)
                throw new Exception($"Cannot have more than {byte.MaxValue} channels!");
            if (ServerProperties.Channels.Exists(x => x.Name.Length > 12))
                throw new Exception("Channel name cannot be longer than 12 characters!");
            if (ServerProperties.Channels.Exists(x => string.IsNullOrWhiteSpace(x.Name)))
                throw new Exception("Channel name cannot be empty!");
            if (ServerProperties.Channels.Exists(x => x.Password.Length > 12))
                throw new Exception("Channel password cannot be longer than 12 characters!");
            if (ServerProperties.Channels.Exists(x => x.OverrideSettings?.ProximityDistance > Constants.MaxProximityDistance || x.OverrideSettings?.ProximityDistance < Constants.MinProximityDistance))
                throw new Exception($"Channel proximity distance can only be between {Constants.MinProximityDistance} and {Constants.MaxProximityDistance}!");

            if (ServerProperties.Channels.Count <= 0)
            {
                Logger.LogToConsole(LogType.Warn, $"No default channel set, adding default channel Main...", "Properties");
                ServerProperties.Channels.Add(new Channel() { Name = "Main", Hidden = true });
            }

            if (string.IsNullOrWhiteSpace(ServerProperties.PermanentServerKey))
            {
                Logger.LogToConsole(LogType.Warn, "Permanent server key not set or empty. Generating temporary key.", "Properties");
                ServerProperties.PermanentServerKey = Guid.NewGuid().ToString();
            }

            if (string.IsNullOrWhiteSpace(ServerProperties.ServerMOTD))
            {
                Logger.LogToConsole(LogType.Warn, "Server MOTD is not set. Setting to default message.", "Properties");
                ServerProperties.ServerMOTD = "VoiceCraft Proximity Chat!";
            }

            Logger.LogToConsole(LogType.Success, "Loaded properties successfully!", "Properties");

            return ServerProperties;
        }

        public static List<string> LoadBanlist()
        {
            var Banlist = new List<string>();
            //Load banlist files and create if not exists.
            if (File.Exists(BanlistFile))
            {
                Logger.LogToConsole(LogType.Info, $"Loading banlist from {BanlistFile}...", "Banlist");
                string jsonString = File.ReadAllText(BanlistFile);
                var banlist = JsonConvert.DeserializeObject<List<string>>(jsonString);
                if (banlist != null)
                    Banlist = banlist;
                else
                    Logger.LogToConsole(LogType.Warn, $"Failed to parse {BanlistFile}. Falling back to default banlist.", "Banlist");
            }
            else if (File.Exists(BanlistDirectory))
            {
                Logger.LogToConsole(LogType.Info, $"Loading banlist from {BanlistDirectory}...", "Banlist");
                string jsonString = File.ReadAllText(BanlistDirectory);
                var banlist = JsonConvert.DeserializeObject<List<string>>(jsonString);
                if (banlist != null)
                    Banlist = banlist;
                else
                    Logger.LogToConsole(LogType.Warn, $"Failed to parse {BanlistDirectory}. Falling back to default banlist.", "Banlist");
            }
            else
            {
                Logger.LogToConsole(LogType.Warn, $"{BanlistFile} file cannot be found. Creating file at {BanlistDirectory}...", "Banlist");
                string jsonString = JsonConvert.SerializeObject(Banlist, Formatting.Indented);
                File.WriteAllText(BanlistDirectory, jsonString);
                Logger.LogToConsole(LogType.Success, $"Successfully created file {BanlistDirectory}.", "Banlist");
            }

            Logger.LogToConsole(LogType.Success, "Loaded banlist successfully!", "Banlist");
            return Banlist;
        }

    /// <summary>
    /// Saves the banlist to file.
    /// </summary>
    /// <param name="banlist">The list of banned player names.</param>
    public static void SaveBanlist(List<string> banlist)
    {
        if (!File.Exists(BanlistDirectory))
        {
            Logger.LogToConsole(LogType.Warn, $"{BanlistDirectory} file does not exist. Creating file...", "Banlist");
            string jsonString = JsonConvert.SerializeObject(banlist, Formatting.Indented);
            File.WriteAllText(BanlistDirectory, jsonString);
            Logger.LogToConsole(LogType.Success, $"Successfully created file {BanlistDirectory}.", "Banlist");
        }
        else
        {
            string jsonString = JsonConvert.SerializeObject(banlist, Formatting.Indented);
            File.WriteAllText(BanlistDirectory, jsonString);
        }
    }
}

/// <summary>
/// Server connection/positioning mode types.
/// </summary>
public enum ConnectionTypes
{
    /// <summary>Server-side positioning via Minecraft plugin.</summary>
    Server,
    /// <summary>Client-side positioning via WebSocket connection.</summary>
    Client,
    /// <summary>Hybrid mode supporting both positioning methods.</summary>
    Hybrid
}

