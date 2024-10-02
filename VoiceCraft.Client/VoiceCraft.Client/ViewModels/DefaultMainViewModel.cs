using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Client.PDK.ViewModels;

namespace VoiceCraft.Client.ViewModels
{
    public partial class DefaultMainViewModel : ViewModelBase
    {
        public override string Title => "Default";

        [ObservableProperty]
        private string _message = string.Empty;
    }
}