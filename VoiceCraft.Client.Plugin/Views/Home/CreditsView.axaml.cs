using VoiceCraft.Client.PDK.ViewModels;
using VoiceCraft.Client.PDK.Views;
using VoiceCraft.Client.Plugin.ViewModels.Home;

namespace VoiceCraft.Client.Plugin.Views.Home
{
    public partial class CreditsView : ViewBase
    {
        public override ViewModelBase ViewModel => CreditsViewModel;

        public readonly CreditsViewModel CreditsViewModel;
        
        public CreditsView(CreditsViewModel viewModel)
        {
            InitializeComponent();

            DataContext = viewModel;
            CreditsViewModel = viewModel;
        }
    }
}