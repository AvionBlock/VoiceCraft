using System.CommandLine;

namespace VoiceCraft.Server.Commands;

public class VoiceCraftRootCommand : RootCommand
{
    public VoiceCraftRootCommand() : base("VoiceCraft application server root command.")
    {
        var exitOnInvalidPropertiesOption = new Option<bool>("--exit-on-invalid-properties")
        {
            Description = "Exits when the VoiceCraft server fails to parse the ServerProperties.json file.",
            DefaultValueFactory = _ => false
        };
        var languageOption = new Option<string>("--language")
        {
            Description = "The language to use when voicecraft starts. Overrides the ServerProperties.json file.",
            DefaultValueFactory = _ => "en-US"
        };
        Add(exitOnInvalidPropertiesOption);
        Add(languageOption);
        
        SetAction(async result =>
        {
            var exitOnInvalidProperties = result.GetValue(exitOnInvalidPropertiesOption);
            var language = result.GetValue(languageOption);
            await App.Start(exitOnInvalidProperties, language);
        });
    }
}