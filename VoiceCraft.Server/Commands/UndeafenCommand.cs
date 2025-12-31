using System.CommandLine;
using VoiceCraft.Core.Locales;
using VoiceCraft.Core.World;
using VoiceCraft.Server.Servers;

namespace VoiceCraft.Server.Commands;

public class UndeafenCommand : Command
{
    public UndeafenCommand(VoiceCraftServer server) : base(
        Localizer.Get("Commands.Undeafen.Name"),
        Localizer.Get("Commands.Undeafen.Description"))
    {
        var idArgument = new Argument<int>(Localizer.Get("Commands.Undeafen.Arguments.Id.Name"))
        {
            Description = Localizer.Get("Commands.Undeafen.Arguments.Id.Description")
        };
        Add(idArgument);

        SetAction(result =>
        {
            var id = result.GetRequiredValue(idArgument);

            var entity = server.World.GetEntity(id);
            switch (entity)
            {
                case null:
                    throw new Exception(Localizer.Get($"Commands.Exceptions.EntityNotFound:{id}"));
                case VoiceCraftNetworkEntity networkEntity:
                    networkEntity.ServerDeafened = false;
                    return;
                default:
                    entity.Deafened = false;
                    break;
            }
        });
    }
}