using VoiceCraft.Client.PDK.ViewModels;
using VoiceCraft.Client.PDK.Views;
using VoiceCraft.Client.Plugin.ViewModels.Home;

namespace VoiceCraft.Client.Plugin.Views.Home
{
    public partial class ServersView : ViewBase
    {
        public override ViewModelBase ViewModel => ServersViewModel;

        public readonly ServersViewModel ServersViewModel;

        public ServersView(ServersViewModel viewModel)
        {
            InitializeComponent();

            DataContext = viewModel;
            ServersViewModel = viewModel;
        }
    }
}