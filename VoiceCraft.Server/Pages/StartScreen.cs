using Spectre.Console;
using VoiceCraft.Server.Application;

namespace VoiceCraft.Server.Pages
{
    public class StartScreen(VoiceCraftServer server)
    {
        public async Task Start()
        {
            await AnsiConsole.Status()
                .StartAsync("Starting...", ctx =>
                {
                    //Startup.
                    Console.Title = $"VoiceCraft - {VoiceCraftServer.Version}: Starting...";
                    AnsiConsole.Write(new FigletText("VoiceCraft").Color(Color.Aqua));
                    
                    //Properties
                    ctx.Status("Loading Server Properties...");
                    var properties = ServerProperties.Load("testPath");
                    AnsiConsole.MarkupLine("[green]Successfully loaded server properties![/]");
                    
                    //Server Startup
                    ctx.Status("Starting Server...");
                    server.Properties = properties;
                    server.Start();
                    ctx.Status("Server Started");
                    
                    //Finish
                    AnsiConsole.MarkupLine("[green]VoiceCraft server started![/]");
                    return Task.CompletedTask;
                });
        }
    }
}