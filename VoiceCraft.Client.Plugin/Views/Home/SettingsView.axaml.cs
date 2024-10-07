using VoiceCraft.Client.PDK.ViewModels;
using VoiceCraft.Client.PDK.Views;
using VoiceCraft.Client.Plugin.ViewModels.Home;

namespace VoiceCraft.Client.Plugin.Views.Home
{
    public partial class SettingsView : ViewBase
    {
        public override ViewModelBase ViewModel => SettingsViewModel;

        public readonly SettingsViewModel SettingsViewModel;
        public SettingsView(SettingsViewModel viewModel)
        {
            InitializeComponent();

            DataContext = viewModel;
            SettingsViewModel = viewModel;
        }
    }
}