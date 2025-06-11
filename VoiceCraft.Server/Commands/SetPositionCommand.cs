using System.CommandLine;
using System.Numerics;
using VoiceCraft.Server.Servers;

namespace VoiceCraft.Server.Commands;

public class SetPositionCommand : Command
{
    public SetPositionCommand(VoiceCraftServer server) : base("setPosition", "Sets a position for an entity.")
    {
        var idArgument = new Argument<int>("id", "The entity's id.");
        var xPosArgument = new Argument<float>("x", "The X position.");
        var yPosArgument = new Argument<float>("y", "The Y position.");
        var zPosArgument = new Argument<float>("z", "The Z position.");
        AddArgument(idArgument);
        AddArgument(xPosArgument);
        AddArgument(yPosArgument);
        AddArgument(zPosArgument);

        this.SetHandler((id, xPos, yPos, zPos) =>
            {
                var entity = server.World.GetEntity(id);
                if (entity is null)
                    throw new Exception(string.Format(Locales.Locales.Commands_Exceptions_CannotFindEntity, id));

                entity.Position = new Vector3(xPos, yPos, zPos);
            },
            idArgument, xPosArgument, yPosArgument, zPosArgument);
    }
}