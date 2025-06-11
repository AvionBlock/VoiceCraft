using System.CommandLine;
using VoiceCraft.Server.Servers;

namespace VoiceCraft.Server.Commands;

public class SetWorldIdCommand : Command
{
    public SetWorldIdCommand(VoiceCraftServer server) : base("setWorldId", "Sets a WorldId for an entity.")
    {
        var idArgument = new Argument<int>("id", "The entity's id.");
        var worldIdArgument = new Argument<string?>("worldId", "The WorldId to set.");
        AddArgument(idArgument);
        AddArgument(worldIdArgument);

        this.SetHandler((id, worldId) =>
            {
                var entity = server.World.GetEntity(id);
                if (entity is null)
                    throw new Exception(string.Format(Locales.Locales.Commands_Exceptions_CannotFindEntity, id));

                entity.WorldId = worldId ?? string.Empty;
            },
            idArgument, worldIdArgument);
    }
}