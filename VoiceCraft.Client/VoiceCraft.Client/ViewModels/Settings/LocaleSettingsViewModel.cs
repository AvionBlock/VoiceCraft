using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Jeek.Avalonia.Localization;
using VoiceCraft.Client.Models.Settings;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.ViewModels.Settings
{
    public partial class LocaleSettingsViewModel : ObservableObject, IDisposable
    {
        private bool _updating;
        private bool _disposed;
        private readonly LocaleSettings _localeSettings;
        private readonly SettingsService _settingsService;
        
        [ObservableProperty] private string _culture;
        
        public LocaleSettingsViewModel(SettingsService settingsService)
        {
            _localeSettings = settingsService.LocaleSettings;
            _settingsService = settingsService;
            _localeSettings.OnUpdated += Update;
            _culture = _localeSettings.Culture;
        }

        partial void OnCultureChanging(string value)
        {
            ThrowIfDisposed();
            Localizer.Language = value;
            
            if (_updating) return;
            _updating = true;
            _localeSettings.Culture = value;
            _ = _settingsService.SaveAsync();
            _updating = false;
        }
        
        private void Update(LocaleSettings localeSettings)
        {
            if (_updating) return;
            _updating = true;
            
            Culture = localeSettings.Culture;
            
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
            _localeSettings.OnUpdated -= Update;
            
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}