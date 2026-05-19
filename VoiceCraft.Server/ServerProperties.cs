using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Spectre.Console;
using VoiceCraft.Core.Locales;
using VoiceCraft.Network.Audio.Effects;
using VoiceCraft.Network.Interfaces;
using VoiceCraft.Network.Servers;

namespace VoiceCraft.Server;

public class ServerProperties
{
    private const string FileName = "ServerProperties.json";
    private const string ConfigPath = "config";

    private ServerPropertiesStructure _properties = new();
    private Dictionary<ushort, JsonElement> DefaultAudioEffectsConfig => _properties.DefaultAudioEffectsConfig;

    public LiteNetVoiceCraftServer.LiteNetVoiceCraftConfig VoiceCraftConfig => _properties.VoiceCraftConfig;
    public WebRtcVoiceCraftServer.WebRtcVoiceCraftConfig WebRtcConfig => _properties.WebRtcConfig;
    public McWssMcApiServer.McWssMcApiConfig McWssConfig => _properties.McWssConfig;
    public HttpMcApiServer.HttpMcApiConfig McHttpConfig => _properties.McHttpConfig;
    public TcpMcApiServer.McTcpConfig McTcpConfig => _properties.McTcpConfig;
    public bool TelemetryEnabled => _properties.TelemetryEnabled;
    public string TelemetryToken => _properties.TelemetryToken;
    public OrderedDictionary<ushort, IAudioEffect> DefaultAudioEffects { get; } = [];

    public void Load(bool throwOnInvalidProperties)
    {
        var files = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, FileName, SearchOption.AllDirectories);
        if (files.Length == 0)
        {
            if (throwOnInvalidProperties)
                throw new Exception(Localizer.Get("ServerProperties.FailNotFound"));
            AnsiConsole.MarkupLine($"[yellow]{Localizer.Get("ServerProperties.NotFound")}[/]");
            _properties = CreateConfigFile();
            ParseAudioEffects();
            AnsiConsole.MarkupLine($"[green]{Localizer.Get($"ServerProperties.Success")}[/]");
            return;
        }

