using VoiceCraft.Maui.ViewModels;

namespace VoiceCraft.Maui.Views.Desktop;

public partial class ServerDetails : ContentPage
{
	public ServerDetails(ServerDetailsViewModel viewModel)
	{
		InitializeComponent();
        BindingContext = viewModel;
	}
}