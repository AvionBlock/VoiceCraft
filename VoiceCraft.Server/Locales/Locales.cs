using Jeek.Avalonia.Localization;
// ReSharper disable InconsistentNaming

namespace VoiceCraft.Server.Locales
{
    public static class Locales
    {
        public static string Startup_Title_Starting => Localizer.Get("Startup.Title.Starting");
        public static string Startup_ServerSetupTable_Server => Localizer.Get("Startup.ServerSetupTable.Server");
        public static string Startup_ServerSetupTable_Port => Localizer.Get("Startup.ServerSetupTable.Port");
        public static string Startup_ServerSetupTable_Protocol => Localizer.Get("Startup.ServerSetupTable.Protocol");
        public static string Startup_ServerProperties_Loading => Localizer.Get("Startup.ServerProperties.Loading");
        public static string Startup_ServerProperties_Success => Localizer.Get("Startup.ServerProperties.Success");
        public static string Startup_VoiceCraftServer_Starting => Localizer.Get("Startup.VoiceCraftServer.Starting");
        public static string Startup_VoiceCraftServer_Failed => Localizer.Get("Startup.VoiceCraftServer.Failed");
        public static string Startup_VoiceCraftServer_Success => Localizer.Get("Startup.VoiceCraftServer.Success");
        public static string Startup_Finished => Localizer.Get("Startup.Finished");
        public static string Startup_Exception => Localizer.Get("Startup.Exception");

        public static string Command_Exception => Localizer.Get("Command.Exception");

        public static string Shutdown_StartingIn => Localizer.Get("Shutdown.StartingIn");
        public static string Shutdown_Starting => Localizer.Get("Shutdown.Starting");
        public static string Shutdown_Success => Localizer.Get("Shutdown.Success");

        public static string ServerProperties_LoadFile_NotFound => Localizer.Get("ServerProperties.LoadFile.NotFound");
        public static string ServerProperties_LoadFile_Loading => Localizer.Get("ServerProperties.LoadFile.Loading");
        public static string ServerProperties_LoadFile_JSONFailed => Localizer.Get("ServerProperties.LoadFile.JSONFailed");
        public static string ServerProperties_LoadFile_Failed => Localizer.Get("ServerProperties.LoadFile.Failed");
        public static string ServerProperties_CreateFile_Generating => Localizer.Get("ServerProperties.CreateFile.Generating");
        public static string ServerProperties_CreateFile_Success => Localizer.Get("ServerProperties.CreateFile.Success");
        public static string ServerProperties_CreateFile_Failed => Localizer.Get("ServerProperties.CreateFile.Failed");


        public static string Commands_Exceptions_CannotFindEntity => Localizer.Get("Commands.Exceptions.CannotFindEntity");

        public static string Commands_Root_Description => Localizer.Get("Commands.Root.Description");
        public static string Commands_Options_id_Name => Localizer.Get("Commands.Options.id.Name");
        public static string Commands_Options_id_Description => Localizer.Get("Commands.Options.id.Description");

        public static string Commands_SetProperty_Name => Localizer.Get("Commands.SetProperty.Name");
        public static string Commands_SetProperty_Description => Localizer.Get("Commands.SetProperty.Description");
        public static string Commands_SetProperty_Options_key_Name => Localizer.Get("Commands.SetProperty.Options.key.Name");
        public static string Commands_SetProperty_Options_key_Description => Localizer.Get("Commands.SetProperty.Options.key.Description");
        public static string Commands_SetProperty_Options_value_Name => Localizer.Get("Commands.SetProperty.Options.value.Name");
        public static string Commands_SetProperty_Options_value_Description => Localizer.Get("Commands.SetProperty.Options.value.Description");

        public static string Commands_SetPosition_Name => Localizer.Get("Commands.SetPosition.Name");
        public static string Commands_SetPosition_Description => Localizer.Get("Commands.SetPosition.Description");
        public static string Commands_SetPosition_Options_x_Description => Localizer.Get("Commands.SetPosition.Options.x.Description");
        public static string Commands_SetPosition_Options_y_Description => Localizer.Get("Commands.SetPosition.Options.y.Description");
        public static string Commands_SetPosition_Options_z_Description => Localizer.Get("Commands.SetPosition.Options.z.Description");

        public static string Commands_SetWorldId_Name => Localizer.Get("Commands.SetWorldId.Name");
        public static string Commands_SetWorldId_Description => Localizer.Get("Commands.SetWorldId.Description");
        public static string Commands_SetWorldId_Options_world_Name => Localizer.Get("Commands.SetWorldId.Options.world.Name");
        public static string Commands_SetWorldId_Options_world_Description => Localizer.Get("Commands.SetWorldId.Options.world.Description");

        public static string Commands_List_Name => Localizer.Get("Commands.List.Name");
        public static string Commands_List_Description => Localizer.Get("Commands.List.Description");
        public static string Commands_List_Options_clientsOnly_Name => Localizer.Get("Commands.List.Options.clientsOnly.Name");
        public static string Commands_List_Options_clientsOnly_Description => Localizer.Get("Commands.List.Options.clientsOnly.Description");
        public static string Commands_List_Options_limit_Name => Localizer.Get("Commands.List.Options.limit.Name");
        public static string Commands_List_Options_limit_Description => Localizer.Get("Commands.List.Options.limit.Description");
        public static string Commands_List_Exceptions_Limit => Localizer.Get("Commands.List.Exceptions.Limit");
        public static string Commands_List_EntityTable_Id => Localizer.Get("Commands.List.EntityTable.Id");
        public static string Commands_List_EntityTable_Name => Localizer.Get("Commands.List.EntityTable.Name");
        public static string Commands_List_EntityTable_Position => Localizer.Get("Commands.List.EntityTable.Position");
        public static string Commands_List_EntityTable_Rotation => Localizer.Get("Commands.List.EntityTable.Rotation");
        public static string Commands_List_Showing => Localizer.Get("Commands.List.Showing");

        public static string Commands_SetTitle_Name => Localizer.Get("Commands.SetTitle.Name");
        public static string Commands_SetTitle_Description => Localizer.Get("Commands.SetTitle.Description");
        public static string Commands_SetTitle_Options_title_Name => Localizer.Get("Commands.SetTitle.Options.title.Name");
        public static string Commands_SetTitle_Options_title_Description => Localizer.Get("Commands.SetTitle.Options.title.Description");
        public static string Commands_SetTitle_Exceptions_NotAClientEntity => Localizer.Get("Commands.SetTitle.Exceptions.NotAClientEntity");


        public static string AudioEffectSystem_FailedToAddEffect => Localizer.Get("AudioEffectSystem.FailedToAddEffect");
        public static string AudioEffectSystem_FailedToRemoveEffect => Localizer.Get("AudioEffectSystem.FailedToRemoveEffect");
        public static string AudioEffectSystem_NoAvailableIdFound => Localizer.Get("AudioEffectSystem.NoAvailableIdFound");
    }
}