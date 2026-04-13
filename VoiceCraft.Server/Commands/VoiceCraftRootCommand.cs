using System.CommandLine;

namespace VoiceCraft.Server.Commands;

public class VoiceCraftRootCommand : RootCommand
{
    public VoiceCraftRootCommand() : base("VoiceCraft application server root command.")
    {
        var exitOnInvalidPropertiesOption = new Option<bool>("--exit-on-invalid-properties", "-e")
        {
            Description = "Exits when the VoiceCraft server fails to parse the ServerProperties.json file.",
            DefaultValueFactory = _ => false
        };
        var languageOption = new Option<string>("--language", "-l")
        {
            Description = "The language to use when voicecraft starts. Overrides the ServerProperties.json file."
        };
        var transportModeOption = new Option<string[]>("--transport-mode", "-m")
        {
            Description = "Choose which Minecraft API transports to enable for this run, for example 'http,tcp' or 'wss'."
        };
        var transportHostOption = new Option<string>("--transport-host", "-th")
        {
            Description = "Set the host address used by the Minecraft API transports for this run."
        };
        var transportPortOption = new Option<int?>("--transport-port", "-p")
        {
            Description = "Set the port used by the Minecraft API transports for this run."
        };
        var serverKeyOption = new Option<string>("--server-key", "-k")
        {
            Description = "Set the shared server key used by Minecraft API clients to authenticate."
        };
        Add(exitOnInvalidPropertiesOption);
        Add(languageOption);
        Add(transportModeOption);
        Add(transportHostOption);
        Add(transportPortOption);
        Add(serverKeyOption);
        
        SetAction(async result =>
        {
            var exitOnInvalidProperties = result.GetValue(exitOnInvalidPropertiesOption);
            var language = result.GetValue(languageOption);
            var runtimeOverrides = new ServerRuntimeOverrides
            {
                TransportMode = result.GetValue(transportModeOption) ?? [],
                TransportHost = result.GetValue(transportHostOption),
                TransportPort = result.GetValue(transportPortOption),
                ServerKey = result.GetValue(serverKeyOption)
            };
            await App.Start(exitOnInvalidProperties, language, runtimeOverrides);
        });
    }
}
