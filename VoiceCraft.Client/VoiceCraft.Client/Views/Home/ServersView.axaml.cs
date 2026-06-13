using Avalonia.Controls;
using VoiceCraft.Client.ViewModels.Data;
using VoiceCraft.Client.ViewModels.Home;

namespace VoiceCraft.Client.Views.Home;

public partial class ServersView : UserControl
{
    public ServersView()
    {
        InitializeComponent();
    }

    private void Servers_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox listBox ||
            DataContext is not ServersViewModel viewModel ||
            listBox.SelectedValue is not ServerDataViewModel server ||
            !viewModel.OpenServerCommand.CanExecute(server)) return;
        
        listBox.SelectedItem = null;
        viewModel.OpenServerCommand.Execute(server);
        e.Handled = true;
    }
}