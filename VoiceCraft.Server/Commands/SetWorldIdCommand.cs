using System.CommandLine;
using VoiceCraft.Server.Servers;

namespace VoiceCraft.Server.Commands;

public class SetWorldIdCommand : Command
{
    public SetWorldIdCommand(VoiceCraftServer server) : base(
        Locales.Locales.Commands_SetWorldId_Name,
        Locales.Locales.Commands_SetWorldId_Description)
    {
        var idArgument = new Argument<int>(
            Locales.Locales.Commands_SetWorldId_Arguments_Id_Name,
            Locales.Locales.Commands_SetWorldId_Arguments_Id_Description);
        var valueArgument = new Argument<string?>(
            Locales.Locales.Commands_SetWorldId_Arguments_Value_Name,
            Locales.Locales.Commands_SetWorldId_Arguments_Value_Description);
        AddArgument(idArgument);
        AddArgument(valueArgument);

        this.SetHandler((id, value) =>
            {
                var entity = server.World.GetEntity(id);
                if (entity is null)
                    throw new Exception(string.Format(Locales.Locales.Commands_Exceptions_CannotFindEntity, id));

                entity.WorldId = value ?? string.Empty;
            },
            idArgument, valueArgument);
    }
}