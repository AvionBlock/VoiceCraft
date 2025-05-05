using System.CommandLine;
using VoiceCraft.Core.Network.Packets;
using VoiceCraft.Server.Application;
using VoiceCraft.Server.Data;

namespace VoiceCraft.Server.Commands
{
    public class SetTitleCommand : Command
    {
        public SetTitleCommand(VoiceCraftServer server) : base(Locales.Locales.Commands_SetTitle_Name, Locales.Locales.Commands_SetTitle_Description)
        {
            var idArgument = new Argument<byte>(Locales.Locales.Commands_Options_id_Name, Locales.Locales.Commands_Options_id_Description);
            var titleArgument = new Argument<string>(Locales.Locales.Commands_SetTitle_Options_title_Name, Locales.Locales.Commands_SetTitle_Options_title_Description);
            AddArgument(idArgument);
            AddArgument(titleArgument);
            
            this.SetHandler((id, title) =>
            {
                var entity = server.World.GetEntity(id);
                if (entity is null)
                    throw new Exception(string.Format(Locales.Locales.Commands_Exceptions_CannotFindEntity, id));
                if (entity is not VoiceCraftNetworkEntity networkEntity)
                    throw new Exception(string.Format(Locales.Locales.Commands_SetTitle_Exceptions_NotAClientEntity, id));

                var packet = new SetTitlePacket(title);
                server.SendPacket(networkEntity.NetPeer, packet);
            }, idArgument, titleArgument);
        }
    }
}