using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
        public bool Running { get; private set; }
        public ConnectionState ConnectionState => _voiceCraftClient.ConnectionState;

        public string Title
        {
            get => _title;
            private set
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

        //Client
        private readonly VoiceCraftClient _voiceCraftClient = new();
        private readonly Dictionary<VoiceCraftEntity, EntityViewModel> _entityViewModels = new();

        //Audio
        private IAudioRecorder? _audioRecorder;
        private IAudioPlayer? _audioPlayer;
        private IEchoCanceler? _echoCanceler;
        private IAutomaticGainController? _gainController;
        private IDenoiser? _denoiser;

        //Displays
        private string _title = string.Empty;
        private string _description = string.Empty;
        private bool _muted;
        private bool _deafened;
        
        private bool _stop;

        public void Start(CancellationToken token)
        {
            Running = true;

            try
            {
                Title = Locales.Locales.VoiceCraft_Status_Initializing;

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
                _audioRecorder.OnDataAvailable += Write;

                //Setup audio player.
                _audioPlayer = audioService.CreateAudioPlayer(Constants.SampleRate, Constants.Channels, Constants.Format);
                _audioPlayer.BufferMilliseconds = Constants.FrameSizeMs;
                _audioPlayer.SelectedDevice = audioSettings.OutputDevice == "Default" ? null : audioSettings.OutputDevice;

                //Setup Preprocessors
                _echoCanceler = audioService.GetEchoCanceler(audioSettings.EchoCanceler)?.Instantiate();
                _gainController = audioService.GetAutomaticGainController(audioSettings.AutomaticGainController)?.Instantiate();
                _denoiser = audioService.GetDenoiser(audioSettings.Denoiser)?.Instantiate();
                
                //Initialize and start.
                _audioRecorder.Initialize();
                _audioPlayer.Initialize(Read);
                _echoCanceler?.Initialize(_audioRecorder, _audioPlayer);
                _gainController?.Initialize(_audioRecorder);
                _denoiser?.Initialize(_audioRecorder);
                _audioRecorder.Start();
                _audioPlayer.Play();

                _voiceCraftClient.Connect(ip, port, LoginType.Login);
                Title = Locales.Locales.VoiceCraft_Status_Connecting;

                var startTime = DateTime.UtcNow;
                while (!token.IsCancellationRequested && !_stop)
                {
                    _voiceCraftClient.Update(); //Update all networking processes.
                    var dist = DateTime.UtcNow - startTime;
                    var delay = Constants.FrameSizeMs - dist.TotalMilliseconds;
                    if (delay > 0)
                        Task.Delay((int)delay, token).GetAwaiter().GetResult();
                    startTime = DateTime.UtcNow;
                }

                if (_voiceCraftClient.ConnectionState != ConnectionState.Disconnected)
                    _voiceCraftClient.Disconnect();

                _audioRecorder.OnDataAvailable -= Write;
                _voiceCraftClient.OnConnected -= ClientOnConnected;
                _voiceCraftClient.OnDisconnected -= ClientOnDisconnected;
                _voiceCraftClient.World.OnEntityCreated -= ClientWorldOnEntityCreated;
                _voiceCraftClient.World.OnEntityDestroyed -= ClientWorldOnEntityDestroyed;
            }
            catch (Exception ex)
            {
                notificationService.SendErrorNotification($"Voip Background Error: {ex.Message}");
                OnDisconnected?.Invoke(ex.Message);
                throw;
            }
            finally
            {
                Running = false;
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
            _audioRecorder?.Dispose();
            _audioRecorder = null;
            _audioPlayer?.Dispose();
            _audioPlayer = null;
            _echoCanceler?.Dispose();
            _echoCanceler = null;
            _gainController?.Dispose();
            _gainController = null;
            _denoiser?.Dispose();
            _denoiser = null;
            GC.SuppressFinalize(this);
        }

        private void ClientOnConnected()
        {
            Title = Locales.Locales.VoiceCraft_Status_Connected;
            OnConnected?.Invoke();
        }

        private void ClientOnDisconnected(string reason)
        {
            _stop = true;
            Title = $"{Locales.Locales.VoiceCraft_Status_Disconnected} {reason}";
            notificationService.SendNotification($"{Locales.Locales.VoiceCraft_Status_Disconnected} {reason}");
            OnDisconnected?.Invoke(reason);
        }

        private void ClientWorldOnEntityCreated(VoiceCraftEntity entity)
        {
            var entityViewModel = new EntityViewModel(entity);
            if (!_entityViewModels.TryAdd(entity, entityViewModel)) return;
            OnEntityAdded?.Invoke(entityViewModel);
        }

        private void ClientWorldOnEntityDestroyed(VoiceCraftEntity entity)
        {
            if (!_entityViewModels.Remove(entity, out var entityViewModel)) return;
            OnEntityRemoved?.Invoke(entityViewModel);
        }

        private void ClientOnSetTitle(string title)
        {
            Title = title;
        }

        private int Read(byte[] buffer, int offset, int count)
        {
            var read = _voiceCraftClient.Read(buffer, offset, count);
            _echoCanceler?.EchoPlayback(buffer, read);
            return read;
        }

        private void Write(byte[] buffer, int bytesRead)
        {
            _echoCanceler?.EchoCancel(buffer, bytesRead);
            _gainController?.Process(buffer);
            _denoiser?.Denoise(buffer);
            _voiceCraftClient.Write(buffer, bytesRead);
        }
    }
}