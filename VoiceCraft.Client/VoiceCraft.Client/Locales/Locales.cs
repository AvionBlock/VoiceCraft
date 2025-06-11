using Jeek.Avalonia.Localization;

// ReSharper disable InconsistentNaming

namespace VoiceCraft.Client.Locales;

public static class Locales
{
    public static string SelectedServer_ServerInfo_Status_Status => Localizer.Get("SelectedServer.ServerInfo.Status.Status");
    public static string SelectedServer_ServerInfo_Status_Pinging => Localizer.Get("SelectedServer.ServerInfo.Status.Pinging");

    public static string Audio_Recorder_InitFailed => Localizer.Get("Audio.Recorder.InitFailed");
    public static string Audio_Recorder_Init => Localizer.Get("Audio.Recorder.Init");
    
    public static string Audio_Player_InitFailed => Localizer.Get("Audio.Recorder.InitFailed");
    public static string Audio_Player_Init => Localizer.Get("Audio.Player.Init");
    
    public static string Audio_AEC_InitFailed => Localizer.Get("Audio.AEC.InitFailed");
    public static string Audio_AEC_Init => Localizer.Get("Audio.AEC.Init");
    
    public static string Audio_AGC_InitFailed => Localizer.Get("Audio.AGC.InitFailed");
    public static string Audio_AGC_Init => Localizer.Get("Audio.AGC.Init");
    
    public static string Audio_DN_InitFailed => Localizer.Get("Audio.DN.InitFailed");
    public static string Audio_DN_Init => Localizer.Get("Audio.DN.Init");
    
    public static string VoiceCraft_Status_Initializing => Localizer.Get("VoiceCraft.Status.Initializing");
    public static string VoiceCraft_Status_Connecting => Localizer.Get("VoiceCraft.Status.Connecting");
    public static string VoiceCraft_Status_Connected => Localizer.Get("VoiceCraft.Status.Connected");
    public static string VoiceCraft_Status_Disconnected => Localizer.Get("VoiceCraft.Status.Disconnected");
}