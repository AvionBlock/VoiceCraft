using System.CommandLine;
using VoiceCraft.Core.Network.Packets;
using VoiceCraft.Server.Data;
using VoiceCraft.Server.Servers;

namespace VoiceCraft.Server.Commands;

public class SetDescriptionCommand : Command
{
    public SetDescriptionCommand(VoiceCraftServer server) : base("setDescription", "Sets a description for a client entity.")
    {
        var idArgument = new Argument<int>("id", "The client's entity id.");
        var descriptionArgument = new Argument<string>("description", () => string.Empty, "The description to set.");
        AddArgument(idArgument);
        AddArgument(descriptionArgument);

        this.SetHandler((id, description) =>
        {
            var entity = server.World.GetEntity(id);
            if (entity is null)
                throw new Exception(Locales.Locales.Commands_Exceptions_CannotFindEntity.Replace("{id}", id.ToString()));
            if (entity is not VoiceCraftNetworkEntity networkEntity)
                throw new Exception(Locales.Locales.Commands_Exceptions_EntityNotAClient.Replace("{id}", id.ToString()));

            var packet = new SetDescriptionPacket(description);
            server.SendPacket(networkEntity.NetPeer, packet);
        }, idArgument, descriptionArgument);
    }
}