using System.CommandLine;
using VoiceCraft.Core.Locales;
using VoiceCraft.Core.World;
using VoiceCraft.Network.World;

namespace VoiceCraft.Server.Commands;

public class KickCommand : Command
{
    public KickCommand(VoiceCraftWorld world) : base(
        Localizer.Get("Commands.Kick.Name"),
        Localizer.Get("Commands.Kick.Description"))
    {
        var idArgument = new Argument<int>(Localizer.Get("Commands.Kick.Arguments.Id.Name"))
        {
            Description = Localizer.Get("Commands.Kick.Arguments.Id.Description")
        };
        Add(idArgument);

        SetAction(result =>
        {
            var id = result.GetRequiredValue(idArgument);

            var entity = world.GetEntity(id);
            if (entity is null)
                throw new Exception(Localizer.Get($"Commands.Exceptions.EntityNotFound:{id}"));
            if (entity is not VoiceCraftNetworkEntity networkEntity)
                throw new Exception(Localizer.Get($"Commands.Exceptions.EntityNotAClient:{id}"));
            networkEntity.Destroy(); //Yes, This basically kicks the client.
        });
    }
}