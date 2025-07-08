using System.Text.Json;
using System.Text.Json.Serialization;
using Spectre.Console;
using VoiceCraft.Server.Config;

namespace VoiceCraft.Server;

public class ServerProperties
{
    private const string FileName = "ServerProperties.json";
    private const string ConfigPath = "config";

    private ServerPropertiesStructure _properties = new();

    public VoiceCraftConfig VoiceCraftConfig => _properties.VoiceCraftConfig;
    public McWssConfig McWssConfig => _properties.McWssConfig;
    public McApiConfig McApiConfig => _properties.McApiConfig;

    public void Load()
    {
        var files = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, FileName, SearchOption.AllDirectories);
        if (files.Length == 0)
        {
            AnsiConsole.MarkupLine($"[yellow]{Locales.Locales.ServerProperties_NotFound}[/]");
            _properties = CreateConfigFile();
            AnsiConsole.MarkupLine($"[green]{Locales.Locales.ServerProperties_Success}[/]");
            return;
        }

        var file = files[0];
        _properties = LoadFile(file);
        AnsiConsole.MarkupLine($"[green]{Locales.Locales.ServerProperties_Success}[/]");
    }

    private static ServerPropertiesStructure LoadFile(string path)
    {
        try
        {
            AnsiConsole.MarkupLine($"[yellow]{Locales.Locales.ServerProperties_Loading.Replace("{path}", path)}[/]");
            var text = File.ReadAllText(path);
            var properties =
                JsonSerializer.Deserialize<ServerPropertiesStructure>(text, ServerPropertiesStructureGenerationContext.Default.ServerPropertiesStructure);
            if (properties == null)
                throw new Exception(Locales.Locales.ServerProperties_Exceptions_ParseJson);
            return properties;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow]{Locales.Locales.ServerProperties_Failed.Replace("{errorMessage}", ex.Message)}[/]");
        }

        return new ServerPropertiesStructure();
    }

    private static ServerPropertiesStructure CreateConfigFile()
    {
        var properties = new ServerPropertiesStructure();
        var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigPath);
        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigPath, FileName);
        AnsiConsole.MarkupLine($"[yellow]{Locales.Locales.ServerProperties_Generating_Generating.Replace("{path}", path)}[/]");
        try
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            File.WriteAllText(filePath, JsonSerializer.Serialize(properties, ServerPropertiesStructureGenerationContext.Default.ServerPropertiesStructure));
            AnsiConsole.MarkupLine($"[green]{Locales.Locales.ServerProperties_Generating_Success}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine(
                $"[red]{Locales.Locales.ServerProperties_Generating_Failed.Replace("{path}", path).Replace("{errorMessage}", ex.Message)}[/]");
        }

        return properties;
    }
}

public class ServerPropertiesStructure
{
    public VoiceCraftConfig VoiceCraftConfig { get; set; } = new();
    public McWssConfig McWssConfig { get; set; } = new();
    public McApiConfig McApiConfig { get; set; } = new();
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ServerPropertiesStructure), GenerationMode = JsonSourceGenerationMode.Metadata)]
public partial class ServerPropertiesStructureGenerationContext : JsonSerializerContext;