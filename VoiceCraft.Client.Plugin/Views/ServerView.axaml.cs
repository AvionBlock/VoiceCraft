using Avalonia.Controls;
using VoiceCraft.Client.Plugin.ViewModels;

namespace VoiceCraft.Client.Plugin.Views
{
    public partial class ServerView : UserControl
    {
        public ServerViewModel ViewModel;

        public ServerView(ServerViewModel viewModel)
        {
            InitializeComponent();

            DataContext = viewModel;
            ViewModel = viewModel;
        }
    }
}