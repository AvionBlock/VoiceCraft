using System.Text.Json;
using System.Text.Json.Serialization;
using Spectre.Console;
using VoiceCraft.Core.Audio.Effects;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Core.Locales;
using VoiceCraft.Server.Config;

namespace VoiceCraft.Server;

public class ServerProperties
{
    private const string FileName = "ServerProperties.json";
    private const string ConfigPath = "config";

    private ServerPropertiesStructure _properties = new();
    private Dictionary<ushort, JsonElement> DefaultAudioEffectsConfig => _properties.DefaultAudioEffectsConfig;

    public VoiceCraftConfig VoiceCraftConfig => _properties.VoiceCraftConfig;
    public McWssConfig McWssConfig => _properties.McWssConfig;
    public McHttpConfig McHttpConfig => _properties.McHttpConfig;
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
}

public class ServerPropertiesStructure
{
    public ServerPropertiesStructure()
    {
        DefaultAudioEffectsConfig.Add(1, JsonSerializer.SerializeToElement(new VisibilityEffect()));
        DefaultAudioEffectsConfig.Add(2, JsonSerializer.SerializeToElement(new ProximityEffect { MaxRange = 30 }));
        DefaultAudioEffectsConfig.Add(4, JsonSerializer.SerializeToElement(new ProximityEchoEffect { Range = 30 }));
        DefaultAudioEffectsConfig.Add(8, JsonSerializer.SerializeToElement(new ProximityMuffleEffect()));
    }

    public VoiceCraftConfig VoiceCraftConfig { get; set; } = new();
    public McWssConfig McWssConfig { get; set; } = new();
    public McHttpConfig McHttpConfig { get; set; } = new();
    public Dictionary<ushort, JsonElement> DefaultAudioEffectsConfig { get; set; } = [];
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ServerPropertiesStructure), GenerationMode = JsonSourceGenerationMode.Metadata)]
public partial class ServerPropertiesStructureGenerationContext : JsonSerializerContext;