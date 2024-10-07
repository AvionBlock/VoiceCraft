using VoiceCraft.Client.PDK.ViewModels;
using VoiceCraft.Client.PDK.Views;
using VoiceCraft.Client.Plugin.ViewModels;

namespace VoiceCraft.Client.Plugin.Views
{
    public partial class EditServerView : ViewBase
    {
        public override ViewModelBase ViewModel => EditServerViewModel;

        public readonly EditServerViewModel EditServerViewModel;

        public EditServerView(EditServerViewModel viewModel)
        {
            InitializeComponent();

            DataContext = viewModel;
            EditServerViewModel = viewModel;
        }
    }
}