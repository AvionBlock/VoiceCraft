using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Jeek.Avalonia.Localization;

namespace VoiceCraft.Client.ViewModels.Data;

public partial class ContributorViewModel : ObservableObject
{
    private readonly string[] _localeRoles;

    [ObservableProperty] private string _name;
    [ObservableProperty] private ObservableCollection<string> _roles = [];
    [ObservableProperty] private Bitmap? _imageIcon;

    public ContributorViewModel(string name, string[] roles, Bitmap? imageIcon = null)
    {
        _name = name;
        _localeRoles = roles;
        _imageIcon = imageIcon;
        Localizer.LanguageChanged += (_, _) => UpdateLocalizations();
        UpdateLocalizations();
    }

    private void UpdateLocalizations()
    {
        Roles.Clear();
        foreach (var role in _localeRoles)
        {
            Roles.Add(Localizer.Get(role));
        }
    }
}