using System;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Client.Models.Settings;

namespace VoiceCraft.Client.ViewModels.Settings
{
    public partial class ThemeSettingsViewModel : ObservableObject, IDisposable
    {
        private bool _updating;
        private bool _disposed;
        private readonly ThemeSettings _themeSettings;

        [ObservableProperty] private string _selectedTheme;

        public ThemeSettingsViewModel(ThemeSettings themeSettings)
        {
            _themeSettings = themeSettings;
            _themeSettings.OnUpdated += Update;
            _selectedTheme = _themeSettings.SelectedTheme;
        }
        protected override void OnPropertyChanging(PropertyChangingEventArgs e)
        {
            ThrowIfDisposed();
            
            if (_updating) return;
            _updating = true;
            
            _themeSettings.SelectedTheme = SelectedTheme;
            
            base.OnPropertyChanging(e);
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