using Avalonia.Controls;
using VoiceCraft.Client.Plugin.Settings;
using VoiceCraft.Client.Plugin.ViewModels;

namespace VoiceCraft.Client.Plugin.Views
{
    public partial class EditServerView : UserControl
    {
        private EditServerViewModel serverViewModel;

        public Server Server
        {
            get
            {
                return serverViewModel.Server;
            }
            set
            {
                serverViewModel.Server = value;
            }
        }

        public EditServerView(EditServerViewModel viewModel)
        {
            InitializeComponent();

            DataContext = viewModel;
            serverViewModel = viewModel;
        }
    }
}