using System.CommandLine;
using System.Numerics;
using VoiceCraft.Server.Application;

namespace VoiceCraft.Server.Commands;

public class SetPositionCommand : Command
{
    public SetPositionCommand(VoiceCraftServer server) : base(Locales.Locales.Commands_SetPosition_Name, Locales.Locales.Commands_SetPosition_Description)
    {
        var idArgument = new Argument<byte>(Locales.Locales.Commands_Options_id_Name, Locales.Locales.Commands_Options_id_Description);
        var xPosArgument = new Argument<float>("x", Locales.Locales.Commands_SetPosition_Options_x_Description);
        var yPosArgument = new Argument<float>("y", Locales.Locales.Commands_SetPosition_Options_y_Description);
        var zPosArgument = new Argument<float>("z", Locales.Locales.Commands_SetPosition_Options_z_Description);
        AddArgument(idArgument);
        AddArgument(xPosArgument);
        AddArgument(yPosArgument);
        AddArgument(zPosArgument);

        this.SetHandler((id, xPos, yPos, zPos) =>
            {
                var entity = server.World.GetEntity(id);
                if (entity is null)
                    throw new Exception(string.Format(Locales.Locales.Commands_Exceptions_CannotFindEntity, id));

                entity.Position = new Vector3(xPos, yPos, zPos);
            },
            idArgument, xPosArgument, yPosArgument, zPosArgument);
    }
}