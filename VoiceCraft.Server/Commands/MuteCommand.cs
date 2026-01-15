using System.CommandLine;
using VoiceCraft.Core.Locales;
using VoiceCraft.Core.World;
using VoiceCraft.Network.World;

namespace VoiceCraft.Server.Commands;

public class MuteCommand : Command
{
    public MuteCommand(VoiceCraftWorld world) : base(
        Localizer.Get("Commands.Mute.Name"),
        Localizer.Get("Commands.Mute.Description"))
    {
        var idArgument = new Argument<int>(Localizer.Get("Commands.Mute.Arguments.Id.Name"))
        {
            Description = Localizer.Get("Commands.Mute.Arguments.Id.Description")
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
                    networkEntity.ServerMuted = true;
                    return;
                default:
                    entity.Muted = true;
                    break;
            }
        });
    }
}