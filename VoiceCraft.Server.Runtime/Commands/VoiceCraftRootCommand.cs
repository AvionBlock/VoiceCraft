using System;
using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;

namespace VoiceCraft.Server.Runtime.Commands;

public class VoiceCraftRootCommand : RootCommand
{
    public VoiceCraftRootCommand(IServiceProvider serviceProvider) : base("VoiceCraft application server root command.")
    {
        var exitOnInvalidPropertiesOption = new Option<bool>("--exit-on-invalid-properties", "-eip")
        {
            Description = "Exits when the VoiceCraft server fails to parse the ServerProperties.json file.",
            DefaultValueFactory = _ => false
        };
        var disableCommands = new Option<bool>("--disable-commands", "-dc")
        {
            Description = "Disables runtime commands.",
            DefaultValueFactory = _ => false
        };
        var disableColor = new Option<bool>("--disable-color", "-d-clr")
        {
            Description = "Disables printing color to the console.",
            DefaultValueFactory = _ => false
        };
        var disableAnsi = new Option<bool>("--disable-ansi", "-da")
        {
            Description = "Disables printing VT/ANSI escape sequences.",
            DefaultValueFactory = _ => false
        };
        var failFast = new Option<bool>("--fail-fast", "-ff")
        {
            Description = "Fails faster by skipping the shutdown timeout and immediately throwing the error.",
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
        var voicePortOption = new Option<uint?>("--voice-port", "-vp")
        {
            Description = "Set the port used by the VoiceCraft server for this run.",
            DefaultValueFactory = _ => null
        };
        var serverKeyOption = new Option<string?>("--server-key", "-sk")
        {
            Description = "Set the shared server key used by Minecraft API clients to authenticate.",
            DefaultValueFactory = _ => null
        };
        Add(exitOnInvalidPropertiesOption);
        Add(disableCommands);
        Add(disableColor);
        Add(disableAnsi);
        Add(failFast);
        Add(languageOption);
        Add(transportModeOption);
        Add(transportHostOption);
        Add(transportPortOption);
        Add(voicePortOption);
        Add(serverKeyOption);
        
        SetAction(async result =>
        {
            var runtimeOptions = new RuntimeOptions
            {
                ExitOnInvalidProperties = result.GetValue(exitOnInvalidPropertiesOption),
                DisableCommands = result.GetValue(disableCommands),
                DisableColor = result.GetValue(disableColor),
                DisableAnsi = result.GetValue(disableAnsi),
                FailFast = result.GetValue(failFast),
                Language = result.GetValue(languageOption),
                TransportMode = result.GetValue(transportModeOption) ?? [],
                TransportHost = result.GetValue(transportHostOption),
                TransportPort = result.GetValue(transportPortOption),
                VoicePort = result.GetValue(voicePortOption),
                ServerKey = result.GetValue(serverKeyOption)
            };
            await serviceProvider.GetRequiredService<App>().StartAsync(runtimeOptions);
        });
    }
}
