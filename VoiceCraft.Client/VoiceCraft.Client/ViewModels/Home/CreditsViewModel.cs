using System;
using System.Collections.ObjectModel;
using System.Reflection;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using OpusSharp.Core;
using VoiceCraft.Client.Models;
using VoiceCraft.Client.Network;

namespace VoiceCraft.Client.ViewModels.Home;

public partial class CreditsViewModel : ViewModelBase
{
    private readonly Bitmap? _defaultIcon = LoadImage("avares://VoiceCraft.Client/Assets/vcLow.png");

    [ObservableProperty] private string _appVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "N.A.";

    [ObservableProperty] private string _opusVersion = OpusInfo.Version();

    [ObservableProperty] private Version _voicecraftVersion = VoiceCraftClient.Version;

    [ObservableProperty] private ObservableCollection<Contributor> _contributors;

    public CreditsViewModel()
    {
        _contributors =
        [
            new Contributor(
                "SineVector241",
                ["Credits.Roles.Author", "Credits.Roles.Programmer"],
                LoadImage("avares://VoiceCraft.Client/Assets/sinePlushie.png")),
            new Contributor(
                "Miniontoby",
                ["Credits.Roles.Translator", "Credits.Roles.Programmer"],
                LoadImage("avares://VoiceCraft.Client/Assets/minionToby.png")),
            new Contributor(
                "Unny",
                ["Credits.Roles.Translator"],
                _defaultIcon)
        ];
    }

    private static Bitmap? LoadImage(string path)
    {
        return AssetLoader.Exists(new Uri(path)) ? new Bitmap(AssetLoader.Open(new Uri(path))) : null;
    }
}