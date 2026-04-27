using System;
using System.Collections.ObjectModel;
using System.Reflection;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using OpusSharp.Core;
using VoiceCraft.Client.ViewModels.Data;
using VoiceCraft.Core.Locales;
using VoiceCraft.Network.Clients;

namespace VoiceCraft.Client.ViewModels.Home;

public partial class CreditsViewModel : ViewModelBase
{
    //private readonly Bitmap? _defaultIcon = LoadImage("avares://VoiceCraft.Client/Assets/Contributors/vc.png");

    [ObservableProperty] public partial string AppVersion { get; set; } = string.Empty;
    [ObservableProperty] public partial string Codec { get; set; } = string.Empty;
    [ObservableProperty] public partial ObservableCollection<ContributorDataViewModel> Contributors { get; set; }
    [ObservableProperty] public partial string Version { get; set; } = string.Empty;

    public CreditsViewModel()
    {
        Contributors =
        [
            new ContributorDataViewModel(
                "SineVector241",
                ["Credits.Roles.Author", "Credits.Roles.Programmer"],
                LoadImage("avares://VoiceCraft.Client/Assets/Contributors/sinePlushie.png")),
            new ContributorDataViewModel(
                "Miniontoby",
                ["Credits.Roles.Translator", "Credits.Roles.Programmer"],
                LoadImage("avares://VoiceCraft.Client/Assets/Contributors/minionToby.png")),
            new ContributorDataViewModel(
                "Unny",
                ["Credits.Roles.Translator"],
                LoadImage("avares://VoiceCraft.Client/Assets/Contributors/unny.png")),
            new ContributorDataViewModel(
                "AlphaMSq",
                ["Credits.Roles.Translator", "Credits.Roles.Programmer"],
                LoadImage("avares://VoiceCraft.Client/Assets/Contributors/alphamsq.png"))
        ];

        Localizer.Instance.OnLanguageChanged += UpdateLocalizations;
        UpdateLocalizations();
    }

    private static Bitmap? LoadImage(string path)
    {
        return AssetLoader.Exists(new Uri(path)) ? new Bitmap(AssetLoader.Open(new Uri(path))) : null;
    }

    private void UpdateLocalizations(string language = "")
    {
        AppVersion =
            Localizer.Get(
                $"Credits.AppVersion:{Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "N.A."}");
        Version = Localizer.Get($"Credits.Version:{VoiceCraftClient.Version.ToString()}");
        Codec = Localizer.Get($"Credits.Codec:{GetCodecVersion()}");
    }

    private static string GetCodecVersion()
    {
        try
        {
            return OpusInfo.Version();
        }
        catch (DllNotFoundException)
        {
            return "N.A.";
        }
        catch (BadImageFormatException)
        {
            return "N.A.";
        }
        catch (TypeInitializationException)
        {
            return "N.A.";
        }
    }
}