using System.Text.Json;
using System.Text.Json.Serialization;
using Spectre.Console;
using VoiceCraft.Core.Locales;
using VoiceCraft.Server.Config;

namespace VoiceCraft.Server;

public class ServerProperties
{
    private const string FileName = "ServerProperties.json";
    private const string ConfigPath = "config";

    private ServerPropertiesStructure _properties = new();

    public VoiceCraftConfig VoiceCraftConfig => _properties.VoiceCraftConfig;
    public McWssConfig McWssConfig => _properties.McWssConfig;
    public McHttpConfig McHttpConfig => _properties.McHttpConfig;

    public void Load()
    {
        var files = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, FileName, SearchOption.AllDirectories);
        if (files.Length == 0)
        {
            AnsiConsole.MarkupLine($"[yellow]{Localizer.Get("ServerProperties.NotFound")}[/]");
            _properties = CreateConfigFile();
            AnsiConsole.MarkupLine($"[green]{Localizer.Get("ServerProperties.Success")}[/]");
            return;
        }

        var file = files[0];
        _properties = LoadFile(file);
        AnsiConsole.MarkupLine($"[green]{Localizer.Get("ServerProperties.Success")}[/]");
    }

    private static ServerPropertiesStructure LoadFile(string path)
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
            AnsiConsole.MarkupLine(
                $"[yellow]{Localizer.Get($"ServerProperties.Failed:{ex.Message}")}[/]");
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
}

public class ServerPropertiesStructure
{
    public VoiceCraftConfig VoiceCraftConfig { get; set; } = new();
    public McWssConfig McWssConfig { get; set; } = new();
    public McHttpConfig McHttpConfig { get; set; } = new();
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ServerPropertiesStructure), GenerationMode = JsonSourceGenerationMode.Metadata)]
public partial class ServerPropertiesStructureGenerationContext : JsonSerializerContext;