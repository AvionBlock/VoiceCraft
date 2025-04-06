using System.CommandLine;
using VoiceCraft.Core;
using VoiceCraft.Core.Network.Packets;
using VoiceCraft.Server.Application;

namespace VoiceCraft.Server.Commands
{
    public class SetTitleCommand : Command
    {
        public SetTitleCommand(VoiceCraftServer server) : base("settitle", "Sets a title for a client.")
        {
            var idArgument = new Argument<int>("id", "The entity client Id.");
            var titleArgument = new Argument<string>("title", "The title to set.");
            AddArgument(idArgument);
            AddArgument(titleArgument);
            
            this.SetHandler((id, title) =>
            {
                if (!server.World.Entities.TryGetValue(id, out var entity))
                    throw new Exception($"Could not find entity with id: {id}");
                if(entity is not VoiceCraftNetworkEntity networkEntity)
                    throw new Exception($"Entity with id {id} is not a client entity!");

                var packet = new SetTitlePacket(title);
                server.NetworkSystem.SendPacket(networkEntity.NetPeer, packet);
            }, idArgument, titleArgument);
        }
    }
}