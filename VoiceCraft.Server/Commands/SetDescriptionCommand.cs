using System.CommandLine;
using VoiceCraft.Core;
using VoiceCraft.Core.Locales;
using VoiceCraft.Core.World;
using VoiceCraft.Network.Packets.VcPackets.Request;
using VoiceCraft.Network.World;
using VoiceCraft.Server.Servers;

namespace VoiceCraft.Server.Commands;

public class SetDescriptionCommand : Command
{
    public SetDescriptionCommand(VoiceCraftServer server, VoiceCraftWorld world) : base(
        Localizer.Get("Commands.SetDescription.Name"),
        Localizer.Get("Commands.SetDescription.Description"))
    {
        var idArgument = new Argument<int>(Localizer.Get("Commands.SetDescription.Arguments.Id.Name"))
        {
            Description = Localizer.Get("Commands.SetDescription.Arguments.Id.Description")
        };
        var valueArgument = new Argument<string?>(Localizer.Get("Commands.SetDescription.Arguments.Value.Name"))
        {
            Description = Localizer.Get("Commands.SetDescription.Arguments.Value.Description"),
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
            if (entity is not VoiceCraftNetworkEntity networkEntity)
                throw new Exception(Localizer.Get($"Commands.Exceptions.EntityNotAClient:{id}"));

            server.SendPacket(networkEntity.NetPeer,
                PacketPool<VcSetDescriptionRequestPacket>.GetPacket()
                    .Set(string.IsNullOrWhiteSpace(value) ? "" : value));
        });
    }
}