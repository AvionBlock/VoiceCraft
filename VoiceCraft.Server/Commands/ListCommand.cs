using System.CommandLine;
using Spectre.Console;
using VoiceCraft.Core.World;
using VoiceCraft.Server.Servers;

namespace VoiceCraft.Server.Commands;

public class ListCommand : Command
{
    public ListCommand(VoiceCraftServer server) : base(
        Locales.Locales.Commands_List_Name,
        Locales.Locales.Commands_List_Description)
    {
        var clientsOnlyOption = new Option<bool>(
            $"--{Locales.Locales.Commands_List_Options_ClientsOnly_Name}",
            () => false,
            Locales.Locales.Commands_List_Options_ClientsOnly_Description);
        var limitOption = new Option<int>(
            $"--{Locales.Locales.Commands_List_Options_Limit_Name}",
            () => 10,
            Locales.Locales.Commands_List_Options_Limit_Description);
        AddOption(clientsOnlyOption);
        AddOption(limitOption);

        this.SetHandler((clientsOnly, limit) =>
            {
                if (limit < 0)
                    throw new ArgumentOutOfRangeException(nameof(limit), Locales.Locales.Commands_List_Exceptions_LimitArgument);

                var table = new Table()
                    .AddColumn(Locales.Locales.Tables_ListCommandEntities_Id)
                    .AddColumn(Locales.Locales.Tables_ListCommandEntities_Name)
                    .AddColumn(Locales.Locales.Tables_ListCommandEntities_Position)
                    .AddColumn(Locales.Locales.Tables_ListCommandEntities_Rotation)
                    .AddColumn(Locales.Locales.Tables_ListCommandEntities_WorldId);

                var list = server.World.Entities;
                if (clientsOnly)
                    list = list.OfType<VoiceCraftNetworkEntity>();

                AnsiConsole.WriteLine(Locales.Locales.Commands_List_Showing.Replace("{limit}", limit.ToString()));
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
            },
            clientsOnlyOption, limitOption);
    }
}