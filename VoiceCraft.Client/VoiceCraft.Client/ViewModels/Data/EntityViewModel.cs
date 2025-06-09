using System;
using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Client.Models.Settings;
using VoiceCraft.Client.Network;
using VoiceCraft.Client.Services;
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
    private readonly EntitySettings _entitySettings;
    private readonly SettingsService _settingsService;
    private readonly Guid? _entityUserId;
    private bool _volumeUpdating;

    public EntityViewModel(VoiceCraftClientEntity entity, SettingsService settingsService)
    {
        _entity = entity;
        _entitySettings = settingsService.EntitySettings;
        _settingsService = settingsService;
        _displayName = entity.Name;
        _isMuted = entity.Muted;
        _isDeafened = entity.Deafened;
        _isVisible = entity.IsVisible;
        _volume = entity.Volume;

        if (entity is VoiceCraftClientNetworkEntity networkEntity)
            _entityUserId = networkEntity.UserGuid;

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
        SaveSettings();
        _volumeUpdating = false;
    }

    partial void OnVolumeChanging(float value)
    {
        if (_volumeUpdating) return;
        _volumeUpdating = true;
        _entity.Volume = value;
        _volumeUpdating = false;
    }

    private void SaveSettings()
    {
        if (_entityUserId == null) return;
        if (!_entitySettings.Entities.TryGetValue((Guid)_entityUserId, out var entity))
        {
            entity = new EntitySetting();
            _entitySettings.Entities.Add((Guid)_entityUserId, entity);
        }

        entity.Volume = Volume;

        _ = _settingsService.SaveAsync();
    }
}