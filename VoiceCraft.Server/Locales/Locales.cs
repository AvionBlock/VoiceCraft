using Jeek.Avalonia.Localization;

// ReSharper disable InconsistentNaming

namespace VoiceCraft.Server.Locales;

public static class Locales
{
    public static string Startup_Starting => Localizer.Get("Startup.Starting");
    public static string Startup_Success => Localizer.Get("Startup.Success");
    public static string Startup_Failed => Localizer.Get("Startup.Failed");
    public static string Startup_Commands_Registering => Localizer.Get("Startup.Commands.Registering");
    public static string Startup_Commands_Success => Localizer.Get("Startup.Commands.Success");
    
    public static string Shutdown_Starting => Localizer.Get("Shutdown.Starting");
    public static string Shutdown_StartingIn => Localizer.Get("Shutdown.StartingIn");
    public static string Shutdown_Success => Localizer.Get("Shutdown.Success");
    
    public static string ServerProperties_Loading => Localizer.Get("ServerProperties.Loading");
    public static string ServerProperties_Success => Localizer.Get("ServerProperties.Success");
    public static string ServerProperties_Failed => Localizer.Get("ServerProperties.Failed");
    public static string ServerProperties_NotFound => Localizer.Get("ServerProperties.NotFound");
    public static string ServerProperties_Generating_Generating => Localizer.Get("ServerProperties.Generating.Generating");
    public static string ServerProperties_Generating_Success => Localizer.Get("ServerProperties.Generating.Success");
    public static string ServerProperties_Generating_Failed => Localizer.Get("ServerProperties.Generating.Failed");
    public static string ServerProperties_Exceptions_ParseJson => Localizer.Get("ServerProperties.Exceptions.ParseJson");
    
    public static string Title_Starting => Localizer.Get("Title.Starting");
    
    public static string VoiceCraftServer_Starting => Localizer.Get("VoiceCraftServer.Starting");
    public static string VoiceCraftServer_Success => Localizer.Get("VoiceCraftServer.Success");
    public static string VoiceCraftServer_Exceptions_Failed => Localizer.Get("VoiceCraftServer.Exceptions.Failed");
    
    
    public static string Tables_ServerSetup_Server => Localizer.Get("Tables.ServerSetup.Server");
    public static string Tables_ServerSetup_Port => Localizer.Get("Tables.ServerSetup.Port");
    public static string Tables_ServerSetup_Protocol => Localizer.Get("Tables.ServerSetup.Protocol");
    public static string Tables_ListCommandEntities_Id => Localizer.Get("Tables.ListCommandEntities.Id");
    public static string Tables_ListCommandEntities_Name => Localizer.Get("Tables.ListCommandEntities.Name");
    public static string Tables_ListCommandEntities_Position => Localizer.Get("Tables.ListCommandEntities.Position");
    public static string Tables_ListCommandEntities_Rotation => Localizer.Get("Tables.ListCommandEntities.Rotation");
    public static string Tables_ListCommandEntities_WorldId => Localizer.Get("Tables.ListCommandEntities.WorldId");
    
    public static string Commands_Exception => Localizer.Get("Commands.Exception");
    public static string Commands_Exceptions_EntityNotFound => Localizer.Get("Commands.Exceptions.EntityNotFound");
    public static string Commands_Exceptions_EntityNotAClient => Localizer.Get("Commands.Exceptions.EntityNotAClient");

    public static string Commands_RootCommand_Description => Localizer.Get("Commands.RootCommand.Description");
    
    public static string Commands_List_Name => Localizer.Get("Commands.List.Name");
    public static string Commands_List_Description => Localizer.Get("Commands.List.Description");
    public static string Commands_List_Showing => Localizer.Get("Commands.List.Showing");
    public static string Commands_List_Options_ClientsOnly_Name => Localizer.Get("Commands.List.Options.ClientsOnly.Name");
    public static string Commands_List_Options_ClientsOnly_Description => Localizer.Get("Commands.List.Options.ClientsOnly.Description");
    public static string Commands_List_Options_Limit_Name => Localizer.Get("Commands.List.Options.Limit.Name");
    public static string Commands_List_Options_Limit_Description => Localizer.Get("Commands.List.Options.Limit.Description");
    public static string Commands_List_Exceptions_LimitArgument => Localizer.Get("Commands.List.Exceptions.Limit");
    
    public static string Commands_SetTitle_Name => Localizer.Get("Commands.SetTitle.Name");
    public static string Commands_SetTitle_Description => Localizer.Get("Commands.SetTitle.Description");
    public static string Commands_SetTitle_Arguments_Id_Name => Localizer.Get("Commands.SetTitle.Arguments.Id.Name");
    public static string Commands_SetTitle_Arguments_Id_Description => Localizer.Get("Commands.SetTitle.Arguments.Id.Description");
    public static string Commands_SetTitle_Arguments_Value_Name => Localizer.Get("Commands.SetTitle.Arguments.Value.Name");
    public static string Commands_SetTitle_Arguments_Value_Description => Localizer.Get("Commands.SetTitle.Arguments.Value.Description");
    
