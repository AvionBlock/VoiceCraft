using System;
using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Client.Models.Settings;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.ViewModels.Settings
{
    public partial class ThemeSettingsViewModel : ObservableObject, IDisposable
    {
        private bool _updating;
        private bool _disposed;
        private readonly ThemeSettings _themeSettings;
        private readonly SettingsService _settingsService;

        [ObservableProperty] private string _selectedTheme;

        public ThemeSettingsViewModel(ThemeSettings themeSettings, SettingsService settingsService)
        {
            _themeSettings = themeSettings;
            _settingsService = settingsService;
            _themeSettings.OnUpdated += Update;
            _selectedTheme = _themeSettings.SelectedTheme;
        }

        partial void OnSelectedThemeChanging(string value)
        {
            ThrowIfDisposed();
            
            if (_updating) return;
            _updating = true;
            _themeSettings.SelectedTheme = value;
            _ = _settingsService.SaveAsync();
            _updating = false;
        }

        private void Update(ThemeSettings themeSettings)
        {
            if (_updating) return;
            _updating = true;
            
            SelectedTheme = themeSettings.SelectedTheme;
            
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
            _themeSettings.OnUpdated -= Update;
            
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}