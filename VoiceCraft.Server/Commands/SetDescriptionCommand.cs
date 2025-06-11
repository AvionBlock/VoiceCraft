using System.CommandLine;
using VoiceCraft.Core.Network.Packets;
using VoiceCraft.Server.Data;
using VoiceCraft.Server.Servers;

namespace VoiceCraft.Server.Commands;

public class SetDescriptionCommand : Command
{
    public SetDescriptionCommand(VoiceCraftServer server) : base(
        Locales.Locales.Commands_SetDescription_Name,
        Locales.Locales.Commands_SetDescription_Description)
    {
        var idArgument = new Argument<int>(
            Locales.Locales.Commands_SetDescription_Arguments_Id_Name,
            Locales.Locales.Commands_SetDescription_Arguments_Id_Description);
        var valueArgument = new Argument<string?>(
            Locales.Locales.Commands_SetDescription_Arguments_Value_Name,
            Locales.Locales.Commands_SetDescription_Arguments_Value_Description);
        AddArgument(idArgument);
        AddArgument(valueArgument);

        this.SetHandler((id, value) =>
        {
            var entity = server.World.GetEntity(id);
            if (entity is null)
                throw new Exception(Locales.Locales.Commands_Exceptions_CannotFindEntity.Replace("{id}", id.ToString()));
            if (entity is not VoiceCraftNetworkEntity networkEntity)
                throw new Exception(Locales.Locales.Commands_Exceptions_EntityNotAClient.Replace("{id}", id.ToString()));

            var packet = new SetDescriptionPacket(string.IsNullOrWhiteSpace(value) ? "" : value);
            server.SendPacket(networkEntity.NetPeer, packet);
        }, idArgument, valueArgument);
    }
}