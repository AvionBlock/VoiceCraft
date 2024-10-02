using Avalonia.Controls;
using VoiceCraft.Client.PDK;
using VoiceCraft.Client.ViewModels;

namespace VoiceCraft.Client.Views
{
    public partial class DefaultMainView : UserControl, IMainView
    {
        public readonly DefaultMainViewModel ViewModel;

        public DefaultMainView(DefaultMainViewModel defaultViewModel)
        {
            InitializeComponent();

            DataContext = defaultViewModel;
            ViewModel = defaultViewModel;
        }
    }
}