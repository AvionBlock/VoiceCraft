using Avalonia.Controls;
using VoiceCraft.Client.Plugin.ViewModels.Home;

namespace VoiceCraft.Client.Plugin.Views.Home
{
    public partial class CreditsView : UserControl
    {
        public CreditsView(CreditsViewModel viewModel)
        {
            InitializeComponent();

            DataContext = viewModel;
        }
    }
}