using System.CommandLine;
using VoiceCraft.Core.Network.Packets;
using VoiceCraft.Server.Data;
using VoiceCraft.Server.Servers;

namespace VoiceCraft.Server.Commands;

public class SetTitleCommand : Command
{
    public SetTitleCommand(VoiceCraftServer server) : base("setTitle", "Sets a title for a client entity.")
    {
        var idArgument = new Argument<int>("id", "The client's entity id.");
        var titleArgument = new Argument<string>("title", () => string.Empty, "The title to set.");
        AddArgument(idArgument);
        AddArgument(titleArgument);

        this.SetHandler((id, title) =>
        {
            var entity = server.World.GetEntity(id);
            if (entity is null)
                throw new Exception(Locales.Locales.Commands_Exceptions_CannotFindEntity.Replace("{id}", id.ToString()));
            if (entity is not VoiceCraftNetworkEntity networkEntity)
                throw new Exception(Locales.Locales.Commands_Exceptions_EntityNotAClient.Replace("{id}", id.ToString()));

            var packet = new SetTitlePacket(title);
            server.SendPacket(networkEntity.NetPeer, packet);
        }, idArgument, titleArgument);
    }
}