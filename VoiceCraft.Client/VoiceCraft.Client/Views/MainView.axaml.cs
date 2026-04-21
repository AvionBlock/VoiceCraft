using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using VoiceCraft.Client.ViewModels;

namespace VoiceCraft.Client.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }
    
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        var insetsManager = TopLevel.GetTopLevel(this)?.InsetsManager;

        if (insetsManager == null) return;
        insetsManager.DisplayEdgeToEdgePreference = true;
        insetsManager.IsSystemBarVisible = true;
    }
    
    private void ModalBackgroundOnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var viewModel = (MainViewModel?)DataContext;
        if (viewModel == null) return;
        if (sender is not Border border || (border.Child?.IsPointerOver ?? false)) return;
        viewModel.PopModal();
    }
}