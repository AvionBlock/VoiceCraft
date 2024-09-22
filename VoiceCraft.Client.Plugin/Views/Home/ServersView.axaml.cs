using Avalonia.Controls;
using VoiceCraft.Client.Plugin.ViewModels.Home;

namespace VoiceCraft.Client.Plugin.Views.Home
{
    public partial class ServersView : UserControl
    {
        public ServersView(ServersViewModel viewModel)
        {
            InitializeComponent();
            
            DataContext = viewModel;
        }
    }
}