using System;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Client.Models.Settings;

namespace VoiceCraft.Client.ViewModels.Settings
{
    public partial class NotificationSettingsViewModel : ObservableObject, IDisposable
    {
        private bool _updating;
        private bool _disposed;
        private readonly NotificationSettings _notificationSettings;

        [ObservableProperty] private ushort _dismissDelayMs;
        [ObservableProperty] private bool _disableNotifications;

        public NotificationSettingsViewModel(NotificationSettings notificationSettings)
        {
            _notificationSettings = notificationSettings;
            _notificationSettings.OnUpdated += Update;
            _dismissDelayMs = _notificationSettings.DismissDelayMS;
            _disableNotifications = _notificationSettings.DisableNotifications;
        }
        
        protected override void OnPropertyChanging(PropertyChangingEventArgs e)
        {
            ThrowIfDisposed();
            
            if (_updating) return;
            _updating = true;
            
            _notificationSettings.DismissDelayMS = DismissDelayMs;
            _notificationSettings.DisableNotifications = DisableNotifications;
            
            base.OnPropertyChanging(e);
            _updating = false;
        }
        
        private void Update(NotificationSettings notificationSettings)
        {
            if (_updating) return;
            _updating = true;
            
            DismissDelayMs = notificationSettings.DismissDelayMS;
            DisableNotifications = notificationSettings.DisableNotifications;
            
            _updating = false;
        }
        
        private void ThrowIfDisposed()
        {
            if (!_disposed) return;
            throw new ObjectDisposedException(nameof(ServerViewModel));
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