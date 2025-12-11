using System.CommandLine;
using System.Numerics;
using VoiceCraft.Server.Servers;

namespace VoiceCraft.Server.Commands;

public class SetPositionCommand : Command
{
    public SetPositionCommand(VoiceCraftServer server) : base(
        Locales.Locales.Commands_SetPosition_Name,
        Locales.Locales.Commands_SetPosition_Description)
    {
        var idArgument = new Argument<int>(
            Locales.Locales.Commands_SetPosition_Arguments_Id_Name,
            Locales.Locales.Commands_SetPosition_Arguments_Id_Description);
        var xPosArgument = new Argument<float>(
            Locales.Locales.Commands_SetPosition_Arguments_X_Name,
            Locales.Locales.Commands_SetPosition_Arguments_X_Description);
        var yPosArgument = new Argument<float>(
            Locales.Locales.Commands_SetPosition_Arguments_Y_Name,
            Locales.Locales.Commands_SetPosition_Arguments_Y_Description);
        var zPosArgument = new Argument<float>(
            Locales.Locales.Commands_SetPosition_Arguments_Z_Name,
            Locales.Locales.Commands_SetPosition_Arguments_Z_Description);
        AddArgument(idArgument);
        AddArgument(xPosArgument);
        AddArgument(yPosArgument);
        AddArgument(zPosArgument);

        this.SetHandler((id, xPos, yPos, zPos) =>
            {
                var entity = server.World.GetEntity(id);
                if (entity is null)
                    throw new Exception(
                        Locales.Locales.Commands_Exceptions_EntityNotFound.Replace("{id}", id.ToString()));

                entity.Position = new Vector3(xPos, yPos, zPos);
            },
            idArgument, xPosArgument, yPosArgument, zPosArgument);
    }
}