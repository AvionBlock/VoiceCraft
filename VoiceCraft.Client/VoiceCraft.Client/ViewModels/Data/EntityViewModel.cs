using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Client.Network;
using VoiceCraft.Core;

namespace VoiceCraft.Client.ViewModels.Data;

public partial class EntityViewModel : ObservableObject
{
    [ObservableProperty] private string _displayName;
    [ObservableProperty] private bool _isDeafened;
    [ObservableProperty] private bool _isMuted;
    [ObservableProperty] private bool _isVisible;
    [ObservableProperty] private float _volume;

    private readonly VoiceCraftClientEntity _entity;
    private bool _volumeUpdating;

    public EntityViewModel(VoiceCraftClientEntity entity)
    {
        _entity = entity;
        _displayName = entity.Name;
        _isMuted = entity.Muted;
        _isDeafened = entity.Deafened;
        _isVisible = entity.IsVisible;
        _volume = entity.Volume;

        entity.OnNameUpdated += (value, _) => DisplayName = value;
        entity.OnMuteUpdated += (value, _) => IsMuted = value;
        entity.OnDeafenUpdated += (value, _) => IsDeafened = value;
        entity.OnIsVisibleUpdated += (value, _) => IsVisible = value;
        entity.OnVolumeUpdated += UpdateVolume;
    }

    private void UpdateVolume(float volume, VoiceCraftEntity entity)
    {
        if (_volumeUpdating) return;
        _volumeUpdating = true;
        Volume = volume;
        _volumeUpdating = false;
    }

    partial void OnVolumeChanging(float value)
    {
        if (_volumeUpdating) return;
        _volumeUpdating = true;
        _entity.Volume = value;
        _volumeUpdating = false;
    }
}