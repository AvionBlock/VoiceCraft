﻿using Newtonsoft.Json;
using System.Diagnostics;
using VoiceCraft.Core;

namespace VoiceCraft.Server.Data
{
    public class Properties
    {
        const string ConfigFolder = "config";
        const string PropertiesFile = "ServerProperties.json";
        const string BanlistFile = "Banlist.json";
        const string PropertiesDirectory = $"{ConfigFolder}/{PropertiesFile}";
        const string BanlistDirectory = $"{ConfigFolder}/{BanlistFile}";

        #region Properties
        public ushort VoiceCraftPortUDP { get; set; } = 9050;
        public ushort MCCommPortTCP { get; set; } = 9051;

        //Unchangeable Settings
        public string PermanentServerKey { get; set; } = "";
        public ConnectionTypes ConnectionType { get; set; } = ConnectionTypes.Server;
        public int ExternalServerTimeoutMS { get; set; } = 5000;
        public int ClientTimeoutMS { get; set; } = 8000;
        public List<Channel> Channels { get; set; } = new List<Channel>();

        //Changeable Settings
        public int ProximityDistance { get; set; } = 30;
        public bool ProximityToggle { get; set; } = true;
        public bool VoiceEffects { get; set; } = true;
        public string ServerMOTD { get; set; } = "VoiceCraft Proximity Chat!";
        public DebugProperties Debugger { get; set; } = new DebugProperties();
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
                var properties = JsonConvert.DeserializeObject<Properties>(jsonString);
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
            if (ServerProperties.ProximityDistance > 120 || ServerProperties.ProximityDistance < 1)
                throw new Exception("Proximity distance can only be between 1 and 120!");
            if (ServerProperties.Channels.Count >= byte.MaxValue)
                throw new Exception($"Cannot have more than {byte.MaxValue - 1} channels!"); //Technically we can only have 254 channels since we start the channelId from 1.
            if (ServerProperties.Channels.Exists(x => x.Name.Length > 12))
                throw new Exception("Channel name cannot be longer than 12 characters!");
            if (ServerProperties.Channels.Exists(x => string.IsNullOrWhiteSpace(x.Name)))
                throw new Exception("Channel name cannot be empty!");
            if (ServerProperties.Channels.Exists(x => x.Password.Length > 12))
                throw new Exception("Channel password cannot be longer than 12 characters!");
            if (ServerProperties.Channels.Exists(x => x.OverrideSettings?.ProximityDistance > 120 || x.OverrideSettings?.ProximityDistance < 1))
                throw new Exception("Channel proximity distance can only be between 1 and 120!");

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

    public enum ConnectionTypes
    {
        Server,
        Client,
        Hybrid
    }
}
