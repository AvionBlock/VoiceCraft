using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using VoiceCraft.Client.ViewModels.Data;
using VoiceCraft.Client.ViewModels.Home;

namespace VoiceCraft.Client.Views.Home;

public partial class ServersView : UserControl
{
    public ServersView()
    {
        InitializeComponent();
        AddHandler(PointerReleasedEvent, ServerItem_OnPointerReleased, RoutingStrategies.Bubble, true);
    }

    private void ServerItem_OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        var server = FindDataContext<ServerDataViewModel>(e.Source);
        if (server == null ||
            DataContext is not ServersViewModel viewModel ||
            IsFromButton(e.Source, sender as Control))
            return;

        if (!viewModel.OpenServerCommand.CanExecute(server)) return;
        viewModel.OpenServerCommand.Execute(server);
        e.Handled = true;
    }

    private T? FindDataContext<T>(object? source)
        where T : class
    {
        for (var current = source as Control; current != null && current != this;
             current = current.GetVisualParent() as Control)
        {
            if (current.DataContext is T dataContext)
                return dataContext;
        }

        return null;
    }

    private static bool IsFromButton(object? source, Control? boundary)
    {
        for (var current = source as Control; current != null && current != boundary;
             current = current.GetVisualParent() as Control)
        {
            if (current is Button)
                return true;
        }

        return false;
    }
}
