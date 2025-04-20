using Jeek.Avalonia.Localization;
// ReSharper disable InconsistentNaming

namespace VoiceCraft.Client.Locales
{
    public static class Locales
    {
        public static string Home_Credits => Localizer.Get("Home.Credits");
        public static string Home_Servers => Localizer.Get("Home.Servers");
        public static string Home_Settings => Localizer.Get("Home.Settings");
        public static string Home_CrashLogs => Localizer.Get("Home.CrashLogs");
  
        public static string AddServer_AddServer => Localizer.Get("AddServer.AddServer");
        public static string AddServer_IP => Localizer.Get("AddServer.IP");
        public static string AddServer_Name => Localizer.Get("AddServer.Name");
        public static string AddServer_Port => Localizer.Get("AddServer.Port");
  
        public static string Credits_AppVersion => Localizer.Get("Credits.AppVersion");
        public static string Credits_Author => Localizer.Get("Credits.Author");
        public static string Credits_Codec => Localizer.Get("Credits.Codec");
        public static string Credits_Contributors => Localizer.Get("Credits.Contributors");
        public static string Credits_Version => Localizer.Get("Credits.Version");
  
        public static string Settings_General => Localizer.Get("Settings.General");
        public static string Settings_General_BackgroundImage => Localizer.Get("Settings.General.BackgroundImage");
        public static string Settings_General_DisableNotifications => Localizer.Get("Settings.General.DisableNotifications");
        public static string Settings_General_HideServerAddresses => Localizer.Get("Settings.General.HideServerAddresses");
        public static string Settings_General_Language => Localizer.Get("Settings.General.Language");
        public static string Settings_General_NotificationDismiss => Localizer.Get("Settings.General.NotificationDismiss");
        public static string Settings_General_Theme => Localizer.Get("Settings.General.Theme");
  
        public static string Settings_Audio => Localizer.Get("Settings.Audio");
        public static string Settings_Audio_AutomaticGainControllers => Localizer.Get("Settings.Audio.AutomaticGainControllers");
        public static string Settings_Audio_Denoisers => Localizer.Get("Settings.Audio.Denoisers");
        public static string Settings_Audio_EchoCancelers => Localizer.Get("Settings.Audio.EchoCancelers");
        public static string Settings_Audio_InputDevices => Localizer.Get("Settings.Audio.InputDevices");
        public static string Settings_Audio_MicrophoneSensitivity => Localizer.Get("Settings.Audio.MicrophoneSensitivity");
        public static string Settings_Audio_MicrophoneTest => Localizer.Get("Settings.Audio.MicrophoneTest");
        public static string Settings_Audio_MicrophoneTest_Test => Localizer.Get("Settings.Audio.MicrophoneTest.Test");
        public static string Settings_Audio_OutputDevices => Localizer.Get("Settings.Audio.OutputDevices");
        public static string Settings_Audio_TestOutput => Localizer.Get("Settings.Audio.TestOutput");
        
        public static string Settings_Advanced => Localizer.Get("Settings.Advanced");
        public static string Settings_Advanced_TriggerGc => Localizer.Get("Settings.Advanced.TriggerGc");
        
        public static string SelectedServer_PingInformation => Localizer.Get("SelectedServer.PingInformation");
        public static string SelectedServer_Latency => Localizer.Get("SelectedServer.Latency");
        public static string SelectedServer_ServerInfo => Localizer.Get("SelectedServer.ServerInfo");
        public static string SelectedServer_ServerInfo_Status => Localizer.Get("SelectedServer.ServerInfo.Status");
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
        
        public static string VoiceCraft_Status_Title => Localizer.Get("VoiceCraft.Status.Title");
        public static string VoiceCraft_Status_Initializing => Localizer.Get("VoiceCraft.Status.Initializing");
        public static string VoiceCraft_Status_Connecting => Localizer.Get("VoiceCraft.Status.Connecting");
        public static string VoiceCraft_Status_Connected => Localizer.Get("VoiceCraft.Status.Connected");
        public static string VoiceCraft_Status_Disconnected => Localizer.Get("VoiceCraft.Status.Disconnected");
    }
}