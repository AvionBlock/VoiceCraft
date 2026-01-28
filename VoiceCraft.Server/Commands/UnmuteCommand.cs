using System.CommandLine;
using VoiceCraft.Core.Locales;
using VoiceCraft.Core.World;
using VoiceCraft.Network.World;

namespace VoiceCraft.Server.Commands;

public class UnmuteCommand : Command
{
    public UnmuteCommand(VoiceCraftWorld world) : base(
        Localizer.Get("Commands.Unmute.Name"),
        Localizer.Get("Commands.Unmute.Description"))
    {
        var idArgument = new Argument<int>(Localizer.Get("Commands.Unmute.Arguments.Id.Name"))
        {
            Description = Localizer.Get("Commands.Unmute.Arguments.Id.Description")
        };
        Add(idArgument);

        SetAction(result =>
        {
            var id = result.GetRequiredValue(idArgument);

            var entity = world.GetEntity(id);
            switch (entity)
            {
                case null:
                    throw new Exception(Localizer.Get($"Commands.Exceptions.EntityNotFound:{id}"));
                case VoiceCraftNetworkEntity networkEntity:
                    networkEntity.ServerMuted = false;
                    return;
                default:
                    entity.Muted = false;
                    break;
            }
        });
    }
}