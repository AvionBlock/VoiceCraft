using VoiceCraft.Client.PDK.ViewModels;
using VoiceCraft.Client.PDK.Views;
using VoiceCraft.Client.Plugin.ViewModels;

namespace VoiceCraft.Client.Plugin.Views
{
    public partial class HomeView : ViewBase
    {
        public override ViewModelBase ViewModel => HomeViewModel;

        public readonly HomeViewModel HomeViewModel;

        public HomeView(HomeViewModel viewModel)
        {
            InitializeComponent();

            DataContext = viewModel;
            HomeViewModel = viewModel;
        }
    }
}