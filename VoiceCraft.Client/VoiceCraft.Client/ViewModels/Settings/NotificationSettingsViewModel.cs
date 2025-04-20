using System;
using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Client.Models.Settings;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.ViewModels.Settings
{
    public partial class NotificationSettingsViewModel : ObservableObject, IDisposable
    {
        private bool _updating;
        private bool _disposed;
        private readonly NotificationSettings _notificationSettings;
        private readonly SettingsService _settingsService;

        [ObservableProperty] private ushort _dismissDelayMs;
        [ObservableProperty] private bool _disableNotifications;

        public NotificationSettingsViewModel(SettingsService settingsService)
        {
            _notificationSettings = settingsService.NotificationSettings;
            _settingsService = settingsService;
            _notificationSettings.OnUpdated += Update;
            _dismissDelayMs = _notificationSettings.DismissDelayMs;
            _disableNotifications = _notificationSettings.DisableNotifications;
        }

        partial void OnDismissDelayMsChanging(ushort value)
        {
            ThrowIfDisposed();
            
            if (_updating) return;
            _updating = true;
            _notificationSettings.DismissDelayMs = value;
            _ = _settingsService.SaveAsync();
            _updating = false;
        }

        partial void OnDisableNotificationsChanging(bool value)
        {
            ThrowIfDisposed();
            
            if (_updating) return;
            _updating = true;
            _notificationSettings.DisableNotifications = value;
            _ = _settingsService.SaveAsync();
            _updating = false;
        }

        private void Update(NotificationSettings notificationSettings)
        {
            if (_updating) return;
            _updating = true;
            
            DismissDelayMs = notificationSettings.DismissDelayMs;
            DisableNotifications = notificationSettings.DisableNotifications;
            
            _updating = false;
        }
        
        private void ThrowIfDisposed()
        {
            if (!_disposed) return;
            throw new ObjectDisposedException(typeof(NotificationSettingsViewModel).ToString());
        }
        
        public void Dispose()
        {
            if(_disposed) return;
            _notificationSettings.OnUpdated -= Update;
            
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}