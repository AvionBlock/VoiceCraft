using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace VoiceCraft.Client.ViewModels.Data;

public partial class CultureDataViewModel(string name, string culture, Bitmap? imageIcon = null) : ObservableObject
{
    [ObservableProperty] public partial string Name { get; set; } = name;
    [ObservableProperty] public partial string Culture { get; set; } = culture;
    [ObservableProperty] public partial Bitmap? ImageIcon { get; set; } = imageIcon;
}