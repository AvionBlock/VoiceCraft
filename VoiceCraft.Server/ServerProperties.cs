using System.Text.Json;
using Spectre.Console;
using VoiceCraft.Server.Config;

namespace VoiceCraft.Server
{
    public class ServerProperties
    {
        private const string FileName = "ServerProperties.json";
        private const string ConfigPath = "config";

        public VoiceCraftConfig VoiceCraftConfig { get; set; } = new();
        public McConfig McConfig { get; set; } = new();

        public static ServerProperties Load()
        {
            var files = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, FileName, SearchOption.AllDirectories);
            if (files.Length == 0)
            {
                AnsiConsole.MarkupLine(
                    "[yellow]" + Locales.Locales.ServerProperties_LoadFile_NotFound + "[/]");
                return CreateConfigFile();
            }

            var file = files[0];
            return LoadFile(file);
        }

        private static ServerProperties LoadFile(string path)
        {
            try
            {
                AnsiConsole.MarkupLine($"[yellow]{string.Format(Locales.Locales.ServerProperties_LoadFile_Loading, path)}[/]");
                var text = File.ReadAllText(path);
                var properties = JsonSerializer.Deserialize<ServerProperties>(text);
                if (properties == null)
                    throw new Exception(Locales.Locales.ServerProperties_LoadFile_JSONFailed);
                return properties;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[yellow]{string.Format(Locales.Locales.ServerProperties_LoadFile_Failed, ex.Message)}[/]");
            }

            return new ServerProperties();
        }

        private static ServerProperties CreateConfigFile()
        {
            var properties = new ServerProperties();
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigPath);
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigPath, FileName);
            AnsiConsole.MarkupLine($"[yellow]{string.Format(Locales.Locales.ServerProperties_CreateFile_Generating, path)}[/]");
            try
            {
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                File.WriteAllText(filePath, JsonSerializer.Serialize(properties, JsonSerializerOptions.Web));
                AnsiConsole.MarkupLine($"[green]{string.Format(Locales.Locales.ServerProperties_CreateFile_Success, path)}[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]{string.Format(Locales.Locales.ServerProperties_CreateFile_Failed, path, ex.Message)}[/]");
            }

            return properties;
        }
    }
}