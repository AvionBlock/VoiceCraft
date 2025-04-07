using System;
using System.Collections.Generic;
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
using VoiceCraft.Client.ViewModels.Data;
using VoiceCraft.Core;
using VoiceCraft.Core.Network;

namespace VoiceCraft.Client.Processes
{
    public class VoipBackgroundProcess(string ip, int port, NotificationService notificationService, AudioService audioService)
        : IBackgroundProcess
    {
        //Events
        public event Action<string>? OnUpdateTitle;
        public event Action<string>? OnUpdateDescription;
        public event Action<bool>? OnUpdateMute;
        public event Action<bool>? OnUpdateDeafen;
        public event Action? OnConnected;
        public event Action<string>? OnDisconnected;
        public event Action<EntityViewModel>? OnEntityAdded;
        public event Action<EntityViewModel>? OnEntityRemoved;
        
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
        private readonly Dictionary<VoiceCraftEntity, EntityViewModel> _entityViewModels = new();
        private IAudioRecorder? _audioRecorder;
        private IAudioPlayer? _audioPlayer;
        private string _title = string.Empty;
        private string _description = string.Empty;
        private bool _muted;
        private bool _deafened;

        public void Start()
        {
            try
            {
                _voiceCraftClient.OnConnected += ClientOnConnected;
                _voiceCraftClient.OnDisconnected += ClientOnDisconnected;
                _voiceCraftClient.World.OnEntityCreated += ClientWorldOnEntityCreated;
                _voiceCraftClient.World.OnEntityDestroyed += ClientWorldOnEntityDestroyed;
                _voiceCraftClient.NetworkSystem.OnSetTitle += ClientOnSetTitle;

                _voiceCraftClient.Connect(ip, port, LoginType.Login);
                Title = Locales.Locales.VoiceCraft_Status_Title;
                Description = Locales.Locales.VoiceCraft_Status_Initializing;

                _audioRecorder = audioService.CreateAudioRecorder();
                _audioRecorder.WaveFormat = VoiceCraftClient.RecordWaveFormat;
                _audioRecorder.BufferMilliseconds = Constants.FrameSizeMs;
                _audioRecorder.DataAvailable += DataAvailable;
                _audioRecorder.StartRecording();
                
                _audioPlayer = audioService.CreateAudioPlayer();
                _audioPlayer.Init(_voiceCraftClient);
                _audioPlayer.Play();

                Title = Locales.Locales.VoiceCraft_Status_Title;
                Description = Locales.Locales.VoiceCraft_Status_Connecting;

                IsStarted = true;
                var startTime = DateTime.UtcNow;
                while (!TokenSource.Token.IsCancellationRequested)
                {
                    _voiceCraftClient.Update(); //Update all networking processes.
                    var dist = DateTime.UtcNow - startTime;
                    var delay = Constants.FrameSizeMs - dist.TotalMilliseconds;
                    if (delay > 0)
                        Task.Delay((int)delay).GetAwaiter().GetResult();
                    startTime = DateTime.UtcNow;
                }

                if (_voiceCraftClient.ConnectionState != ConnectionState.Disconnected)
                    _voiceCraftClient.Disconnect();

                _audioRecorder.DataAvailable -= DataAvailable;
                _voiceCraftClient.OnConnected -= ClientOnConnected;
                _voiceCraftClient.OnDisconnected -= ClientOnDisconnected;
                _voiceCraftClient.World.OnEntityCreated -= ClientWorldOnEntityCreated;
                _voiceCraftClient.World.OnEntityDestroyed -= ClientWorldOnEntityDestroyed;
            }
            catch
            {
                IsStarted = false; //I don't know why I left this as true.
                throw;
            }
        }

        public void ToggleMute()
        {
            Muted = !Muted;
            _voiceCraftClient.Muted = Muted;
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
        
        private void ClientOnDisconnected(string reason)
        {
            TokenSource.Cancel(); //Cancel the thread.
            Title = Locales.Locales.VoiceCraft_Status_Title;
            Description = $"{Locales.Locales.VoiceCraft_Status_Disconnected} {reason}";
            Dispatcher.UIThread.Invoke(() =>
            {
                notificationService.SendNotification($"{Locales.Locales.VoiceCraft_Status_Disconnected} {reason}");
                OnDisconnected?.Invoke(reason);
            });
        }
        
        private void ClientWorldOnEntityCreated(VoiceCraftEntity entity)
        {
            var entityViewModel = new EntityViewModel(entity);
            if(!_entityViewModels.TryAdd(entity, entityViewModel)) return;
            OnEntityAdded?.Invoke(entityViewModel);
        }
        
        private void ClientWorldOnEntityDestroyed(VoiceCraftEntity entity)
        {
            if(!_entityViewModels.Remove(entity, out var entityViewModel)) return;
            OnEntityRemoved?.Invoke(entityViewModel);
        }

        private void ClientOnSetTitle(string title)
        {
            Description = title;
        }
        
        private void DataAvailable(object? sender, WaveInEventArgs e)
        {
            _voiceCraftClient.Write(e.Buffer, 0, e.BytesRecorded);
        }
    }
}