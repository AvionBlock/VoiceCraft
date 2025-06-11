using System.CommandLine;
using VoiceCraft.Core;
using VoiceCraft.Server.Servers;

namespace VoiceCraft.Server.Commands;

public class SetIntPropertyCommand : Command
{
    public SetIntPropertyCommand(VoiceCraftServer server) : base("setIntProperty", "Sets an integer property for an entity.")
    {
        var idArgument = new Argument<int>("id", "The client's entity id.");
        var keyArgument = new Argument<PropertyKey>("key", "The property key to set.");
        var valueArgument = new Argument<int?>("value", "The property value to set.");
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