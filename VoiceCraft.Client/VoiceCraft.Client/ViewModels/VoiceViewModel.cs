using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiteNetLib;
using VoiceCraft.Client.Processes;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.ViewModels.Data;
using VoiceCraft.Core.Network.Packets;

namespace VoiceCraft.Client.ViewModels
{
    public partial class VoiceViewModel(NavigationService navigationService) : ViewModelBase, IDisposable
    {
        public override bool DisableBackButton { get; set; } = true;
        private VoipBackgroundProcess? _process;

        [ObservableProperty] private string _statusText = string.Empty;
        [ObservableProperty] private bool _isMuted;
        [ObservableProperty] private bool _isDeafened;
        [ObservableProperty] private ObservableCollection<AudioSourceViewModel> _audioSources = [];

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
            _process?.Disconnect();
        }

        public override void OnAppearing()
        {
            if (_process == null) return;
            if (_process.ConnectionStatus == ConnectionStatus.Disconnected)
            {
                navigationService.Back();
                return;
            }
            
            StatusText = _process.Description;
            IsMuted = _process.Muted;
            IsDeafened = _process.Deafened;
            _process.OnDisconnected += OnDisconnected;
            _process.OnUpdateDescription += OnUpdateDescription;
            _process.OnUpdateMute += OnUpdateMute;
            _process.OnUpdateDeafen += OnUpdateDeafen;
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
            }

            GC.SuppressFinalize(this);
        }

        private void OnUpdateDescription(string description)
        {
            StatusText = description;
        }
        
        private void OnDisconnected(DisconnectInfo obj)
        {
            if (_process != null)
            {
                _process.OnDisconnected -= OnDisconnected;
                _process.OnUpdateDescription -= OnUpdateDescription;
                _process.OnUpdateMute -= OnUpdateMute;
                _process.OnUpdateDeafen -= OnUpdateDeafen;
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
    }
}