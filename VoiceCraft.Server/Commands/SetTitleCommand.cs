using System.CommandLine;
using VoiceCraft.Core;
using VoiceCraft.Core.Network.VcPackets.Request;
using VoiceCraft.Core.World;
using VoiceCraft.Server.Servers;

namespace VoiceCraft.Server.Commands;

public class SetTitleCommand : Command
{
    public SetTitleCommand(VoiceCraftServer server) : base(
        Locales.Locales.Commands_SetTitle_Name,
        Locales.Locales.Commands_SetTitle_Description)
    {
        var idArgument = new Argument<int>(
            Locales.Locales.Commands_SetTitle_Arguments_Id_Name,
            Locales.Locales.Commands_SetTitle_Arguments_Id_Description);
        var valueArgument = new Argument<string?>(
            Locales.Locales.Commands_SetTitle_Arguments_Value_Name,
            Locales.Locales.Commands_SetTitle_Arguments_Value_Description);
        AddArgument(idArgument);
        AddArgument(valueArgument);

        this.SetHandler((id, value) =>
        {
            var entity = server.World.GetEntity(id);
            if (entity is null)
                throw new Exception(Locales.Locales.Commands_Exceptions_EntityNotFound.Replace("{id}", id.ToString()));
            if (entity is not VoiceCraftNetworkEntity networkEntity)
                throw new Exception(
                    Locales.Locales.Commands_Exceptions_EntityNotAClient.Replace("{id}", id.ToString()));

            server.SendPacket(networkEntity.NetPeer,
                PacketPool<VcSetTitleRequestPacket>.GetPacket()
                    .Set(string.IsNullOrWhiteSpace(value) ? "" : value));
        }, idArgument, valueArgument);
    }
}