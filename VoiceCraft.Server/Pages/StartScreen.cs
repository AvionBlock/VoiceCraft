using Spectre.Console;
using VoiceCraft.Server.Application;

namespace VoiceCraft.Server.Pages
{
    public class StartScreen(VoiceCraftServer server)
    {
        public bool Start()
        {
            return AnsiConsole.Status().Start("Starting...", ctx =>
                {
                    try
                    {
                        //Startup.
                        Console.Title = $"VoiceCraft - {VoiceCraftServer.Version}: Starting...";
                        AnsiConsole.Write(new FigletText("VoiceCraft").Color(Color.Aqua));

                        //Properties
                        ctx.Status("Loading Server Properties...");
                        var properties = ServerProperties.Load().VoiceCraftConfig;
                        AnsiConsole.MarkupLine("[green]Successfully loaded server properties![/]");
                        //Server Startup
                        ctx.Status("Starting Server...");
                        server.Config = properties;
                        server.Start();

                        //Finish
                        AnsiConsole.MarkupLine("[green]VoiceCraft server started![/]");
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine("[red]An error occurred while trying to startup the server![/]");
                        AnsiConsole.WriteException(ex);
                        return false;
                    }
                    return true;
                });
        }
    }
}