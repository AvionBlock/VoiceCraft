using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Core;

namespace VoiceCraft.Client.ViewModels.Data
{
    public partial class EntityViewModel(VoiceCraftEntity entity) : ObservableObject
    {
        [ObservableProperty] private string _displayName = entity.Name;
        [ObservableProperty] private bool _isMuted;
        [ObservableProperty] private bool _isDeafened;
    }
}