using Avalonia.Controls;
using VoiceCraft.Client.Plugin.ViewModels.Home;

namespace VoiceCraft.Client.Plugin.Views.Home
{
    public partial class PluginsView : UserControl
    {
        public PluginsView(PluginsViewModel viewModel)
        {
            InitializeComponent();

            DataContext = viewModel;
        }
    }
}