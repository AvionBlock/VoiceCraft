using VoiceCraft.Client.PDK.ViewModels;
using VoiceCraft.Client.PDK.Views;
using VoiceCraft.Client.Plugin.ViewModels.Home;

namespace VoiceCraft.Client.Plugin.Views.Home
{
    public partial class AddServerView : ViewBase
    {
        public override ViewModelBase ViewModel => AddServerViewModel;

        public readonly AddServerViewModel AddServerViewModel;

        public AddServerView(AddServerViewModel viewModel)
        {
            InitializeComponent();

            DataContext = viewModel;
            AddServerViewModel = viewModel;
        }
    }
}