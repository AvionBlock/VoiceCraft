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
        
        public static string SelectedServer_PingInformation => Localizer.Get("SelectedServer.PingInformation");
        public static string SelectedServer_Latency => Localizer.Get("SelectedServer.Latency");
        public static string SelectedServer_ServerInfo => Localizer.Get("SelectedServer.ServerInfo");
        public static string SelectedServer_ServerInfo_Status => Localizer.Get("SelectedServer.ServerInfo.Status");
        public static string SelectedServer_ServerInfo_Status_Pinging => Localizer.Get("SelectedServer.ServerInfo.Status.Pinging");

        public static string Android_AudioPlayer_Exception_ChannelMask => Localizer.Get("Android.AudioPlayer.Exception.ChannelMask");
        public static string Android_AudioPlayer_Exception_CreateAudioTrack => Localizer.Get("Android.AudioPlayer.Exception.CreateAudioTrack");
        public static string Android_AudioPlayer_Exception_Encoding => Localizer.Get("Android.AudioPlayer.Exception.Encoding");
        public static string Android_AudioPlayer_Exception_Format => Localizer.Get("Android.AudioPlayer.Exception.Format");
        public static string Android_AudioPlayer_Exception_Init => Localizer.Get("Android.AudioPlayer.Exception.Init");
        public static string Android_AudioPlayer_Exception_Reinit => Localizer.Get("Android.AudioPlayer.Exception.Reinit");
        public static string Android_AudioPlayer_Exception_Write => Localizer.Get("Android.AudioPlayer.Exception.Write");
        public static string Android_AudioRecorder_Exception_Capture => Localizer.Get("Android.AudioRecorder.Exception.Capture");
        public static string Android_NativeAEC_Exception_AndroidRecorder => Localizer.Get("Android.NativeAEC.Exception.AndroidRecorder");
        public static string Android_NativeAEC_Exception_Init => Localizer.Get("Android.NativeAEC.Exception.Init");
        public static string Android_NativeAGC_Exception_AndroidRecorder => Localizer.Get("Android.NativeAGC.Exception.AndroidRecorder");
        public static string Android_NativeAGC_Exception_Init => Localizer.Get("Android.NativeAGC.Exception.Init");
        public static string Android_NativeDN_Exception_AndroidRecorder => Localizer.Get("Android.NativeDN.Exception.AndroidRecorder");
        public static string Android_NativeDN_Exception_Init => Localizer.Get("Android.NativeDN.Exception.Init");
  
        public static string VoiceCraft_Status_Title => Localizer.Get("VoiceCraft.Status.Title");
        public static string VoiceCraft_Status_Initializing => Localizer.Get("VoiceCraft.Status.Initializing");
        public static string VoiceCraft_Status_Connecting => Localizer.Get("VoiceCraft.Status.Connecting");
        public static string VoiceCraft_Status_Connected => Localizer.Get("VoiceCraft.Status.Connected");
        public static string VoiceCraft_Status_Disconnected => Localizer.Get("VoiceCraft.Status.Disconnected");
    }
}