using System.CommandLine;
using VoiceCraft.Core;
using VoiceCraft.Server.Application;

namespace VoiceCraft.Server.Commands
{
    public class SetPropertyCommand : Command
    {
        public SetPropertyCommand(VoiceCraftServer server) : base(Locales.Locales.Commands_SetProperty_Name, Locales.Locales.Commands_SetProperty_Description)
        {
            var idArgument = new Argument<byte>(Locales.Locales.Commands_Options_id_Name, Locales.Locales.Commands_Options_id_Description);
            var keyArgument = new Argument<PropertyKey>(Locales.Locales.Commands_SetProperty_Options_key_Name, Locales.Locales.Commands_SetProperty_Options_key_Description);
            var valueArgument = new Argument<int?>(Locales.Locales.Commands_SetProperty_Options_value_Name, Locales.Locales.Commands_SetProperty_Options_value_Description);
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
}