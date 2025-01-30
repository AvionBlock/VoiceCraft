using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using LiteNetLib;
using VoiceCraft.Client.Processes;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.ViewModels.Data;
using VoiceCraft.Core.Network;

namespace VoiceCraft.Client.ViewModels
{
    public partial class VoiceViewModel : ViewModelBase, IDisposable
    {
        private readonly NavigationService _navigationService;
        private readonly BackgroundService _backgroundService;
        private VoipBackgroundProcess? _process;

        [ObservableProperty] private string _statusText = "My Ass is on fire.\nFIRED, YOU'RE NEXT!";
        [ObservableProperty] private ObservableCollection<EntityViewModel> _entities = [new(), new(), new(), new(), new(), new()];
        
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