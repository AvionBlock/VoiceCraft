using System.CommandLine;
using VoiceCraft.Core;
using VoiceCraft.Server.Servers;

namespace VoiceCraft.Server.Commands;

public class ClearPropertyCommand : Command
{
    public ClearPropertyCommand(VoiceCraftServer server) : base(
        Locales.Locales.Commands_ClearProperty_Name,
        Locales.Locales.Commands_ClearProperty_Description)
    {
        var idArgument = new Argument<int>(
            Locales.Locales.Commands_ClearProperty_Arguments_Id_Name,
            Locales.Locales.Commands_ClearProperty_Arguments_Id_Description);
        var keyArgument = new Argument<PropertyKey>(
            Locales.Locales.Commands_ClearProperty_Arguments_Key_Name, 
            Locales.Locales.Commands_ClearProperty_Arguments_Key_Description);
        AddArgument(idArgument);
        AddArgument(keyArgument);

        this.SetHandler((id, key) =>
            {
                var entity = server.World.GetEntity(id);
                if (entity is null)
                    throw new Exception(string.Format(Locales.Locales.Commands_Exceptions_CannotFindEntity, id));

                entity.SetProperty(key, null);
            },
            idArgument, keyArgument);
    }
}