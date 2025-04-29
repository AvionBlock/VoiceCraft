using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Core;

namespace VoiceCraft.Client.ViewModels.Data
{
    public partial class EntityViewModel : ObservableObject
    {
        [ObservableProperty] private string _displayName;
        [ObservableProperty] private bool _isMuted;
        [ObservableProperty] private bool _isDeafened;

        public EntityViewModel(VoiceCraftEntity entity)
        {
            _displayName = entity.Name;
            _isMuted = entity.Muted;
            _isDeafened = entity.Deafened;

            entity.OnNameUpdated += (value, _) => DisplayName = value;
            entity.OnMuteUpdated += (value, _) => IsMuted = value;
            entity.OnDeafenUpdated += (value, _) => IsDeafened = value;
        }
    }
}