        var file = files[0];
        _properties = LoadFile(file, throwOnInvalidProperties);
        ParseAudioEffects();
        AnsiConsole.MarkupLine($"[green]{Localizer.Get("ServerProperties.Success")}[/]");
    }

    public void ApplyRuntimeOverrides(RuntimeOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.ServerKey))
        {
            _properties.McHttpConfig.LoginToken = options.ServerKey;
            _properties.McTcpConfig.LoginToken = options.ServerKey;
            _properties.McWssConfig.LoginToken = options.ServerKey;
        }

        if (options.TransportPort is >= 1 and <= 65535)
        {
            _properties.McTcpConfig.Port = options.TransportPort.Value;
            _properties.McHttpConfig.Hostname = SetUriPort(_properties.McHttpConfig.Hostname, options.TransportPort.Value);
            _properties.McWssConfig.Hostname = SetUriPort(_properties.McWssConfig.Hostname, options.TransportPort.Value);
            _properties.WebRtcConfig.SignalingUrl =
                SetUriPort(_properties.WebRtcConfig.SignalingUrl, options.TransportPort.Value);
            SetDefaultWebRtcPortRange(options.TransportPort.Value);
        }

        if (!string.IsNullOrWhiteSpace(options.TransportHost))
        {
            _properties.McTcpConfig.Hostname = options.TransportHost;
            _properties.McHttpConfig.Hostname = SetUriHost(_properties.McHttpConfig.Hostname, options.TransportHost);
            _properties.McWssConfig.Hostname = SetUriHost(_properties.McWssConfig.Hostname, options.TransportHost);
            _properties.WebRtcConfig.SignalingUrl =
                SetUriHost(_properties.WebRtcConfig.SignalingUrl, options.TransportHost);
        }

        if (options.TransportMode.Length > 0)
            ApplyTransportModeOverrides(options.TransportMode);
    }

    private static ServerPropertiesStructure LoadFile(string path, bool throwOnInvalidConfig)
    {
        try
        {
            AnsiConsole.MarkupLine($"[yellow]{Localizer.Get($"ServerProperties.Loading:{path}")}[/]");
            var text = File.ReadAllText(path);
            var migratedJson = ServerPropertiesMigrator.Migrate(text, out var migrated);
            var properties = JsonSerializer.Deserialize(
                migratedJson,
                ServerPropertiesStructureGenerationContext.Default.ServerPropertiesStructure);
            if (migrated && properties != null)
            {
                AnsiConsole.MarkupLine(
                    $"[bold yellow]Updating ServerProperties.json to config version {ServerPropertiesMigrator.CurrentVersion}.[/]");
                WriteConfigFile(path, properties);
            }

            return properties ?? throw new Exception(Localizer.Get("ServerProperties.Exceptions.ParseJson"));
        }
        catch (Exception ex)
        {
            if (throwOnInvalidConfig)
                throw;
            AnsiConsole.MarkupLine(
                $"[yellow]{Localizer.Get($"ServerProperties.Failed:{ex.Message}")}[/]");
            LogService.Log(ex);
        }

        return new ServerPropertiesStructure();
    }

    private static ServerPropertiesStructure CreateConfigFile()
    {
        var properties = new ServerPropertiesStructure();
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigPath);
        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigPath, FileName);
        AnsiConsole.MarkupLine($"[yellow]{Localizer.Get($"ServerProperties.Generating.Generating:{path}")}[/]");
        try
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            WriteConfigFile(filePath, properties);
            AnsiConsole.MarkupLine($"[green]{Localizer.Get($"ServerProperties.Generating.Success:{path}")}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine(
                $"[red]{Localizer.Get($"ServerProperties.Generating.Failed:{path},{ex.Message}")}[/]");
        }

        return properties;
    }

    private static void WriteConfigFile(string path, ServerPropertiesStructure properties)
    {
        File.WriteAllText(path, ServerPropertiesJson.Write(properties));
    }

    private void ParseAudioEffects()
    {
        foreach (var effect in DefaultAudioEffectsConfig)
        {
            if (effect.Key == 0) continue;
            var audioEffect = IAudioEffect.FromJsonElement(effect.Value);
            if (audioEffect == null) continue;
            audioEffect.Bitmask = effect.Key;
            DefaultAudioEffects.TryAdd(effect.Key, audioEffect);
        }
    }

    private static string SetUriHost(string configuredHostname, string host)
    {
        if (!Uri.TryCreate(configuredHostname, UriKind.Absolute, out var uri))
            return configuredHostname;

        return new UriBuilder(uri)
        {
            Host = host
        }.Uri.ToString();
    }

    private static string SetUriPort(string configuredHostname, int port)
    {
        if (!Uri.TryCreate(configuredHostname, UriKind.Absolute, out var uri))
            return configuredHostname;

        return new UriBuilder(uri)
        {
            Port = port
        }.Uri.ToString();
    }

    private void SetDefaultWebRtcPortRange(int signalingPort)
    {
        if (signalingPort >= 65535)
            return;

        if (!IsDefaultWebRtcPortRange())
            return;

        var rangeStart = signalingPort + 1;
        var rangeEnd = Math.Min(signalingPort + 10, 65535);
        _properties.WebRtcConfig.PortRangeStart = rangeStart;
        _properties.WebRtcConfig.PortRangeEnd = rangeEnd;
    }

    private bool IsDefaultWebRtcPortRange() =>
        (_properties.WebRtcConfig.PortRangeStart == null && _properties.WebRtcConfig.PortRangeEnd == null) ||
        (_properties.WebRtcConfig.PortRangeStart == 9053 && _properties.WebRtcConfig.PortRangeEnd == 9062);

    private void ApplyTransportModeOverrides(IReadOnlyCollection<string> transportModes)
    {
        _properties.McHttpConfig.Enabled = false;
        _properties.McTcpConfig.Enabled = false;
        _properties.McWssConfig.Enabled = false;
        _properties.WebRtcConfig.Enabled = false;

        foreach (var transportMode in ParseTransportModes(transportModes))
        {
            switch (transportMode)
            {
                case "http":
                    _properties.McHttpConfig.Enabled = true;
                    break;
                case "tcp":
                    _properties.McTcpConfig.Enabled = true;
                    break;
                case "wss":
                case "ws":
                case "websocket":
                case "websockets":
                    _properties.McWssConfig.Enabled = true;
                    break;
                case "webrtc":
                case "rtc":
                    _properties.WebRtcConfig.Enabled = true;
                    break;
                default:
                    throw new ArgumentException(
                        $"Unsupported transport mode '{transportMode}'. Supported values are: http, tcp, wss, webrtc.");
            }
        }
    }

    private static IEnumerable<string> ParseTransportModes(IEnumerable<string> transportModes)
    {
        return transportModes
            .SelectMany(x => x.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Select(NormalizeTransportMode)
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static string NormalizeTransportMode(string transportMode)
    {
        return transportMode.Trim().ToLowerInvariant() switch
        {
            "local-socket" => "tcp",
            "tcp-socket" => "tcp",
            _ => transportMode.Trim().ToLowerInvariant()
        };
    }
}

public class ServerPropertiesStructure
{
    public ServerPropertiesStructure()
    {
        DefaultAudioEffectsConfig.Add(1,
            JsonSerializer.SerializeToElement(new VisibilityEffect(),
                VisibilityEffectGenerationContext.Default.VisibilityEffect));
        DefaultAudioEffectsConfig.Add(2,
            JsonSerializer.SerializeToElement(new ProximityEffect { MaxRange = 30 },
                ProximityEffectGenerationContext.Default.ProximityEffect));
        DefaultAudioEffectsConfig.Add(4,
            JsonSerializer.SerializeToElement(new ProximityEchoEffect { Range = 30 },
                ProximityEchoEffectGenerationContext.Default.ProximityEchoEffect));
        DefaultAudioEffectsConfig.Add(8,
            JsonSerializer.SerializeToElement(new ProximityMuffleEffect(),
                ProximityMuffleEffectGenerationContext.Default.ProximityMuffleEffect));
    }

    public string ConfigVersion { get; set; } = ServerPropertiesMigrator.CurrentVersion;
    public bool TelemetryEnabled { get; set; } = true;
    public string TelemetryToken { get; set; } = Guid.NewGuid().ToString("N");
    public LiteNetVoiceCraftServer.LiteNetVoiceCraftConfig VoiceCraftConfig { get; set; } = new();
    public WebRtcVoiceCraftServer.WebRtcVoiceCraftConfig WebRtcConfig { get; set; } = new();
    public McWssMcApiServer.McWssMcApiConfig McWssConfig { get; set; } = new();
    public HttpMcApiServer.HttpMcApiConfig McHttpConfig { get; set; } = new();
    public TcpMcApiServer.McTcpConfig McTcpConfig { get; set; } = new();
    public Dictionary<ushort, JsonElement> DefaultAudioEffectsConfig { get; set; } = [];
}

public static class ServerPropertiesMigrator
{
    public static string CurrentVersion => VoiceCraftServer.Version.ToString();

    public static string Migrate(string json, out bool migrated)
    {
        var node = JsonNode.Parse(json);
        if (node is not JsonObject config)
            throw new Exception(Localizer.Get("ServerProperties.Exceptions.ParseJson"));

        var version = ReadConfigVersion(config["ConfigVersion"]);
        var currentVersion = VoiceCraftServer.Version;
        if (version > currentVersion)
        {
            AnsiConsole.MarkupLine(
                $"[bold yellow]ServerProperties.json config version {version} is newer than this VoiceCraft server version {currentVersion}. It may not work correctly.[/]");
            migrated = false;
            return config.ToJsonString();
        }

        var defaults = JsonNode.Parse(JsonSerializer.Serialize(
            new ServerPropertiesStructure(),
            ServerPropertiesStructureGenerationContext.Default.ServerPropertiesStructure))!.AsObject();

        migrated = version < currentVersion;
        if (MergeDefaults(config, defaults))
            migrated = true;

        if (version < currentVersion)
        {
            ApplyCurrentVersionMigrations(config);
            migrated = true;
        }

        config["ConfigVersion"] = CurrentVersion;
        return config.ToJsonString();
    }

    private static Version ReadConfigVersion(JsonNode? node)
    {
        if (node == null)
            return new Version(0, 0, 0);

        if (node.GetValueKind() == JsonValueKind.String &&
            Version.TryParse(node.GetValue<string>(), out var version))
            return version;

        throw new InvalidOperationException("ServerProperties.json ConfigVersion must be a server version string.");
    }

    private static void ApplyCurrentVersionMigrations(JsonObject config)
    {
        Rename(config, "VoiceCraftWebRtcConfig", "WebRtcConfig");
    }

    private static bool MergeDefaults(JsonObject target, JsonObject defaults)
    {
        var changed = false;
        foreach (var property in defaults)
        {
            if (!target.TryGetPropertyValue(property.Key, out var targetValue) || targetValue == null)
            {
                target[property.Key] = property.Value?.DeepClone();
                changed = true;
                continue;
            }

            if (targetValue is JsonObject targetObject && property.Value is JsonObject defaultObject)
                changed |= MergeDefaults(targetObject, defaultObject);
        }

        return changed;
    }

    private static void Rename(JsonObject config, string oldName, string newName)
    {
        if (!config.TryGetPropertyValue(oldName, out var value) ||
            config.ContainsKey(newName))
            return;

        config.Remove(oldName);
        config[newName] = value;
    }
}

public static class ServerPropertiesJson
{
    public static string Write(ServerPropertiesStructure properties) =>
        JsonSerializer.Serialize(
            properties,
            ServerPropertiesStructureGenerationContext.Default.ServerPropertiesStructure);
}

public class RuntimeOptions
{
    public bool ExitOnInvalidProperties { get; init; }
    public string? Language { get; init; }
    public string[] TransportMode { get; init; } = [];
    public string? TransportHost { get; init; }
    public int? TransportPort { get; init; }
    public string? ServerKey { get; init; }
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ServerPropertiesStructure), GenerationMode = JsonSourceGenerationMode.Metadata)]
public partial class ServerPropertiesStructureGenerationContext : JsonSerializerContext;
