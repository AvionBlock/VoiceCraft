using Avalonia.Controls;
using VoiceCraft.Client.ViewModels;
using VoiceCraft.Client.ViewModels.Data;

namespace VoiceCraft.Client.Views;

public partial class VoiceView : UserControl
{
    public VoiceView()
    {
        InitializeComponent();
    }

    private void Entities_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox listBox ||
            DataContext is not VoiceViewModel viewModel ||
            listBox.SelectedValue is not EntityDataViewModel entity ||
            !viewModel.OpenEntityCommand.CanExecute(entity)) return;

        listBox.SelectedItem = null;
        viewModel.OpenEntityCommand.Execute(entity);
        e.Handled = true;
    }
}