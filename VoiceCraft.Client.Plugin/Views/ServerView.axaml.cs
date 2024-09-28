using Avalonia.Controls;
using VoiceCraft.Client.Plugin.Settings;
using VoiceCraft.Client.Plugin.ViewModels;

namespace VoiceCraft.Client.Plugin.Views
{
    public partial class ServerView : UserControl
    {
        private ServerViewModel serverViewModel;

        public Server SelectedServer
        {
            get
            {
                return serverViewModel.SelectedServer;
            }
            set
            {
                serverViewModel.SelectedServer = value;
            }
        }

        public ServerView(ServerViewModel viewModel)
        {
            InitializeComponent();

            DataContext = viewModel;
            serverViewModel = viewModel;
        }
    }
}