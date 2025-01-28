using System;
using System.Diagnostics;
using LiteNetLib;
using VoiceCraft.Client.Processes;
using VoiceCraft.Client.Services;
using VoiceCraft.Core.Network;

namespace VoiceCraft.Client.ViewModels
{
    public class VoiceViewModel : ViewModelBase, IDisposable
    {
        private readonly NavigationService _navigationService;
        private readonly BackgroundService _backgroundService;
        private VoipBackgroundProcess? _process;
        
        public VoiceViewModel(NavigationService navigationService, BackgroundService backgroundService)
        {
            _navigationService = navigationService;
            _backgroundService = backgroundService;
        }
        
        public override void OnAppearing()
        {
            _process = _backgroundService.GetBackgroundProcess<VoipBackgroundProcess>();
            if (_process == null)
            {
                _navigationService.Back();
                return;
            }

            if (_process.ConnectionStatus == ConnectionStatus.Disconnected)
            {
                _navigationService.Back();
                return;
            }
            
            _process.OnConnected += OnConnected;
            _process.OnDisconnected += OnDisconnected;
        }

        public void Dispose()
        {
            if (_process != null)
            {
                _process.OnConnected -= OnConnected;
                _process.OnDisconnected -= OnDisconnected;
            }

            GC.SuppressFinalize(this);
        }

        private void OnConnected()
        {
            Debug.WriteLine("Connected!");
        }

        private void OnDisconnected(DisconnectInfo obj)
        {
            _navigationService.Back();
        }
    }
}