using System.CommandLine;
using VoiceCraft.Core;
using VoiceCraft.Server.Servers;

namespace VoiceCraft.Server.Commands;

public class SetIntPropertyCommand : Command
{
    public SetIntPropertyCommand(VoiceCraftServer server) : base(
        Locales.Locales.Commands_SetIntProperty_Name,
        Locales.Locales.Commands_SetIntProperty_Description)
    {
        var idArgument = new Argument<int>(
            Locales.Locales.Commands_SetIntProperty_Arguments_Id_Name,
            Locales.Locales.Commands_SetIntProperty_Arguments_Id_Description);
        var keyArgument = new Argument<PropertyKey>(
            Locales.Locales.Commands_SetIntProperty_Arguments_Key_Name, 
            Locales.Locales.Commands_SetIntProperty_Arguments_Key_Description);
        var valueArgument = new Argument<int>(
            Locales.Locales.Commands_SetIntProperty_Arguments_Value_Name, 
            Locales.Locales.Commands_SetIntProperty_Arguments_Value_Description);
        AddArgument(idArgument);
        AddArgument(keyArgument);
        AddArgument(valueArgument);

        this.SetHandler((id, key, value) =>
            {
                var entity = server.World.GetEntity(id);
                if (entity is null)
                    throw new Exception(string.Format(Locales.Locales.Commands_Exceptions_CannotFindEntity, id));

                entity.SetProperty(key, value);
            },
            idArgument, keyArgument, valueArgument);
    }
}