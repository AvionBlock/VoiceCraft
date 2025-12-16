using System.CommandLine;
using VoiceCraft.Core.Locales;

namespace VoiceCraft.Server.Commands;

public class StopCommand : Command
{
    public StopCommand() : base(
        Localizer.Get("Commands.Stop.Name"),
        Localizer.Get("Commands.Stop.Description"))
    {
        SetAction(_ => { App.Shutdown(); });
    }
}