using VoiceCraft.Maui.ViewModels;

namespace VoiceCraft.Maui.Views.Mobile;
public partial class Voice : ContentPage
{
    VoiceViewModel viewModel;
    public Voice(VoiceViewModel viewModel)
    {
        InitializeComponent();
        this.viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        viewModel.PageAppearingCommand.Execute(this);
        base.OnAppearing();
    }

    protected override void OnDisappearing()
    {
        viewModel.PageDisappearingCommand.Execute(this);
        base.OnAppearing();
    }

    protected override bool OnBackButtonPressed()
    {
        return true;
    }
}