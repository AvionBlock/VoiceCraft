using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace VoiceCraft.Client.ViewModels.Data;

public partial class ContributorViewModel(string name, string[] roles, Bitmap? imageIcon = null) : ObservableObject
{
    [ObservableProperty] private Bitmap? _imageIcon = imageIcon;
    [ObservableProperty] private string _name = name;
    [ObservableProperty] private ObservableCollection<string> _roles = new(roles);
}