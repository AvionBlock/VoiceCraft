using System.CommandLine;

namespace VoiceCraft.Server.Commands;

public class StopCommand : Command
{
    public StopCommand() : base(
        Locales.Locales.Commands_Stop_Name,
        Locales.Locales.Commands_Stop_Description)
    {
        this.SetHandler(() => { App.Shutdown(); });
    }
}