using VoiceCraft.Maui.ViewModels;

namespace VoiceCraft.Maui.Views.Mobile;

public partial class Settings : ContentPage
{
    readonly SettingsViewModel viewModel;
    public Settings(SettingsViewModel viewModel)
    {
        InitializeComponent();
        this.viewModel = viewModel;
        BindingContext = viewModel;
    }

    private void ClientEntryUnfocused(object sender, FocusEventArgs e)
    {
        if (sender is Entry entry)
        {
            var valid = int.TryParse(entry.Text, out var result);
            if (!valid)
            {
                var cleaned = new string(entry.Text.Where(char.IsDigit).ToArray());
                if (int.TryParse(cleaned, out var res))
                {
                    var clamped = Math.Clamp(res, 1025, 65535);
                    viewModel.Settings.ClientPort = clamped;
                    entry.Text = clamped.ToString();
                }
                else
                {
                    viewModel.Settings.ClientPort = 1025;
                }
            }
            else if (result > 65535 || result < 1025)
            {
                var clamped = Math.Clamp(result, 1025, 65535);
                viewModel.Settings.ClientPort = clamped;
            }
        }
    }

    private void BufferEntryUnfocused(object sender, FocusEventArgs e)
    {
        if (sender is Entry entry)
        {
            var valid = int.TryParse(entry.Text, out var result);
            if (!valid)
            {
                var cleaned = new string(entry.Text.Where(char.IsDigit).ToArray());
                if (int.TryParse(cleaned, out var res))
                {
                    var clamped = Math.Clamp(res, 40, 2000);
                    viewModel.Settings.JitterBufferSize = clamped;
                    entry.Text = clamped.ToString();
                }
                else
                {
                    viewModel.Settings.JitterBufferSize = 80;
                }
            }
            else if (result > 2000 || result < 40)
            {
                var clamped = Math.Clamp(result, 40, 2000);
                viewModel.Settings.JitterBufferSize = clamped;
            }
        }
    }

    protected override void OnDisappearing()
    {
        viewModel.SaveSettingsCommand.Execute(this);
        base.OnDisappearing();
    }
}