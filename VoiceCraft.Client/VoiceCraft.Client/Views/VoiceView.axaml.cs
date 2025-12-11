using Avalonia.Controls;
using Avalonia.Input;
using VoiceCraft.Client.ViewModels;

namespace VoiceCraft.Client.Views;

public partial class VoiceView : UserControl
{
    public VoiceView()
    {
        InitializeComponent();
    }


    private void ModalBackgroundOnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var viewModel = (VoiceViewModel?)DataContext;
        if (viewModel == null) return;
        if (sender is not Border border || (border.Child?.IsPointerOver ?? false)) return;
        viewModel.SelectedEntity = null;
    }
}