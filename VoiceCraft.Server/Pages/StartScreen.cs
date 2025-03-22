using Spectre.Console;
using VoiceCraft.Server.Application;

namespace VoiceCraft.Server.Pages
{
    public class StartScreen(VoiceCraftServer server)
    {
        public void Start()
        {
            try
            {
                //Startup.
                Console.Title = $"VoiceCraft - {VoiceCraftServer.Version}: Starting...";
                AnsiConsole.Write(new FigletText("VoiceCraft").Color(Color.Aqua));

                //Properties
                AnsiConsole.WriteLine("Loading Server Properties...");
                var properties = ServerProperties.Load().VoiceCraftConfig;
                AnsiConsole.MarkupLine("[green]Successfully loaded server properties![/]");
                //Server Startup
                AnsiConsole.WriteLine("Starting VoiceCraft Server...");
                server.Config = properties;
                if(!server.Start())
                    throw new Exception("Failed to start VoiceCraft Server! Please check if another process is using the same port!");
                
                //Finish
                AnsiConsole.MarkupLine("[bold green]VoiceCraft server started![/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine("[red]An error occurred while trying to startup the server![/]");
                AnsiConsole.WriteException(ex);
                App.Shutdown(10000);
            }
        }
    }
}