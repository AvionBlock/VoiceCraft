using System.CommandLine;
using VoiceCraft.Core.Locales;
using VoiceCraft.Core.World;
using VoiceCraft.Network;
using VoiceCraft.Network.Packets.VcPackets.Request;
using VoiceCraft.Network.Servers;
using VoiceCraft.Network.World;

namespace VoiceCraft.Server.Commands;

public class SetTitleCommand : Command
{
    public SetTitleCommand(VoiceCraftServer server, VoiceCraftWorld world) : base(
        Localizer.Get("Commands.SetTitle.Name"),
        Localizer.Get("Commands.SetTitle.Description"))
    {
        var idArgument = new Argument<int>(Localizer.Get("Commands.SetTitle.Arguments.Id.Name"))
        {
            Description = Localizer.Get("Commands.SetTitle.Arguments.Id.Description")
        };
        var valueArgument = new Argument<string?>(Localizer.Get("Commands.SetTitle.Arguments.Value.Name"))
        {
            Description = Localizer.Get("Commands.SetTitle.Arguments.Value.Description"),
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
                PacketPool<VcSetTitleRequestPacket>.GetPacket()
                    .Set(string.IsNullOrWhiteSpace(value) ? "" : value));
        });
    }
}