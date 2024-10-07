using VoiceCraft.Client.PDK.ViewModels;
using VoiceCraft.Client.PDK.Views;
using VoiceCraft.Client.Plugin.ViewModels;

namespace VoiceCraft.Client.Plugin.Views
{
    public partial class ServerView : ViewBase
    {
        public override ViewModelBase ViewModel => ServerViewModel;

        public readonly ServerViewModel ServerViewModel;

        public ServerView(ServerViewModel viewModel)
        {
            InitializeComponent();

            DataContext = viewModel;
            ServerViewModel = viewModel;
        }
    }
}