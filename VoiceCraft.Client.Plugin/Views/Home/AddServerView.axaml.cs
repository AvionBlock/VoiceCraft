using Avalonia.Controls;
using VoiceCraft.Client.Plugin.ViewModels.Home;

namespace VoiceCraft.Client.Plugin.Views.Home
{
    public partial class AddServerView : UserControl
    {
        public AddServerView(AddServerViewModel viewModel)
        {
            InitializeComponent();

            DataContext = viewModel;
        }
    }
}