using System;
using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Client.Models.Settings;
using VoiceCraft.Client.Services;
using VoiceCraft.Network.World;

namespace VoiceCraft.Client.ViewModels.Data;

public partial class EntityDataViewModel : ObservableObject
{
    private readonly Guid? _entityUserId;
    private readonly SettingsService _settingsService;

    private readonly UserSettings _userSettings;

    //Entity Display.
    [ObservableProperty] public partial string DisplayName { get; set; }
    [ObservableProperty] public partial bool IsDeafened { get; set; }
    [ObservableProperty] public partial bool IsMuted { get; set; }
    [ObservableProperty] public partial bool IsServerDeafened { get; set; }
    [ObservableProperty] public partial bool IsServerMuted { get; set; }
    [ObservableProperty] public partial bool IsSpeaking { get; set; }
    [ObservableProperty] public partial bool IsVisible { get; set; }
    [ObservableProperty] public partial bool UserMuted { get; set; }

    private bool _userMutedUpdating;
    private bool _userVolumeUpdating;

    //User Settings
    [ObservableProperty] private float _volume;

    public VoiceCraftClientEntity Entity { get; }

    public EntityDataViewModel(VoiceCraftClientEntity entity, SettingsService settingsService)
    {
        Entity = entity;
        _userSettings = settingsService.UserSettings;
        _settingsService = settingsService;

        if (entity is VoiceCraftClientNetworkEntity networkEntity)
        {
            _entityUserId = networkEntity.UserGuid;
            IsServerMuted = networkEntity.ServerMuted;
            IsServerDeafened = networkEntity.ServerDeafened;
            if (_userSettings.Users.TryGetValue((Guid)_entityUserId, out var entitySetting))
            {
                entity.Volume = entitySetting.Volume;
                entity.UserMuted = entitySetting.UserMuted;
            }

            networkEntity.OnServerMuteUpdated += (value, _) => IsServerMuted = value;
            networkEntity.OnServerDeafenUpdated += (value, _) => IsServerDeafened = value;
        }

        DisplayName = entity.Name;
        IsMuted = entity.Muted;
        IsDeafened = entity.Deafened;
        IsVisible = entity.IsVisible;
        IsSpeaking = entity.IsSpeaking;
        _volume = entity.Volume;
        UserMuted = entity.UserMuted;

        entity.OnNameUpdated += (value, _) => DisplayName = value;
        entity.OnMuteUpdated += (value, _) => IsMuted = value;
        entity.OnDeafenUpdated += (value, _) => IsDeafened = value;
        entity.OnIsVisibleUpdated += (value, _) => IsVisible = value;
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
        Entity.Volume = value;
        SaveSettings();
        _userVolumeUpdating = false;
    }

    partial void OnUserMutedChanging(bool value)
    {
        if (_userMutedUpdating) return;
        _userMutedUpdating = true;
        Entity.UserMuted = value;
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

        entity.Volume = Entity.Volume;
        entity.UserMuted = Entity.UserMuted;

        _ = _settingsService.SaveAsync();
    }
}