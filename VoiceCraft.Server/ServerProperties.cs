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
            AnsiConsole.MarkupLine($"[green]{Localizer.Get("ServerProperties.Success")}[/]");
            return;
        }

        var file = files[0];
        _properties = LoadFile(file, throwOnInvalidProperties);
        ParseAudioEffects();
        AnsiConsole.MarkupLine($"[green]{Localizer.Get("ServerProperties.Success")}[/]");
    }

    public void ApplyRuntimeOverrides(ServerRuntimeOverrides overrides)
    {
        if (!string.IsNullOrWhiteSpace(overrides.ServerKey))
        {
            _properties.McHttpConfig.LoginToken = overrides.ServerKey;
            _properties.McTcpConfig.LoginToken = overrides.ServerKey;
            _properties.McWssConfig.LoginToken = overrides.ServerKey;
        }

        if (overrides.TransportPort is >= 1 and <= 65535)
        {
            _properties.McTcpConfig.Port = overrides.TransportPort.Value;
            _properties.McHttpConfig.Hostname = SetHttpPort(_properties.McHttpConfig.Hostname, overrides.TransportPort.Value);
        }

        if (!string.IsNullOrWhiteSpace(overrides.TransportHost))
        {
            _properties.McTcpConfig.Hostname = overrides.TransportHost;
            _properties.McHttpConfig.Hostname = SetHttpHost(_properties.McHttpConfig.Hostname, overrides.TransportHost);
        }

        if (overrides.TransportMode.Length > 0)
            ApplyTransportModeOverrides(overrides.TransportMode);
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
            AnsiConsole.MarkupLine($"[green]{Localizer.Get("ServerProperties.Generating.Success")}[/]");
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

    private static string SetHttpHost(string configuredHostname, string host)
    {
        if (!Uri.TryCreate(configuredHostname, UriKind.Absolute, out var uri))
            return configuredHostname;

        return new UriBuilder(uri)
        {
            Host = host
        }.Uri.ToString();
    }

    private static string SetHttpPort(string configuredHostname, int port)
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

    public LiteNetVoiceCraftServer.LiteNetVoiceCraftConfig VoiceCraftConfig { get; set; } = new();
    public McWssMcApiServer.McWssMcApiConfig McWssConfig { get; set; } = new();
    public HttpMcApiServer.HttpMcApiConfig McHttpConfig { get; set; } = new();
    public TcpMcApiServer.McTcpConfig McTcpConfig { get; set; } = new();
    public Dictionary<ushort, JsonElement> DefaultAudioEffectsConfig { get; set; } = [];
}

public class ServerRuntimeOverrides
{
    public string[] TransportMode { get; set; } = [];
    public string? TransportHost { get; set; }
    public int? TransportPort { get; set; }
    public string? ServerKey { get; set; }
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ServerPropertiesStructure), GenerationMode = JsonSourceGenerationMode.Metadata)]
public partial class ServerPropertiesStructureGenerationContext : JsonSerializerContext;
