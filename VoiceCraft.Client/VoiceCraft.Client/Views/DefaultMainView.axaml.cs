using VoiceCraft.Client.PDK;
using VoiceCraft.Client.PDK.ViewModels;
using VoiceCraft.Client.PDK.Views;

namespace VoiceCraft.Client.Views
{
    public partial class DefaultMainView : ViewBase, IMainView
    {
        public override ViewModelBase ViewModel => (ViewModelBase)_mainViewModel;
        private readonly IMainViewModel _mainViewModel;

        public DefaultMainView(IMainViewModel mainViewModel)
        {
            InitializeComponent();

            DataContext = mainViewModel;
            _mainViewModel = mainViewModel;
        }
    }
}