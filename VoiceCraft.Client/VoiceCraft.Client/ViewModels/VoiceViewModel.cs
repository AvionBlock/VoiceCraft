using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiteNetLib;
using VoiceCraft.Client.Processes;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.ViewModels.Data;
using VoiceCraft.Core;

namespace VoiceCraft.Client.ViewModels
{
    public partial class VoiceViewModel(NavigationService navigationService) : ViewModelBase, IDisposable
    {
        public override bool DisableBackButton { get; set; } = true;
        private VoipBackgroundProcess? _process;

        [ObservableProperty] private string _statusText = string.Empty;
        [ObservableProperty] private bool _isMuted;
        [ObservableProperty] private bool _isDeafened;
        [ObservableProperty] private ObservableCollection<EntityViewModel> _entityViewModels = [];

        [RelayCommand]
        private void ToggleMute()
        {
            _process?.ToggleMute();
        }

        [RelayCommand]
        private void ToggleDeafen()
        {
            _process?.ToggleDeafen();
        }
        
        [RelayCommand]
        private void Disconnect()
        {
            if (_process?.ConnectionState == ConnectionState.Disconnected)
            {
                navigationService.Back(); //If disconnected. Return to previous page.
                return;
            }
            
            _process?.Disconnect();
        }

        public override void OnAppearing()
        {
            if (_process == null) return;
            if (_process.Status != BackgroundProcessStatus.Started)
            {
                navigationService.Back();
                return;
            }
            
            //Register events first.
            _process.OnDisconnected += OnDisconnected;
            _process.OnUpdateDescription += OnUpdateDescription;
            _process.OnUpdateMute += OnUpdateMute;
            _process.OnUpdateDeafen += OnUpdateDeafen;
            _process.OnEntityAdded += OnEntityAdded;
            _process.OnEntityRemoved += OnEntityRemoved;
            
            StatusText = _process.Description;
            IsMuted = _process.Muted;
            IsDeafened = _process.Deafened;
        }

        public void AttachToProcess(VoipBackgroundProcess process)
        {
            _process = process;
            OnAppearing();
        }
        
        public void Dispose()
        {
            if (_process != null)
            {
                _process.OnDisconnected -= OnDisconnected;
                _process.OnUpdateDescription -= OnUpdateDescription;
                _process.OnUpdateMute -= OnUpdateMute;
                _process.OnUpdateDeafen -= OnUpdateDeafen;
                _process.OnEntityAdded -= OnEntityAdded;
                _process.OnEntityRemoved -= OnEntityRemoved;
            }

            GC.SuppressFinalize(this);
        }

        private void OnUpdateDescription(string description)
        {
            StatusText = description;
        }
        
        private void OnDisconnected(string reason)
        {
            if (_process != null)
            {
                _process.OnDisconnected -= OnDisconnected;
                _process.OnUpdateDescription -= OnUpdateDescription;
                _process.OnUpdateMute -= OnUpdateMute;
                _process.OnUpdateDeafen -= OnUpdateDeafen;
                _process.OnEntityAdded -= OnEntityAdded;
                _process.OnEntityRemoved -= OnEntityRemoved;
            }

            navigationService.Back();
        }
        
        private void OnUpdateMute(bool muted)
        {
            IsMuted = muted;
        }
        
        private void OnUpdateDeafen(bool deafened)
        {
            IsDeafened = deafened;
        }
        
        private void OnEntityAdded(EntityViewModel entity)
        {
            EntityViewModels.Add(entity);
        }
        
        private void OnEntityRemoved(EntityViewModel entity)
        {
            EntityViewModels.Remove(entity);
        }
    }
}