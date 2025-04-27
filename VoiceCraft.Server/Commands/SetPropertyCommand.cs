using System.CommandLine;
using VoiceCraft.Core;
using VoiceCraft.Server.Application;

namespace VoiceCraft.Server.Commands
{
    public class SetPropertyCommand : Command
    {
        public SetPropertyCommand(VoiceCraftServer server) : base("setproperty", "Set a property to a given entity.")
        {
            var idArgument = new Argument<byte>("id", "The entity Id.");
            var keyArgument = new Argument<PropertyKey>("key", "The property key.");
            var valueArgument = new Argument<int?>("value", "The property value.");
            AddArgument(idArgument);
            AddArgument(keyArgument);
            AddArgument(valueArgument);

            this.SetHandler((id, key, value) =>
                {
                    var entity = server.World.GetEntity(id);
                    if (entity == null)
                        throw new Exception($"Could not find entity with id: {id}");
                    
                    entity.SetProperty(key, value);
                },
                idArgument, keyArgument, valueArgument);
        }
    }
}