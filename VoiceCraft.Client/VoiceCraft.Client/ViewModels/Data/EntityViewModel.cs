using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Client.Network;

namespace VoiceCraft.Client.ViewModels.Data
{
    public partial class EntityViewModel : ObservableObject
    {
        [ObservableProperty] private string _displayName;
        [ObservableProperty] private bool _isMuted;
        [ObservableProperty] private bool _isDeafened;
        [ObservableProperty] private bool _isVisible;
        [ObservableProperty] private float _volume;

        public EntityViewModel(VoiceCraftClientEntity entity)
        {
            _displayName = entity.Name;
            _isMuted = entity.Muted;
            _isDeafened = entity.Deafened;
            _isVisible = entity.IsVisible;
            _volume = entity.Volume;

            entity.OnNameUpdated += (value, _) => DisplayName = value;
            entity.OnMuteUpdated += (value, _) => IsMuted = value;
            entity.OnDeafenUpdated += (value, _) => IsDeafened = value;
            entity.OnIsVisibleUpdated += (value, _) => IsVisible = value;
        }
    }
}