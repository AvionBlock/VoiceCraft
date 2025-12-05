using VoiceCraft.Maui.ViewModels;

namespace VoiceCraft.Maui.Views.Mobile;

public partial class Servers : ContentPage
{
    public Servers(ServersViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}