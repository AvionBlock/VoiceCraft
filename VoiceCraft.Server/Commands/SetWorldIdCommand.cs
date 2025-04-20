using System.CommandLine;
using VoiceCraft.Server.Application;

namespace VoiceCraft.Server.Commands
{
    public class SetWorldIdCommand : Command
    {
        public SetWorldIdCommand(VoiceCraftServer server) : base("setworldid", "Set the world ID to a given entity.")
        {
            var idArgument = new Argument<byte>("id", "The entity Id.");
            var worldIdArgument = new Argument<string?>("world", "The world ID to set.");
            AddArgument(idArgument);
            AddArgument(worldIdArgument);

            this.SetHandler((id, worldId) =>
                {
                    var entity = server.World.GetEntity(id);
                    if (entity == null)
                        throw new Exception($"Could not find entity with id: {id}");
                    
                    entity.WorldId = worldId ?? string.Empty;
                },
                idArgument, worldIdArgument);
        }
    }
}