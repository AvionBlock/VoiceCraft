using System.CommandLine;
using Spectre.Console;
using VoiceCraft.Server.Application;

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
                    if(limit < 0)
                        throw new ArgumentException("Limit cannot be less than zero!", nameof(limit));

                    var list = server.World.Entities.Where(x => !clientsOnly || x.Key >= 0).ToArray();
                    var total = list.Length;
                    
                    AnsiConsole.WriteLine($"Showing {Math.Min(limit, total)} out of {total} entities.");
                    foreach (var entity in list)
                    {
                        if (limit <= 0)
                            break;
                        limit--;
                        AnsiConsole.WriteLine($"{entity.Key}: Name: {entity.Value.Name}, Position: {entity.Value.Position}, Rotation: {entity.Value.Rotation}");
                    }
                },
                clientsOnlyOption, limitOption);
        }
    }
}