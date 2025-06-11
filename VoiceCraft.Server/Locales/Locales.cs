using Jeek.Avalonia.Localization;

// ReSharper disable InconsistentNaming

namespace VoiceCraft.Server.Locales;

public static class Locales
{
    public static string Startup_Starting => Localizer.Get("Startup.Starting");
    public static string Startup_Success => Localizer.Get("Startup.Success");
    public static string Startup_Failed => Localizer.Get("Startup.Failed");
    
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
    public static string Commands_Exceptions_CannotFindEntity => Localizer.Get("Commands.Exceptions.CannotFindEntity");
    public static string Commands_Exceptions_EntityNotAClient => Localizer.Get("Commands.Exceptions.EntityNotAClient");
    public static string Commands_List_Showing => Localizer.Get("Commands.List.Showing");
    public static string Commands_List_Exceptions_LimitArgument => Localizer.Get("Commands.List.Exceptions.Limit");
    
    public static string AudioEffectSystem_Exceptions_AddEffect => Localizer.Get("AudioEffectSystem.AddEffect");
    public static string AudioEffectSystem_Exceptions_RemoveEffect => Localizer.Get("AudioEffectSystem.RemoveEffect");
    public static string AudioEffectSystem_Exceptions_AvailableId => Localizer.Get("AudioEffectSystem.AvailableId");
}