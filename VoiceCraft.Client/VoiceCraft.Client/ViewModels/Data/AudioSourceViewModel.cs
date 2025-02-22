using CommunityToolkit.Mvvm.ComponentModel;

namespace VoiceCraft.Client.ViewModels.Data
{
    public partial class AudioSourceViewModel : ObservableObject
    {
        [ObservableProperty] private string _displayName = "Test";
        [ObservableProperty] private bool _isMuted;
        [ObservableProperty] private bool _isDeafened;
    }
}