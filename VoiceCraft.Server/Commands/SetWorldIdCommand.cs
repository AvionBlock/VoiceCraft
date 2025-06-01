using System.CommandLine;
using VoiceCraft.Server.Application;

namespace VoiceCraft.Server.Commands;

public class SetWorldIdCommand : Command
{
    public SetWorldIdCommand(VoiceCraftServer server) : base(Locales.Locales.Commands_SetWorldId_Name, Locales.Locales.Commands_SetWorldId_Description)
    {
        var idArgument = new Argument<byte>(Locales.Locales.Commands_Options_id_Name, Locales.Locales.Commands_Options_id_Description);
        var worldIdArgument = new Argument<string?>(Locales.Locales.Commands_SetWorldId_Options_world_Name,
            Locales.Locales.Commands_SetWorldId_Options_world_Description);
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