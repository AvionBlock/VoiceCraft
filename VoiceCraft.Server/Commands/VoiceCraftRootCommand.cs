using System.CommandLine;

namespace VoiceCraft.Server.Commands;

public class VoiceCraftRootCommand : RootCommand
{
    public VoiceCraftRootCommand() : base("VoiceCraft application server root command.")
    {
        var exitOnInvalidPropertiesOption = new Option<bool>("--exit-on-invalid-properties", "-eip")
        {
            Description = "Exits when the VoiceCraft server fails to parse the ServerProperties.json file.",
            DefaultValueFactory = _ => false
        };
        var languageOption = new Option<string?>("--language", "-l")
        {
            Description = "The language to use when voicecraft starts. Overrides the ServerProperties.json file.",
            DefaultValueFactory = _ => null
        };
        var transportModeOption = new Option<string[]>("--transport-mode", "-tm")
        {
            Description = "Choose which Minecraft API transports to enable for this run, for example http, tcp or wss.",
            DefaultValueFactory = _ => []
        };
        var transportHostOption = new Option<string?>("--transport-host", "-th")
        {
            Description = "Set the host address used by the Minecraft API transports for this run.",
            DefaultValueFactory = _ => null
        };
        var transportPortOption = new Option<int?>("--transport-port", "-tp")
        {
            Description = "Set the port used by the Minecraft API transports for this run.",
            DefaultValueFactory = _ => null
        };
        var serverKeyOption = new Option<string?>("--server-key", "-sk")
        {
            Description = "Set the shared server key used by Minecraft API clients to authenticate.",
            DefaultValueFactory = _ => null
        };
        Add(exitOnInvalidPropertiesOption);
        Add(languageOption);
        Add(transportModeOption);
        Add(transportHostOption);
        Add(transportPortOption);
        Add(serverKeyOption);
        
        SetAction(async result =>
        {
            var runtimeOptions = new RuntimeOptions
            {
                ExitOnInvalidProperties = result.GetValue(exitOnInvalidPropertiesOption),
                Language = result.GetValue(languageOption),
                TransportMode = result.GetValue(transportModeOption) ?? [],
                TransportHost = result.GetValue(transportHostOption),
                TransportPort = result.GetValue(transportPortOption),
                ServerKey = result.GetValue(serverKeyOption)
            };
            await App.Start(runtimeOptions);
        });
    }
}