    public static string Commands_SetDescription_Name => Localizer.Get("Commands.SetDescription.Name");
    public static string Commands_SetDescription_Description => Localizer.Get("Commands.SetDescription.Description");
    public static string Commands_SetDescription_Arguments_Id_Name => Localizer.Get("Commands.SetDescription.Arguments.Id.Name");
    public static string Commands_SetDescription_Arguments_Id_Description => Localizer.Get("Commands.SetDescription.Arguments.Id.Description");
    public static string Commands_SetDescription_Arguments_Value_Name => Localizer.Get("Commands.SetDescription.Arguments.Value.Name");
    public static string Commands_SetDescription_Arguments_Value_Description => Localizer.Get("Commands.SetDescription.Arguments.Value.Description");
    
    public static string Commands_SetIntProperty_Name => Localizer.Get("Commands.SetIntProperty.Name");
    public static string Commands_SetIntProperty_Description => Localizer.Get("Commands.SetIntProperty.Description");
    public static string Commands_SetIntProperty_Arguments_Id_Name => Localizer.Get("Commands.SetIntProperty.Arguments.Id.Name");
    public static string Commands_SetIntProperty_Arguments_Id_Description => Localizer.Get("Commands.SetIntProperty.Arguments.Id.Description");
    public static string Commands_SetIntProperty_Arguments_Key_Name => Localizer.Get("Commands.SetIntProperty.Arguments.Key.Name");
    public static string Commands_SetIntProperty_Arguments_Key_Description => Localizer.Get("Commands.SetIntProperty.Arguments.Key.Description");
    public static string Commands_SetIntProperty_Arguments_Value_Name => Localizer.Get("Commands.SetIntProperty.Arguments.Value.Name");
    public static string Commands_SetIntProperty_Arguments_Value_Description => Localizer.Get("Commands.SetIntProperty.Arguments.Value.Description");
    
    public static string Commands_ClearProperty_Name => Localizer.Get("Commands.ClearProperty.Name");
    public static string Commands_ClearProperty_Description => Localizer.Get("Commands.ClearProperty.Description");
    public static string Commands_ClearProperty_Arguments_Id_Name => Localizer.Get("Commands.ClearProperty.Arguments.Id.Name");
    public static string Commands_ClearProperty_Arguments_Id_Description => Localizer.Get("Commands.ClearProperty.Arguments.Id.Description");
    public static string Commands_ClearProperty_Arguments_Key_Name => Localizer.Get("Commands.ClearProperty.Arguments.Key.Name");
    public static string Commands_ClearProperty_Arguments_Key_Description => Localizer.Get("Commands.ClearProperty.Arguments.Key.Description");
    
    public static string Commands_SetPosition_Name => Localizer.Get("Commands.SetPosition.Name");
    public static string Commands_SetPosition_Description => Localizer.Get("Commands.SetPosition.Description");
    public static string Commands_SetPosition_Arguments_Id_Name => Localizer.Get("Commands.SetPosition.Arguments.Id.Name");
    public static string Commands_SetPosition_Arguments_Id_Description => Localizer.Get("Commands.SetPosition.Arguments.Id.Description");
    public static string Commands_SetPosition_Arguments_X_Name => Localizer.Get("Commands.SetPosition.Arguments.X.Name");
    public static string Commands_SetPosition_Arguments_X_Description => Localizer.Get("Commands.SetPosition.Arguments.X.Description");
    public static string Commands_SetPosition_Arguments_Y_Name => Localizer.Get("Commands.SetPosition.Arguments.Y.Name");
    public static string Commands_SetPosition_Arguments_Y_Description => Localizer.Get("Commands.SetPosition.Arguments.Y.Description");
    public static string Commands_SetPosition_Arguments_Z_Name => Localizer.Get("Commands.SetPosition.Arguments.Z.Name");
    public static string Commands_SetPosition_Arguments_Z_Description => Localizer.Get("Commands.SetPosition.Arguments.Z.Description");
    
    public static string Commands_SetWorldId_Name => Localizer.Get("Commands.SetWorldId.Name");
    public static string Commands_SetWorldId_Description => Localizer.Get("Commands.SetWorldId.Description");
    public static string Commands_SetWorldId_Arguments_Id_Name => Localizer.Get("Commands.SetWorldId.Arguments.Id.Name");
    public static string Commands_SetWorldId_Arguments_Id_Description => Localizer.Get("Commands.SetWorldId.Arguments.Id.Description");
    public static string Commands_SetWorldId_Arguments_Value_Name => Localizer.Get("Commands.SetWorldId.Arguments.Value.Name");
    public static string Commands_SetWorldId_Arguments_Value_Description => Localizer.Get("Commands.SetWorldId.Arguments.Value.Description");
    
    public static string AudioEffectSystem_Exceptions_AddEffect => Localizer.Get("AudioEffectSystem.AddEffect");
    public static string AudioEffectSystem_Exceptions_RemoveEffect => Localizer.Get("AudioEffectSystem.RemoveEffect");
    public static string AudioEffectSystem_Exceptions_AvailableId => Localizer.Get("AudioEffectSystem.AvailableId");
}