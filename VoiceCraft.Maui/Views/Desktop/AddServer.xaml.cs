using VoiceCraft.Maui.ViewModels;

namespace VoiceCraft.Maui.Views.Desktop;

public partial class AddServer : ContentPage
{
    AddServerViewModel viewModel;
	public AddServer(AddServerViewModel viewModel)
	{
		InitializeComponent();
        this.viewModel = viewModel;
        BindingContext = viewModel;
	}

    private void PortEntryUnfocused(object sender, FocusEventArgs e)
    {
        if (sender is Entry)
        {
            var entry = (Entry)sender;
            var valid = int.TryParse(entry.Text, out var result);
            if (!valid)
            {
                var cleaned = new string(entry.Text.Where(char.IsDigit).ToArray());
                if (int.TryParse(cleaned, out var res))
                {
                    var clamped = Math.Clamp(res, 1025, 65535);
                    viewModel.Server.Port = clamped;
                    entry.Text = clamped.ToString();
                }
                else
                {
                    viewModel.Server.Port = 1025;
                }
            }
            else if (result > 65535 || result < 1025)
            {
                var clamped = Math.Clamp(result, 1025, 65535);
                viewModel.Server.Port = clamped;
            }
        }
    }

    private void KeyEntryUnfocused(object sender, FocusEventArgs e)
    {
        if (sender is Entry)
        {
            var entry = (Entry)sender;
            var valid = int.TryParse(entry.Text, out var result);
            if (!valid)
            {
                var cleaned = new string(entry.Text.Where(char.IsDigit).ToArray());
                if (int.TryParse(cleaned, out var res))
                {
                    var clamped = Math.Clamp(res, short.MinValue, short.MaxValue);
                    viewModel.Server.Key = (short)clamped;
                    entry.Text = clamped.ToString();
                }
                else
                {
                    viewModel.Server.Key = 0;
                }
            }
            else if (result > short.MaxValue || result < short.MinValue)
            {
                var clamped = Math.Clamp(result, short.MinValue, short.MaxValue);
                viewModel.Server.Key = (short)clamped;
            }
        }
    }
}