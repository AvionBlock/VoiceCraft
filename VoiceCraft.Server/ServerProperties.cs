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
            ApplyEnvironmentOverrides(_properties);
            ParseAudioEffects();
            AnsiConsole.MarkupLine($"[green]{Localizer.Get("ServerProperties.Success")}[/]");
            return;
        }

        var file = files[0];
        _properties = LoadFile(file, throwOnInvalidProperties);
        ApplyEnvironmentOverrides(_properties);
        ParseAudioEffects();
        AnsiConsole.MarkupLine($"[green]{Localizer.Get("ServerProperties.Success")}[/]");
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

    private static void ApplyEnvironmentOverrides(ServerPropertiesStructure properties)
    {
        var transportMode = Environment.GetEnvironmentVariable("GEYSERVOICE_TRANSPORT_MODE");
        var transportHost = Environment.GetEnvironmentVariable("GEYSERVOICE_TRANSPORT_HOST");
        var transportPort = Environment.GetEnvironmentVariable("GEYSERVOICE_TRANSPORT_PORT");
        var serverKey = Environment.GetEnvironmentVariable("GEYSERVOICE_SERVER_KEY");
        var httpEnabled = Environment.GetEnvironmentVariable("GEYSERVOICE_HTTP_ENABLED");

        if (!string.IsNullOrWhiteSpace(serverKey))
        {
            properties.McHttpConfig.LoginToken = serverKey;
            properties.McTcpConfig.LoginToken = serverKey;
            properties.McWssConfig.LoginToken = serverKey;
        }

        if (!string.IsNullOrWhiteSpace(transportPort) && int.TryParse(transportPort, out var parsedPort) &&
            parsedPort is >= 1 and <= 65535)
        {
            properties.McTcpConfig.Port = parsedPort;
            properties.McHttpConfig.Hostname = SetHttpPort(properties.McHttpConfig.Hostname, parsedPort);
        }

        if (!string.IsNullOrWhiteSpace(transportHost))
        {
            properties.McTcpConfig.Hostname = transportHost;
            properties.McHttpConfig.Hostname = SetHttpHost(properties.McHttpConfig.Hostname, transportHost);
        }

        if (!string.IsNullOrWhiteSpace(transportMode))
        {
            switch (transportMode.Trim().ToLowerInvariant())
            {
                case "local-socket":
                case "tcp":
                case "tcp-socket":
                    properties.McTcpConfig.Enabled = true;
                    properties.McHttpConfig.Enabled = false;
                    break;
                case "http":
                    properties.McHttpConfig.Enabled = true;
                    properties.McTcpConfig.Enabled = false;
                    break;
            }
        }

        if (!string.IsNullOrWhiteSpace(httpEnabled) && bool.TryParse(httpEnabled, out var isHttpEnabled))
            properties.McHttpConfig.Enabled = isHttpEnabled;
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

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ServerPropertiesStructure), GenerationMode = JsonSourceGenerationMode.Metadata)]
public partial class ServerPropertiesStructureGenerationContext : JsonSerializerContext;
