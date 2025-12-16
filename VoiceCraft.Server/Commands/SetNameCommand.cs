using System.CommandLine;
using VoiceCraft.Core.Locales;
using VoiceCraft.Server.Servers;

namespace VoiceCraft.Server.Commands;

public class SetNameCommand : Command
{
    public SetNameCommand(VoiceCraftServer server) : base(
        Localizer.Get("Commands.SetName.Name"),
        Localizer.Get("Commands.SetName.Description"))
    {
        var idArgument = new Argument<int>(Localizer.Get("Commands.SetName.Arguments.Id.Name"))
        {
            Description = Localizer.Get("Commands.SetName.Arguments.Id.Description")
        };
        var valueArgument = new Argument<string>(Localizer.Get("Commands.SetName.Arguments.Value.Name"))
        {
            Description = Localizer.Get("Commands.SetName.Arguments.Value.Description")
        };
        Add(idArgument);
        Add(valueArgument);
        
        SetAction(result =>
        {
            var id = result.GetRequiredValue(idArgument);
            var value = result.GetRequiredValue(valueArgument);
            
            var entity = server.World.GetEntity(id);
            if (entity is null)
                throw new Exception(Localizer.Get($"Commands.Exceptions.EntityNotFound:{id}"));

            entity.Name = value;
        });
    }
}