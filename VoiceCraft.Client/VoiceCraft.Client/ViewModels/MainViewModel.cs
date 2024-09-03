using CommunityToolkit.Mvvm.ComponentModel;

namespace VoiceCraft.Client.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        [ObservableProperty]
        private string _greeting = "Welcome to Avalonia!";
    }
}
