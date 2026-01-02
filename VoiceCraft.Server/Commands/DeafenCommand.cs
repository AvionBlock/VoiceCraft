using System.CommandLine;
using VoiceCraft.Core.Locales;
using VoiceCraft.Core.World;
using VoiceCraft.Server.Servers;

namespace VoiceCraft.Server.Commands;

public class DeafenCommand : Command
{
    public DeafenCommand(VoiceCraftServer server) : base(
        Localizer.Get("Commands.Deafen.Name"),
        Localizer.Get("Commands.Deafen.Description"))
    {
        var idArgument = new Argument<int>(Localizer.Get("Commands.Deafen.Arguments.Id.Name"))
        {
            Description = Localizer.Get("Commands.Deafen.Arguments.Id.Description")
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
                    networkEntity.ServerDeafened = true;
                    return;
                default:
                    entity.Deafened = true;
                    break;
            }
        });
    }
}