using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace VoiceCraft.Client.ViewModels.Data;

public partial class ContributorDataViewModel(string name, string[] roles, Bitmap? imageIcon = null) : ObservableObject
{
    [ObservableProperty] public partial Bitmap? ImageIcon { get; set; } = imageIcon;
    [ObservableProperty] public partial string Name { get; set; } = name;
    [ObservableProperty] public partial ObservableCollection<string> Roles { get; set; } = new(roles);
}