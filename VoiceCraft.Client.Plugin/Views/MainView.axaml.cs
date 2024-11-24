using Avalonia.Controls;
using VoiceCraft.Client.PDK;

namespace VoiceCraft.Client.Plugin.Views
{
    public partial class MainView : UserControl, IMainView
    {
        public MainView(IMainViewModel mainViewModel)
        {
            InitializeComponent();

            DataContext = mainViewModel;
        }
    }
}