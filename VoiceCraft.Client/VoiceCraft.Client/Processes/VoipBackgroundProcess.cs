using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using LiteNetLib;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.Services.Interfaces;
using VoiceCraft.Core.Network;

namespace VoiceCraft.Client.Processes
{
    public class VoipBackgroundProcess : IBackgroundProcess
    {
        //Events
        public event Action<string>? OnUpdateTitle;
        public event Action<string>? OnUpdateDescription;
        public event Action? OnConnected;
        public event Action<DisconnectInfo>? OnDisconnected;
        
        //Public Variables
        public CancellationTokenSource TokenSource { get; }
        public ConnectionStatus ConnectionStatus => _voiceCraftClient.ConnectionStatus; 
        
        //Privates
        private readonly VoiceCraftClient _voiceCraftClient;
        private readonly NotificationService _notificationService;
        private readonly string _ip;
        private readonly int _port;

        public VoipBackgroundProcess(string ip, int port, NotificationService notificationService)
        {
            TokenSource = new CancellationTokenSource();
            _voiceCraftClient = new VoiceCraftClient();
            _notificationService = notificationService;
            _voiceCraftClient.OnConnected += ClientOnConnected;
            _voiceCraftClient.OnDisconnected += ClientOnDisconnected;
            _ip = ip;
            _port = port;
        }

        public void Start()
        {
            _voiceCraftClient.Connect(_ip, _port, LoginType.Login);
            OnUpdateTitle?.Invoke("VoiceCraft Client is running");
            OnUpdateDescription?.Invoke("Status: Connecting...");

            while (_voiceCraftClient.ConnectionStatus != ConnectionStatus.Disconnected)
            {
                if (TokenSource.Token.IsCancellationRequested)
                    _voiceCraftClient.Disconnect();
                _voiceCraftClient.Update(); //Update all networking processes.
                Task.Delay(1).GetAwaiter().GetResult();
            }
        }
        
        public void Dispose()
        {
            if(_voiceCraftClient.ConnectionStatus != ConnectionStatus.Disconnected)
                _voiceCraftClient.Disconnect();
            _voiceCraftClient.Dispose();
            GC.SuppressFinalize(this);
        }
        
        private void ClientOnConnected()
        {
            OnUpdateTitle?.Invoke("VoiceCraft Client is running");
            OnUpdateDescription?.Invoke("Status: Connected!");
            Dispatcher.UIThread.Invoke(() => OnConnected?.Invoke());
        }
        
        private void ClientOnDisconnected(DisconnectInfo obj)
        {
            OnUpdateTitle?.Invoke("VoiceCraft Client is running");
            OnUpdateDescription?.Invoke($"Status: Disconnected! Reason: {obj.Reason}");
            Dispatcher.UIThread.Invoke(() =>
            {
                _notificationService.SendNotification($"Disconnected! Reason: {obj.Reason}");
                OnDisconnected?.Invoke(obj);
            });
        }
    }
}