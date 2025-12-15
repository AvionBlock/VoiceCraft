using System.CommandLine;
using VoiceCraft.Core.Locales;
using VoiceCraft.Server.Servers;

namespace VoiceCraft.Server.Commands;

public class SetWorldIdCommand : Command
{
    public SetWorldIdCommand(VoiceCraftServer server) : base(
        Localizer.Get("Commands.SetWorldId.Name"),
        Localizer.Get("Commands.SetWorldId.Description"))
    {
        var idArgument = new Argument<int>(
            Localizer.Get("Commands.SetWorldId.Arguments.Id.Name"),
            Localizer.Get("Commands.SetWorldId.Arguments.Id.Description"));
        var valueArgument = new Argument<string?>(
            Localizer.Get("Commands.SetWorldId.Arguments.Value.Name"),
            Localizer.Get("Commands.SetWorldId.Arguments.Value.Description"));
        AddArgument(idArgument);
        AddArgument(valueArgument);

        this.SetHandler((id, value) =>
            {
                var entity = server.World.GetEntity(id);
                if (entity is null)
                    throw new Exception(Localizer.Get($"Commands.Exceptions.EntityNotFound:{id}"));

                entity.WorldId = value ?? string.Empty;
            },
            idArgument, valueArgument);
    }
}