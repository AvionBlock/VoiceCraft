using System.CommandLine;
using Spectre.Console;
using VoiceCraft.Server.Application;
using VoiceCraft.Server.Data;

namespace VoiceCraft.Server.Commands;

public class ListCommand : Command
{
    public ListCommand(VoiceCraftServer server) : base(Locales.Locales.Commands_List_Name, Locales.Locales.Commands_List_Description)
    {
        var clientsOnlyOption = new Option<bool>("--" + Locales.Locales.Commands_List_Options_clientsOnly_Name, () => false,
            Locales.Locales.Commands_List_Options_clientsOnly_Name);
        var limitOption = new Option<int>("--" + Locales.Locales.Commands_List_Options_limit_Name, () => 10,
            Locales.Locales.Commands_List_Options_limit_Description);
        AddOption(clientsOnlyOption);
        AddOption(limitOption);

        this.SetHandler((clientsOnly, limit) =>
            {
                if (limit < 0)
                    throw new ArgumentException(Locales.Locales.Commands_List_Exceptions_Limit, nameof(limit));

                var table = new Table()
                    .AddColumn(Locales.Locales.Commands_List_EntityTable_Id)
                    .AddColumn(Locales.Locales.Commands_List_EntityTable_Name)
                    .AddColumn(Locales.Locales.Commands_List_EntityTable_Position)
                    .AddColumn(Locales.Locales.Commands_List_EntityTable_Rotation);

                var list = server.World.Entities;
                if (clientsOnly)
                    list = list.OfType<VoiceCraftNetworkEntity>();

                AnsiConsole.WriteLine(string.Format(Locales.Locales.Commands_List_Showing, limit));
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