using System.CommandLine;
using VoiceCraft.Core.Locales;

namespace VoiceCraft.Server.Runtime.Commands;

public class StopCommand : Command
{
    public StopCommand(App app) : base(
        Localizer.Get("Commands.Stop.Name"),
        Localizer.Get("Commands.Stop.Description"))
    {
        Aliases.Add("shutdown");
        SetAction(__ => { _ = app.ShutdownAsync(); });
    }
}