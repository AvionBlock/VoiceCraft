using System.CommandLine;
using VoiceCraft.Core.Network.Packets;
using VoiceCraft.Server.Application;
using VoiceCraft.Server.Data;

namespace VoiceCraft.Server.Commands
{
    public class SetTitleCommand : Command
    {
        public SetTitleCommand(VoiceCraftServer server) : base("settitle", "Sets a title for a client.")
        {
            var idArgument = new Argument<byte>("id", "The entity client Id.");
            var titleArgument = new Argument<string>("title", "The title to set.");
            AddArgument(idArgument);
            AddArgument(titleArgument);
            
            this.SetHandler((id, title) =>
            {
                var entity = server.World.GetEntity(id);
                if (entity == null)
                    throw new Exception($"Could not find entity with id: {id}");
                if(entity is not VoiceCraftNetworkEntity networkEntity)
                    throw new Exception($"Entity with id {id} is not a client entity!");

                var packet = new SetTitlePacket(title);
                server.SendPacket(networkEntity.NetPeer, packet);
            }, idArgument, titleArgument);
        }
    }
}