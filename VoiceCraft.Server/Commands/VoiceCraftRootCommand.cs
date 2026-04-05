using System.CommandLine;
using VoiceCraft.Core.Locales;

namespace VoiceCraft.Server.Commands;

public class VoiceCraftRootCommand : RootCommand
{
    public VoiceCraftRootCommand() : base(Localizer.Get("Commands.RootCommand.Name"))
    {
        var exitOnInvalidPropertiesOption = new Option<bool>("--exit-on-invalid-properties")
        {
            Description = Localizer.Get("Commands.RootCommand.Options.ExitOnInvalidProperties.Description"),
            DefaultValueFactory = _ => false
        };
        var languageOption = new Option<string>("--language")
        {
            Description = Localizer.Get("Commands.RootCommand.Options.Language.Description")
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
