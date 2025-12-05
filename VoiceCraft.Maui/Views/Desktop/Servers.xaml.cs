using VoiceCraft.Maui.ViewModels;

namespace VoiceCraft.Maui.Views.Desktop;

public partial class Servers : ContentPage
{
    public Servers(ServersViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
