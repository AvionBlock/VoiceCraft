using Avalonia.Controls;
using Avalonia.Interactivity;

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
}