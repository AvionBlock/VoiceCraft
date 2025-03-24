using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using LiteNetLib;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using VoiceCraft.Client.Audio.Interfaces;
using VoiceCraft.Client.Network;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.Services.Interfaces;
using VoiceCraft.Core.Network;

namespace VoiceCraft.Client.Processes
{
    public class VoipBackgroundProcess(string ip, int port, NotificationService notificationService, AudioService audioService)
        : IBackgroundProcess
    {
        private string _title = string.Empty;
        private string _description = string.Empty;
        private bool _muted;
        private bool _deafened;
        
        //Events
        public event Action<string>? OnUpdateTitle;
        public event Action<string>? OnUpdateDescription;
        public event Action<bool>? OnUpdateMute;
        public event Action<bool>? OnUpdateDeafen;
        public event Action? OnConnected;
        public event Action<DisconnectInfo>? OnDisconnected;
        
        //Public Variables
        public bool IsStarted { get; private set; }
        public CancellationTokenSource TokenSource { get; } = new();
        public ConnectionState ConnectionState => _voiceCraftClient.ConnectionState;
        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                OnUpdateTitle?.Invoke(value);
            }
        }
        
        public string Description
        {
            get => _description;
            private set
            {
                _description = value;
                OnUpdateDescription?.Invoke(value);
            }
        }

        public bool Muted
        {
            get => _muted;
            private set
            {
                _muted = value;
                OnUpdateMute?.Invoke(value);
            }
        }
        
        public bool Deafened
        {
            get => _deafened;
            private set
            {
                _deafened = value;
                OnUpdateDeafen?.Invoke(value);
            }
        }

        //Privates
        private readonly VoiceCraftClient _voiceCraftClient = new();
        private IAudioRecorder? _audioRecorder;
        private IAudioPlayer? _audioPlayer;

        public void Start()
        {
            try
            {
                _voiceCraftClient.OnConnected += ClientOnConnected;
                _voiceCraftClient.OnDisconnected += ClientOnDisconnected;

                _voiceCraftClient.Connect(ip, port, LoginType.Login);
                Title = Locales.Locales.VoiceCraft_Status_Title;
                Description = Locales.Locales.VoiceCraft_Status_Initializing;

                _audioRecorder = audioService.CreateAudioRecorder();
                _audioRecorder.WaveFormat = VoiceCraftClient.WaveFormat;
                _audioRecorder.DataAvailable += DataAvailable;
                _audioRecorder.StartRecording();

                Title = Locales.Locales.VoiceCraft_Status_Title;
                Description = Locales.Locales.VoiceCraft_Status_Connecting;

                IsStarted = true;
                while (!TokenSource.Token.IsCancellationRequested)
                {
                    _voiceCraftClient.Update(); //Update all networking processes.
                    Task.Delay(20).GetAwaiter().GetResult(); //Same update tick rate as the server.
                }

                if (_voiceCraftClient.ConnectionState != ConnectionState.Disconnected)
                    _voiceCraftClient.Disconnect();

                _voiceCraftClient.OnConnected -= ClientOnConnected;
                _voiceCraftClient.OnDisconnected -= ClientOnDisconnected;
            }
            catch
            {
                IsStarted = true;
                throw;
            }
        }

        public void ToggleMute()
        {
            Muted = !Muted;
        }

        public void ToggleDeafen()
        {
            Deafened = !Deafened;
        }

        public void Disconnect()
        {
            _voiceCraftClient.Disconnect();
        }
        
        public void Dispose()
        {
            _voiceCraftClient.Dispose();
            if (_audioRecorder?.CaptureState != CaptureState.Stopped)
                _audioRecorder?.StopRecording();
            _audioRecorder?.Dispose();
            _audioRecorder = null;

            if (_audioPlayer?.PlaybackState != PlaybackState.Stopped)
                _audioPlayer?.Stop();
            _audioPlayer?.Dispose();
            _audioPlayer = null;
            GC.SuppressFinalize(this);
        }
        
        private void ClientOnConnected()
        {
            Title = Locales.Locales.VoiceCraft_Status_Title;
            Description = Locales.Locales.VoiceCraft_Status_Connected;
            Dispatcher.UIThread.Invoke(() => OnConnected?.Invoke());
        }
        
        private void ClientOnDisconnected(DisconnectInfo obj)
        {
            TokenSource.Cancel(); //Cancel the thread.
            Title = Locales.Locales.VoiceCraft_Status_Title;
            Description = $"{Locales.Locales.VoiceCraft_Status_Disconnected} {obj.Reason}";
            Dispatcher.UIThread.Invoke(() =>
            {
                notificationService.SendNotification($"{Locales.Locales.VoiceCraft_Status_Disconnected} {obj.Reason}");
                OnDisconnected?.Invoke(obj);
            });
        }
        
        private void DataAvailable(object? sender, WaveInEventArgs e)
        {
            _voiceCraftClient.Write(e.Buffer, 0, e.BytesRecorded);
        }
    }
}