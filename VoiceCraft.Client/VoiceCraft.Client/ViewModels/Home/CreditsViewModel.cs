using System;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using OpusSharp.Core;
using VoiceCraft.Client.Network;

namespace VoiceCraft.Client.ViewModels.Home;

public partial class CreditsViewModel : ViewModelBase
{
    [ObservableProperty] private string _appVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "N.A.";

    [ObservableProperty] private string _opusVersion = OpusInfo.Version();

    [ObservableProperty] private Version _voicecraftVersion = VoiceCraftClient.Version;
}