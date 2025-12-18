using System;
using System.Collections.Generic;
using System.Threading;
using VoiceCraft.Client.Network;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.ViewModels.Data;
using VoiceCraft.Core;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Core.Locales;
using VoiceCraft.Core.World;

namespace VoiceCraft.Client.Processes;

public class VoipBackgroundProcess(
    string ip,
    int port,
    string locale,
    NotificationService notificationService,
    AudioService audioService,
    SettingsService settingsService)
    : IBackgroundProcess
{
    private readonly Dictionary<VoiceCraftEntity, EntityViewModel> _entityViewModels = new();

    //Client
    private readonly VoiceCraftClient _voiceCraftClient = new();
    private IAudioPlayer? _audioPlayer;

    //Audio
    private IAudioRecorder? _audioRecorder;
    private IDenoiser? _denoiser;
    private string _description = string.Empty;
    private bool _disconnected;
    private string _disconnectReason = "VoiceCraft.DisconnectReason.Error";
    private IEchoCanceler? _echoCanceler;
    private IAutomaticGainController? _gainController;

    private bool _stopping;
    private bool _stopRequested;

    //Displays
    private string _title = string.Empty;

    //Public Variables
    public bool HasEnded { get; private set; }

    public VcConnectionState ConnectionState => _voiceCraftClient.ConnectionState;

    public bool Muted => _voiceCraftClient.Muted;

    public bool Deafened => _voiceCraftClient.Deafened;

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

    //Events
    public event Action<string>? OnUpdateTitle;
    public event Action<string>? OnUpdateDescription;

    public void Start(CancellationToken token)
    {
        try
        {
            Title = "VoiceCraft.Status.Initializing";
            var audioSettings = settingsService.AudioSettings;

            _voiceCraftClient.MicrophoneSensitivity = audioSettings.MicrophoneSensitivity;
            _voiceCraftClient.OnConnected += ClientOnConnected;
            _voiceCraftClient.OnDisconnected += ClientOnDisconnected;
            _voiceCraftClient.OnSetTitle += ClientOnSetTitle;
            _voiceCraftClient.OnSetDescription += ClientOnSetDescription;
            _voiceCraftClient.OnMuteUpdated += ClientOnMuteUpdated;
            _voiceCraftClient.OnDeafenUpdated += ClientOnDeafenUpdated;
            _voiceCraftClient.OnSpeakingUpdated += ClientOnSpeakingUpdated;
            _voiceCraftClient.World.OnEntityCreated += ClientWorldOnEntityCreated;
            _voiceCraftClient.World.OnEntityDestroyed += ClientWorldOnEntityDestroyed;

            //Setup audio recorder.
            _audioRecorder =
                audioService.CreateAudioRecorder(Constants.SampleRate, Constants.Channels, Constants.Format);
            _audioRecorder.BufferMilliseconds = Constants.FrameSizeMs;
            _audioRecorder.SelectedDevice = audioSettings.InputDevice == "Default" ? null : audioSettings.InputDevice;
            _audioRecorder.OnDataAvailable += Write;
            _audioRecorder.OnRecordingStopped += OnRecordingStopped;

            //Setup audio player.
            _audioPlayer = audioService.CreateAudioPlayer(Constants.SampleRate, 2, Constants.Format);
            _audioPlayer.BufferMilliseconds = 100;
            _audioPlayer.SelectedDevice = audioSettings.OutputDevice == "Default" ? null : audioSettings.OutputDevice;
            _audioPlayer.OnPlaybackStopped += OnPlaybackStopped;

            //Setup Preprocessors
            _echoCanceler = audioService.GetEchoCanceler(audioSettings.EchoCanceler)?.Instantiate();
            _gainController = audioService.GetAutomaticGainController(audioSettings.AutomaticGainController)
                ?.Instantiate();
            _denoiser = audioService.GetDenoiser(audioSettings.Denoiser)?.Instantiate();

            //Initialize and start.
            _audioRecorder.Initialize();
            _audioPlayer.Initialize(Read);
            _echoCanceler?.Initialize(_audioRecorder, _audioPlayer);
            _gainController?.Initialize(_audioRecorder);
            _denoiser?.Initialize(_audioRecorder);
            _audioRecorder.Start();
            _audioPlayer.Play();

            while (_audioRecorder.CaptureState == CaptureState.Starting ||
                   _audioPlayer.PlaybackState == PlaybackState.Starting)
            {
                if (_stopRequested)
                    return;
                Thread.Sleep(1); //Don't burn the CPU.
            }

            _ = _voiceCraftClient.ConnectAsync(settingsService.UserGuid, settingsService.ServerUserGuid, ip, port,
                locale);
            Title = "VoiceCraft.Status.Connecting";

            var startTime = DateTime.UtcNow;
            while (!_disconnected)
            {
                if ((token.IsCancellationRequested && !_stopping) || (_stopRequested && !_stopping))
                {
                    _stopping = true;
                    _voiceCraftClient.Disconnect();
                }

                _voiceCraftClient.Update(); //Update all networking processes.
                var dist = DateTime.UtcNow - startTime;
                var delay = Constants.TickRate - dist.TotalMilliseconds;
                if (delay > 0)
                    Thread.Sleep((int)delay);
                startTime = DateTime.UtcNow;
            }
        }
        catch (Exception ex)
        {
            notificationService.SendErrorNotification(
                Localizer.Get($"Notification.Error.VoipBackgroundError:{ex.Message}"));
            _disconnectReason = "VoiceCraft.DisconnectReason.Error";
            throw;
        }
        finally
        {
            HasEnded = true;
            OnDisconnected?.Invoke();
            var localeReason = Localizer.Get($"VoiceCraft.Status.Disconnected:{_disconnectReason}");
            Title = localeReason;
            Description = localeReason;
            notificationService.SendNotification(localeReason, Localizer.Get("Notification.VoiceCraft.Badge"));

            if (_audioRecorder != null)
            {
                _audioRecorder.OnDataAvailable -= Write;
                _audioRecorder.OnRecordingStopped -= OnRecordingStopped;
                if (_audioRecorder.CaptureState == CaptureState.Capturing)
                    _audioRecorder.Stop();
            }

            if (_audioPlayer != null)
            {
                _audioPlayer.OnPlaybackStopped -= OnPlaybackStopped;
                if (_audioPlayer.PlaybackState == PlaybackState.Playing)
                    _audioPlayer.Stop();
            }

            _voiceCraftClient.OnConnected -= ClientOnConnected;
            _voiceCraftClient.OnDisconnected -= ClientOnDisconnected;
            _voiceCraftClient.OnSetTitle -= ClientOnSetTitle;
            _voiceCraftClient.OnSetDescription -= ClientOnSetDescription;
            _voiceCraftClient.OnMuteUpdated -= ClientOnMuteUpdated;
            _voiceCraftClient.OnDeafenUpdated -= ClientOnDeafenUpdated;
            _voiceCraftClient.OnSpeakingUpdated -= ClientOnSpeakingUpdated;
            _voiceCraftClient.World.OnEntityCreated -= ClientWorldOnEntityCreated;
            _voiceCraftClient.World.OnEntityDestroyed -= ClientWorldOnEntityDestroyed;
        }
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

    public event Action? OnConnected;
    public event Action? OnDisconnected;
    public event Action<bool>? OnUpdateMute;
    public event Action<bool>? OnUpdateDeafen;
    public event Action<bool>? OnUpdateSpeaking;
    public event Action<EntityViewModel>? OnEntityAdded;
    public event Action<EntityViewModel>? OnEntityRemoved;

    public void ToggleMute(bool value)
    {
        _voiceCraftClient.Muted = value;
    }

    public void ToggleDeafen(bool value)
    {
        _voiceCraftClient.Deafened = value;
    }

    public void Disconnect()
    {
        _stopRequested = true;
    }

    private void ClientOnConnected()
    {
        Title = "VoiceCraft.Status.Connected";
        OnConnected?.Invoke();
    }

    private void ClientOnDisconnected(string reason)
    {
        _disconnectReason = reason;
        _disconnected = true;
    }

    private void ClientOnMuteUpdated(bool mute, VoiceCraftEntity entity)
    {
        OnUpdateMute?.Invoke(mute);
    }

    private void ClientOnDeafenUpdated(bool deafen, VoiceCraftEntity entity)
    {
        OnUpdateDeafen?.Invoke(deafen);
    }

    private void ClientOnSpeakingUpdated(bool speaking)
    {
        OnUpdateSpeaking?.Invoke(speaking);
    }

    private void ClientWorldOnEntityCreated(VoiceCraftEntity entity)
    {
        if (entity is not VoiceCraftClientEntity clientEntity) return;
        var entityViewModel = new EntityViewModel(clientEntity, settingsService);
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

    private void ClientOnSetDescription(string description)
    {
        Description = description;
    }

    private int Read(byte[] buffer, int count)
    {
        var read = _voiceCraftClient.Read(buffer, count);
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

    private void OnRecordingStopped(Exception? ex)
    {
        if (ex != null)
        {
            _disconnectReason = ex.Message;
            _stopRequested = true;
            return;
        }

        _audioRecorder?.Start(); //Try restart recorder.
    }

    private void OnPlaybackStopped(Exception? ex)
    {
        if (ex != null)
        {
            _disconnectReason = ex.Message;
            _stopRequested = true;
            return;
        }

        _audioPlayer?.Play(); //Restart player.
    }
}