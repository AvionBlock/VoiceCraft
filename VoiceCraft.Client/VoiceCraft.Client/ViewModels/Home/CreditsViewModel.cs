using System;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Reflection;
using OpusSharp.Core;
using VoiceCraft.Client.Network;

namespace VoiceCraft.Client.ViewModels.Home
{
    public partial class CreditsViewModel : ViewModelBase
    {
        [ObservableProperty]
        private Version _voicecraftVersion = VoiceCraftClient.Version;

        [ObservableProperty]
        private string _appVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "N.A.";

        [ObservableProperty]
        private string _opusVersion = OpusInfo.Version();
    }
}