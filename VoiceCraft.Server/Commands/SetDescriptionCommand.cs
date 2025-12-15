using System.CommandLine;
using VoiceCraft.Core;
using VoiceCraft.Core.Locales;
using VoiceCraft.Core.Network.VcPackets.Request;
using VoiceCraft.Core.World;
using VoiceCraft.Server.Servers;

namespace VoiceCraft.Server.Commands;

public class SetDescriptionCommand : Command
{
    public SetDescriptionCommand(VoiceCraftServer server) : base(
        Localizer.Get("Commands.SetDescription.Name"),
        Localizer.Get("Commands.SetDescription.Description"))
    {
        var idArgument = new Argument<int>(
            Localizer.Get("Commands.SetDescription.Arguments.Id.Name"),
            Localizer.Get("Commands.SetDescription.Arguments.Id.Description"));
        var valueArgument = new Argument<string?>(
            Localizer.Get("Commands.SetDescription.Arguments.Value.Name"),
            Localizer.Get("Commands.SetDescription.Arguments.Value.Description"));
        AddArgument(idArgument);
        AddArgument(valueArgument);

        this.SetHandler((id, value) =>
        {
            var entity = server.World.GetEntity(id);
            if (entity is null)
                throw new Exception(Localizer.Get($"Commands.Exceptions.EntityNotFound:{id}"));
            if (entity is not VoiceCraftNetworkEntity networkEntity)
                throw new Exception(Localizer.Get($"Commands.Exceptions.EntityNotAClient:{id}"));
            
            server.SendPacket(networkEntity.NetPeer,
                PacketPool<VcSetDescriptionRequestPacket>.GetPacket()
                    .Set(string.IsNullOrWhiteSpace(value) ? "" : value));
        }, idArgument, valueArgument);
    }
}