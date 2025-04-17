using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using LiteNetLib;
using VoiceCraft.Client.Network;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.ViewModels.Data;
using VoiceCraft.Core;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Core.Network;

namespace VoiceCraft.Client.Processes
{
    public class VoipBackgroundProcess(string ip, int port, NotificationService notificationService, AudioService audioService, SettingsService settingsService)
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
        public BackgroundProcessStatus Status { get; set; }
        public CancellationTokenSource TokenSource { get; } = new();
        public ConnectionState ConnectionState => _voiceCraftClient.ConnectionState;

        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                Dispatcher.UIThread.Invoke(() => OnUpdateTitle?.Invoke(value));
            }
        }

        public string Description
        {
            get => _description;
            private set
            {
                _description = value;
                Dispatcher.UIThread.Invoke(() => OnUpdateDescription?.Invoke(value));
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

        //Client
        private readonly VoiceCraftClient _voiceCraftClient = new();

        private readonly Dictionary<VoiceCraftEntity, EntityViewModel> _entityViewModels = new();

        //Audio
        private IAudioRecorder? _audioRecorder;
        private IAudioPlayer? _audioPlayer;

        //Displays
        private string _title = string.Empty;
        private string _description = string.Empty;
        private bool _muted;
        private bool _deafened;

        public void Start(CancellationToken token)
        {
            try
            {
                Title = Locales.Locales.VoiceCraft_Status_Title;
                Description = Locales.Locales.VoiceCraft_Status_Initializing;

                var audioSettings = settingsService.AudioSettings;

                _voiceCraftClient.MicrophoneSensitivity = audioSettings.MicrophoneSensitivity;
                _voiceCraftClient.OnConnected += ClientOnConnected;
                _voiceCraftClient.OnDisconnected += ClientOnDisconnected;
                _voiceCraftClient.World.OnEntityCreated += ClientWorldOnEntityCreated;
                _voiceCraftClient.World.OnEntityDestroyed += ClientWorldOnEntityDestroyed;
                _voiceCraftClient.NetworkSystem.OnSetTitle += ClientOnSetTitle;

                //Setup audio recorder.
                _audioRecorder = audioService.CreateAudioRecorder(Constants.SampleRate, Constants.Channels, Constants.Format);
                _audioRecorder.BufferMilliseconds = Constants.FrameSizeMs;
                _audioRecorder.SelectedDevice = audioSettings.InputDevice == "Default" ? null : audioSettings.InputDevice;
                _audioRecorder.OnDataAvailable += OnDataAvailable;

                //Setup audio player.
                _audioPlayer = audioService.CreateAudioPlayer(Constants.SampleRate, Constants.Channels, Constants.Format);
                _audioPlayer.BufferMilliseconds = Constants.FrameSizeMs;
                _audioPlayer.SelectedDevice = audioSettings.OutputDevice == "Default" ? null : audioSettings.OutputDevice;

                //Start audio stuff.
                _audioRecorder.Initialize();
                _audioPlayer.Initialize(_voiceCraftClient.Read);
                _audioRecorder.Start();
                _audioPlayer.Play();

                _voiceCraftClient.Connect(ip, port, LoginType.Login);
                Title = Locales.Locales.VoiceCraft_Status_Title;
                Description = Locales.Locales.VoiceCraft_Status_Connecting;

                Status = BackgroundProcessStatus.Started;
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

                _audioRecorder.OnDataAvailable -= OnDataAvailable;
                _voiceCraftClient.OnConnected -= ClientOnConnected;
                _voiceCraftClient.OnDisconnected -= ClientOnDisconnected;
                _voiceCraftClient.World.OnEntityCreated -= ClientWorldOnEntityCreated;
                _voiceCraftClient.World.OnEntityDestroyed -= ClientWorldOnEntityDestroyed;
                Status = BackgroundProcessStatus.Completed;
            }
            catch(Exception ex)
            {
                Status = BackgroundProcessStatus.Error;
                Dispatcher.UIThread.Invoke(() =>
                {
                    notificationService.SendErrorNotification($"Voip Background Error: {ex.Message}");
                    OnDisconnected?.Invoke(ex.Message);
                });
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
                _audioRecorder?.Stop();
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
            if (!_entityViewModels.TryAdd(entity, entityViewModel)) return;
            Dispatcher.UIThread.Invoke(() => OnEntityAdded?.Invoke(entityViewModel));
        }

        private void ClientWorldOnEntityDestroyed(VoiceCraftEntity entity)
        {
            if (!_entityViewModels.Remove(entity, out var entityViewModel)) return;
            Dispatcher.UIThread.Invoke(() => OnEntityRemoved?.Invoke(entityViewModel));
        }

        private void ClientOnSetTitle(string title)
        {
            Description = title;
        }

        private void OnDataAvailable(byte[] buffer, int bytesRead)
        {
            _voiceCraftClient.Write(buffer, bytesRead);
        }
    }
}