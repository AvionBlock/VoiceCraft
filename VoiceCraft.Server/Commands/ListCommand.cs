using System.CommandLine;
using Spectre.Console;
using VoiceCraft.Core.Locales;
using VoiceCraft.Core.World;
using VoiceCraft.Server.Servers;

namespace VoiceCraft.Server.Commands;

public class ListCommand : Command
{
    public ListCommand(VoiceCraftServer server) : base(
        Localizer.Get("Commands.List.Name"),
        Localizer.Get("Commands.List.Description"))
    {
        var clientsOnlyOption = new Option<bool>($"--{Localizer.Get("Commands.List.Options.ClientsOnly.Name")}")
        {
            Description = Localizer.Get("Commands.List.Options.ClientsOnly.Description"),
            DefaultValueFactory = _ => false
        };
        var limitOption = new Option<int>($"--{Localizer.Get("Commands.List.Options.Limit.Name")}")
        {
            Description = Localizer.Get("Commands.List.Options.Limit.Description"),
            DefaultValueFactory = _ => 10
        };
        Add(clientsOnlyOption);
        Add(limitOption);

        SetAction(result =>
        {
            var clientsOnly = result.GetValue(clientsOnlyOption);
            var limit = result.GetValue(limitOption);

            if (limit < 0)
                throw new ArgumentOutOfRangeException(nameof(limit),
                    Localizer.Get("Commands.List.Exceptions.Limit"));

            var table = new Table()
                .AddColumn(Localizer.Get("Tables.ListCommandEntities.Id"))
                .AddColumn(Localizer.Get("Tables.ListCommandEntities.Name"))
                .AddColumn(Localizer.Get("Tables.ListCommandEntities.Position"))
                .AddColumn(Localizer.Get("Tables.ListCommandEntities.Rotation"))
                .AddColumn(Localizer.Get("Tables.ListCommandEntities.WorldId"));

            var list = server.World.Entities;
            if (clientsOnly)
                list = list.OfType<VoiceCraftNetworkEntity>();

            AnsiConsole.WriteLine(Localizer.Get($"Commands.List.Showing:{limit}"));
            foreach (var entity in list)
            {
                if (limit <= 0)
                    break;
                limit--;
                table.AddRow(
                    entity.Id.ToString(),
                    entity.Name,
                    $"[red]{entity.Position.X}[/], [green]{entity.Position.Y}[/], [blue]{entity.Position.Z}[/]",
                    $"[red]{entity.Rotation.X}[/], [green]{entity.Rotation.Y}[/]",
                    entity.WorldId);
            }

            AnsiConsole.Write(table);
        });
    }
}