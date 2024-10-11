using Avalonia.Controls;
using VoiceCraft.Client.PDK.ViewModels;
using VoiceCraft.Client.PDK.Views;
using VoiceCraft.Client.Plugin.ViewModels.Home;

namespace VoiceCraft.Client.Plugin.Views.Home
{
    public partial class PluginsView : ViewBase
    {
        public override ViewModelBase ViewModel => PluginsViewModel;

        public readonly PluginsViewModel PluginsViewModel;

        public PluginsView(PluginsViewModel viewModel)
        {
            InitializeComponent();

            DataContext = viewModel;
            PluginsViewModel = viewModel;

            PluginsViewModel.StorageProvider = TopLevel.GetTopLevel(this)?.StorageProvider;
        }
    }
}