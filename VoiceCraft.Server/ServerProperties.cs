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
                    "[yellow]ServerProperties.json was not found in the current and/or sub directories! Falling back to default server properties![/]");
                return CreateConfigFile();
            }

            var file = files[0];
            return LoadFile(file);
        }

        private static ServerProperties LoadFile(string path)
        {
            try
            {
                AnsiConsole.MarkupLine($"[yellow]Loading ServerProperties.json file at {path}...[/]");
                var text = File.ReadAllText(path);
                var properties = JsonSerializer.Deserialize<ServerProperties>(text);
                if (properties == null)
                    throw new Exception("JSON parsing failed.");
                return properties;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[yellow]Failed to load server properties! Falling back to default properties! Error: {ex.Message}[/]");
            }

            return new ServerProperties();
        }

        private static ServerProperties CreateConfigFile()
        {
            var properties = new ServerProperties();
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigPath);
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigPath, FileName);
            AnsiConsole.MarkupLine($"[yellow]Generating ServerProperties.json file at {path}...[/]");
            try
            {
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                File.WriteAllText(filePath, JsonSerializer.Serialize(properties, JsonSerializerOptions.Web));
                AnsiConsole.MarkupLine($"[green]Sucessfully generated ServerProperties.json file at {path}[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Failed to generate ServerProperties.json file at {path}! Error: {ex.Message}[/]");
            }

            return properties;
        }
    }
}