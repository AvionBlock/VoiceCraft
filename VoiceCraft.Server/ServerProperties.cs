using System.Text.Json;
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
        }

        if (!string.IsNullOrWhiteSpace(options.TransportHost))
        {
            _properties.McTcpConfig.Hostname = options.TransportHost;
            _properties.McHttpConfig.Hostname = SetUriHost(_properties.McHttpConfig.Hostname, options.TransportHost);
            _properties.McWssConfig.Hostname = SetUriHost(_properties.McWssConfig.Hostname, options.TransportHost);
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
            var properties =
                JsonSerializer.Deserialize<ServerPropertiesStructure>(text,
                    ServerPropertiesStructureGenerationContext.Default.ServerPropertiesStructure);
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

            File.WriteAllText(filePath,
                JsonSerializer.Serialize(properties,
                    ServerPropertiesStructureGenerationContext.Default.ServerPropertiesStructure));
            AnsiConsole.MarkupLine($"[green]{Localizer.Get($"ServerProperties.Generating.Success:{path}")}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine(
                $"[red]{Localizer.Get($"ServerProperties.Generating.Failed:{path},{ex.Message}")}[/]");
        }

        return properties;
    }

    private void ParseAudioEffects()
    {
        foreach (var effect in DefaultAudioEffectsConfig)
        {
            if (effect.Key == 0) continue;
            var audioEffect = IAudioEffect.FromJsonElement(effect.Value);
            if (audioEffect == null) continue;
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

    private void ApplyTransportModeOverrides(IReadOnlyCollection<string> transportModes)
    {
        _properties.McHttpConfig.Enabled = false;
        _properties.McTcpConfig.Enabled = false;
        _properties.McWssConfig.Enabled = false;

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
                default:
                    throw new ArgumentException(
                        $"Unsupported transport mode '{transportMode}'. Supported values are: http, tcp, wss.");
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

    public bool TelemetryEnabled { get; set; } = true;
    public string TelemetryToken { get; set; } = Guid.NewGuid().ToString("N");
    public LiteNetVoiceCraftServer.LiteNetVoiceCraftConfig VoiceCraftConfig { get; set; } = new();
    public McWssMcApiServer.McWssMcApiConfig McWssConfig { get; set; } = new();
    public HttpMcApiServer.HttpMcApiConfig McHttpConfig { get; set; } = new();
    public TcpMcApiServer.McTcpConfig McTcpConfig { get; set; } = new();
    public Dictionary<ushort, JsonElement> DefaultAudioEffectsConfig { get; set; } = [];
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
