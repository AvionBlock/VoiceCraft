using System.CommandLine;
using System.Numerics;
using VoiceCraft.Server.Application;

namespace VoiceCraft.Server.Commands
{
    public class SetPositionCommand : Command
    {
        public SetPositionCommand(VoiceCraftServer server) : base("setposition", "Set the position to a given entity.")
        {
            var idArgument = new Argument<byte>("id", "The entity Id.");
            var xPosArgument = new Argument<float>("x", "The X position.");
            var yPosArgument = new Argument<float>("y", "The Y position.");
            var zPosArgument = new Argument<float>("z", "The Z position.");
            AddArgument(idArgument);
            AddArgument(yPosArgument);
            AddArgument(yPosArgument);
            AddArgument(zPosArgument);

            this.SetHandler((id, xPos, yPos, zPos) =>
                {
                    var entity = server.World.GetEntity(id);
                    if (entity == null)
                        throw new Exception($"Could not find entity with id: {id}");

                    entity.Position = new Vector3(xPos, yPos, zPos);
                },
                idArgument, xPosArgument, yPosArgument, zPosArgument);
        }
    }
}