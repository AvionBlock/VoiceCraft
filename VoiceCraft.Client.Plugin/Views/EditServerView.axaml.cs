using Avalonia.Controls;
using VoiceCraft.Client.Plugin.ViewModels;

namespace VoiceCraft.Client.Plugin.Views
{
    public partial class EditServerView : UserControl
    {
        public readonly EditServerViewModel ViewModel;

        public EditServerView(EditServerViewModel viewModel)
        {
            InitializeComponent();

            DataContext = viewModel;
            ViewModel = viewModel;
        }
    }
}