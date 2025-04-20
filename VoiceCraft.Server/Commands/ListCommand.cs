using System.CommandLine;
using Spectre.Console;
using VoiceCraft.Server.Application;
using VoiceCraft.Server.Data;

namespace VoiceCraft.Server.Commands
{
    public class ListCommand : Command
    {
        public ListCommand(VoiceCraftServer server) : base("list", "Lists all entities.")
        {
            var clientsOnlyOption = new Option<bool>("--clientsOnly", () => false, "Show clients only.");
            var limitOption = new Option<int>("--limit", () => 10, "Limit the number of shown entities.");
            AddOption(clientsOnlyOption);
            AddOption(limitOption);

            this.SetHandler((clientsOnly, limit) =>
                {
                    if (limit < 0)
                        throw new ArgumentException("Limit cannot be less than zero!", nameof(limit));

                    var table = new Table()
                        .AddColumn("Id")
                        .AddColumn("Name")
                        .AddColumn("Position")
                        .AddColumn("Rotation");

                    var list = server.World.Entities;
                    if (clientsOnly)
                        list = list.OfType<VoiceCraftNetworkEntity>();

                    AnsiConsole.WriteLine($"Showing {limit} entities.");
                    foreach (var entity in list)
                    {
                        if (limit <= 0)
                            break;
                        limit--;
                        table.AddRow(
                            entity.Id.ToString(),
                            entity.Name,
                            $"[red]{entity.Position.X}[/], [green]{entity.Position.Y}[/], [blue]{entity.Position.Z}[/]",
                            $"[red]{entity.Rotation.X}[/], [green]{entity.Rotation.Y}[/], [blue]{entity.Rotation.Z}[/], [yellow]{entity.Rotation.W}[/]");
                    }

                    AnsiConsole.Write(table);
                },
                clientsOnlyOption, limitOption);
        }
    }
}