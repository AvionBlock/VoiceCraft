using System.CommandLine;
using VoiceCraft.Core.Locales;
using VoiceCraft.Core.World;

namespace VoiceCraft.Server.Commands;

public class SetWorldIdCommand : Command
{
    public SetWorldIdCommand(VoiceCraftWorld world) : base(
        Localizer.Get("Commands.SetWorldId.Name"),
        Localizer.Get("Commands.SetWorldId.Description"))
    {
        var idArgument = new Argument<int>(Localizer.Get("Commands.SetWorldId.Arguments.Id.Name"))
        {
            Description = Localizer.Get("Commands.SetWorldId.Arguments.Id.Description")
        };
        var valueArgument = new Argument<string?>(Localizer.Get("Commands.SetWorldId.Arguments.Value.Name"))
        {
            Description = Localizer.Get("Commands.SetWorldId.Arguments.Value.Description"),
            DefaultValueFactory = _ => null
        };
        Add(idArgument);
        Add(valueArgument);

        SetAction(result =>
        {
            var id = result.GetRequiredValue(idArgument);
            var value = result.GetRequiredValue(valueArgument);

            var entity = world.GetEntity(id);
            if (entity is null)
                throw new Exception(Localizer.Get($"Commands.Exceptions.EntityNotFound:{id}"));

            entity.WorldId = value ?? string.Empty;
        });
    }
}