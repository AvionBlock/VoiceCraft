using System.Text.Json;
using System.Text.Json.Serialization;
using Spectre.Console;
using VoiceCraft.Server.Config;

namespace VoiceCraft.Server
{
    public class ServerProperties
    {
        private const string FileName = "ServerProperties.json";
        private const string ConfigPath = "config";

        public VoiceCraftConfig VoiceCraftConfig => _properties.VoiceCraftConfig;
        public McLinkConfig McLinkConfig => _properties.McLinkConfig;

        private ServerPropertiesStructure _properties = new();

        public void Load()
        {
            var files = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, FileName, SearchOption.AllDirectories);
            if (files.Length == 0)
            {
                AnsiConsole.MarkupLine("[yellow]" + Locales.Locales.ServerProperties_LoadFile_NotFound + "[/]");
                _properties = CreateConfigFile();
                return;
            }

            var file = files[0];
            _properties = LoadFile(file);
        }

        private static ServerPropertiesStructure LoadFile(string path)
        {
            try
            {
                AnsiConsole.MarkupLine($"[yellow]{string.Format(Locales.Locales.ServerProperties_LoadFile_Loading, path)}[/]");
                var text = File.ReadAllText(path);
                var properties = JsonSerializer.Deserialize<ServerPropertiesStructure>(text, ServerPropertiesStructureGenerationContext.Default.ServerPropertiesStructure);
                if (properties == null)
                    throw new Exception(Locales.Locales.ServerProperties_LoadFile_JSONFailed);
                return properties;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[yellow]{string.Format(Locales.Locales.ServerProperties_LoadFile_Failed, ex.Message)}[/]");
            }

            return new ServerPropertiesStructure();
        }

        private static ServerPropertiesStructure CreateConfigFile()
        {
            var properties = new ServerPropertiesStructure();
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigPath);
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigPath, FileName);
            AnsiConsole.MarkupLine($"[yellow]{string.Format(Locales.Locales.ServerProperties_CreateFile_Generating, path)}[/]");
            try
            {
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                File.WriteAllText(filePath, JsonSerializer.Serialize(properties, ServerPropertiesStructureGenerationContext.Default.ServerPropertiesStructure));
                AnsiConsole.MarkupLine($"[green]{string.Format(Locales.Locales.ServerProperties_CreateFile_Success, path)}[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]{string.Format(Locales.Locales.ServerProperties_CreateFile_Failed, path, ex.Message)}[/]");
            }

            return properties;
        }
    }

    public class ServerPropertiesStructure
    {
        public VoiceCraftConfig VoiceCraftConfig { get; set; } = new();
        public McLinkConfig McLinkConfig { get; set; } = new();
    }

    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(ServerPropertiesStructure), GenerationMode = JsonSourceGenerationMode.Metadata)]
    public partial class ServerPropertiesStructureGenerationContext : JsonSerializerContext;
}