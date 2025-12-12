using System.CommandLine;
using VoiceCraft.Core.Locales;
using VoiceCraft.Server.Servers;

namespace VoiceCraft.Server.Commands;

public class SetNameCommand : Command
{
    public SetNameCommand(VoiceCraftServer server) : base(
        Locales.Locales.Commands_SetName_Name,
        Locales.Locales.Commands_SetName_Description)
    {
        var idArgument = new Argument<int>(
            Locales.Locales.Commands_SetName_Arguments_Id_Name,
            Locales.Locales.Commands_SetName_Arguments_Id_Description);
        var valueArgument = new Argument<string>(
            Locales.Locales.Commands_SetName_Arguments_Value_Name,
            Locales.Locales.Commands_SetName_Arguments_Value_Description);
        AddArgument(idArgument);
        AddArgument(valueArgument);

        this.SetHandler((id, value) =>
        {
            var entity = server.World.GetEntity(id);
            if (entity is null)
                throw new Exception(Localizer.Get($"Commands.Exceptions.EntityNotFound:{id}"));

            entity.Name = value;
        }, idArgument, valueArgument);
    }
}