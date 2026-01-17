using System;
using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Client.Models.Settings;
using VoiceCraft.Client.Services;
using VoiceCraft.Core.World;
using VoiceCraft.Network.World;

namespace VoiceCraft.Client.ViewModels.Data;

public partial class EntityViewModel : ObservableObject
{
    private readonly VoiceCraftEntity _entity;

    private readonly Guid? _entityUserId;
    private readonly SettingsService _settingsService;

    private readonly UserSettings _userSettings;

    //Entity Display.
    [ObservableProperty] private string _displayName;
    [ObservableProperty] private bool _isDeafened;
    [ObservableProperty] private bool _isMuted;
    [ObservableProperty] private bool _isServerDeafened;
    [ObservableProperty] private bool _isServerMuted;
    [ObservableProperty] private bool _isSpeaking;
    [ObservableProperty] private bool _isVisible;
    [ObservableProperty] private bool _userMuted;
    private bool _userMutedUpdating;
    private bool _userVolumeUpdating;

    //User Settings
    [ObservableProperty] private float _volume;

    public EntityViewModel(VoiceCraftEntity entity, SettingsService settingsService)
    {
        _entity = entity;
        _userSettings = settingsService.UserSettings;
        _settingsService = settingsService;

        if (entity is VoiceCraftNetworkEntity networkEntity)
        {
            _entityUserId = networkEntity.UserGuid;
            _isServerMuted = networkEntity.ServerMuted;
            _isServerDeafened = networkEntity.ServerDeafened;
            if (_userSettings.Users.TryGetValue((Guid)_entityUserId, out var entitySetting))
            {
                entity.Volume = entitySetting.Volume;
                entity.UserMuted = entitySetting.UserMuted;
            }

            networkEntity.OnServerMuteUpdated += (value, _) => IsServerMuted = value;
            networkEntity.OnServerDeafenUpdated += (value, _) => IsServerDeafened = value;
        }

        _displayName = entity.Name;
        _isMuted = entity.Muted;
        _isDeafened = entity.Deafened;
        _isVisible = entity.IsVisible;
        _isSpeaking = entity.IsSpeaking;
        _volume = entity.Volume;
        _userMuted = entity.UserMuted;

        entity.OnNameUpdated += (value, _) => DisplayName = value;
        entity.OnMuteUpdated += (value, _) => IsMuted = value;
        entity.OnDeafenUpdated += (value, _) => IsDeafened = value;
        entity.OnStartedSpeaking += _ => IsSpeaking = true;
        entity.OnStoppedSpeaking += _ => IsSpeaking = false;
        entity.OnVolumeUpdated += UpdateVolume;
        entity.OnUserMutedUpdated += UpdateUserMuted;
    }

    private void UpdateVolume(float volume, VoiceCraftClientEntity entity)
    {
        if (_userVolumeUpdating) return;
        _userVolumeUpdating = true;
        Volume = volume;
        _userVolumeUpdating = false;
    }

    private void UpdateUserMuted(bool userMuted, VoiceCraftClientEntity entity)
    {
        if (_userMutedUpdating) return;
        _userMutedUpdating = true;
        UserMuted = userMuted;
        _userMutedUpdating = false;
    }

    partial void OnVolumeChanging(float value)
    {
        if (_userVolumeUpdating) return;
        _userVolumeUpdating = true;
        _entity.Volume = value;
        SaveSettings();
        _userVolumeUpdating = false;
    }

    partial void OnUserMutedChanging(bool value)
    {
        if (_userMutedUpdating) return;
        _userMutedUpdating = true;
        _entity.UserMuted = value;
        SaveSettings();
        _userMutedUpdating = false;
    }

    private void SaveSettings()
    {
        if (_entityUserId == null) return;
        if (!_userSettings.Users.TryGetValue((Guid)_entityUserId, out var entity))
        {
            entity = new UserSetting();
            _userSettings.Users.Add((Guid)_entityUserId, entity);
        }

        entity.Volume = _entity.Volume;
        entity.UserMuted = _entity.UserMuted;

        _ = _settingsService.SaveAsync();
    }
}