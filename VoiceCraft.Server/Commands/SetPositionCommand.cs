using System.CommandLine;
using System.Numerics;
using VoiceCraft.Core.Locales;
using VoiceCraft.Server.Servers;

namespace VoiceCraft.Server.Commands;

public class SetPositionCommand : Command
{
    public SetPositionCommand(VoiceCraftServer server) : base(
        Localizer.Get("Commands.SetPosition.Name"),
        Localizer.Get("Commands.SetPosition.Description"))
    {
        var idArgument = new Argument<int>(Localizer.Get("Commands.SetPosition.Arguments.Id.Name"))
        {
            Description = Localizer.Get("Commands.SetPosition.Arguments.Id.Description")
        };
        var xPosArgument = new Argument<float>(Localizer.Get("Commands.SetPosition.Arguments.X.Name"))
        {
            Description = Localizer.Get("Commands.SetPosition.Arguments.X.Description")
        };
        var yPosArgument = new Argument<float>(Localizer.Get("Commands.SetPosition.Arguments.Y.Name"))
        {
            Description = Localizer.Get("Commands.SetPosition.Arguments.Y.Description")
        };
        var zPosArgument = new Argument<float>(Localizer.Get("Commands.SetPosition.Arguments.Z.Name"))
        {
            Description = Localizer.Get("Commands.SetPosition.Arguments.Z.Description")
        };
        Add(idArgument);
        Add(xPosArgument);
        Add(yPosArgument);
        Add(zPosArgument);

        SetAction(result =>
        {
            var id = result.GetRequiredValue(idArgument);
            var xPos = result.GetRequiredValue(xPosArgument);
            var yPos = result.GetRequiredValue(yPosArgument);
            var zPos = result.GetRequiredValue(zPosArgument);

            var entity = server.World.GetEntity(id);
            if (entity is null)
                throw new Exception(Localizer.Get($"Commands.Exceptions.EntityNotFound:{id}"));

            entity.Position = new Vector3(xPos, yPos, zPos);
        });
    }
}