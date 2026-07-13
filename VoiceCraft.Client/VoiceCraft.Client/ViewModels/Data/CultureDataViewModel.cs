using System;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;

namespace VoiceCraft.Client.ViewModels.Data;

public partial class CultureDataViewModel(string name, string culture, Bitmap? imageIcon = null)
    : ObservableObject, IDisposable
{
    private bool _disposed;

    [ObservableProperty] public partial string Name { get; set; } = name;
    [ObservableProperty] public partial string Culture { get; set; } = culture;
    [ObservableProperty] public partial Bitmap? ImageIcon { get; set; } = imageIcon;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        ImageIcon?.Dispose();
        ImageIcon = null;
        GC.SuppressFinalize(this);
    }
}
