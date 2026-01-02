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

    //McWss
    private McWssServer? _mcWssServer;

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
            var networkSettings = settingsService.NetworkSettings;

            _voiceCraftClient.MicrophoneSensitivity = audioSettings.MicrophoneSensitivity;
            _voiceCraftClient.OutputVolume = audioSettings.OutputVolume;
            _voiceCraftClient.OnConnected += ClientOnConnected;
            _voiceCraftClient.OnDisconnected += ClientOnDisconnected;
            _voiceCraftClient.OnSetTitle += ClientOnSetTitle;
            _voiceCraftClient.OnSetDescription += ClientOnSetDescription;
            _voiceCraftClient.OnMuteUpdated += ClientOnMuteUpdated;
            _voiceCraftClient.OnDeafenUpdated += ClientOnDeafenUpdated;
            _voiceCraftClient.OnServerMuteUpdated += ClientOnServerMuteUpdated;
            _voiceCraftClient.OnServerDeafenUpdated += ClientOnServerDeafenUpdated;
            _voiceCraftClient.OnSpeakingUpdated += ClientOnSpeakingUpdated;
            _voiceCraftClient.World.OnEntityCreated += ClientWorldOnEntityCreated;
            _voiceCraftClient.World.OnEntityDestroyed += ClientWorldOnEntityDestroyed;

            //Setup audio devices.
            _audioRecorder = InitializeAudioRecorder(audioSettings.InputDevice);
            _audioPlayer = InitializeAudioPlayer(audioSettings.OutputDevice);

            //Setup Preprocessors
            _echoCanceler = audioService.GetEchoCanceler(audioSettings.EchoCanceler)?.Instantiate();
            _gainController = audioService.GetAutomaticGainController(audioSettings.AutomaticGainController)
                ?.Instantiate();
            _denoiser = audioService.GetDenoiser(audioSettings.Denoiser)?.Instantiate();

            //Setup McWss Server
            if (networkSettings.PositioningType == PositioningType.Client)
            {
                _mcWssServer = InitializeMcWssServer();
                Description =
                    $"VoiceCraft.DescriptionStatus.McWss:{networkSettings.McWssListenIp},{networkSettings.McWssHostPort}";
            }

            //Initialize and start.
            _echoCanceler?.Initialize(_audioRecorder, _audioPlayer);
            _gainController?.Initialize(_audioRecorder);
            _denoiser?.Initialize(_audioRecorder);
            _audioRecorder.Start();
            _audioPlayer.Play();
            _mcWssServer?.Start(networkSettings.McWssListenIp, networkSettings.McWssHostPort);

            var sw = new SpinWait();
            while (_audioRecorder.CaptureState == CaptureState.Starting ||
                   _audioPlayer.PlaybackState == PlaybackState.Starting ||
                   _mcWssServer is { IsStarted: false })
            {
                if (_stopRequested)
                    return;
                sw.SpinOnce();
            }

            _ = _voiceCraftClient.ConnectAsync(settingsService.UserGuid, settingsService.ServerUserGuid, ip, port,
                locale, networkSettings.PositioningType);
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
                StopAudioRecorder(_audioRecorder);
            if (_audioPlayer != null)
                StopAudioPlayer(_audioPlayer);
            if (_mcWssServer is { IsStarted: true })
                StopMcWssServer(_mcWssServer);

            _voiceCraftClient.OnConnected -= ClientOnConnected;
            _voiceCraftClient.OnDisconnected -= ClientOnDisconnected;
            _voiceCraftClient.OnSetTitle -= ClientOnSetTitle;
            _voiceCraftClient.OnSetDescription -= ClientOnSetDescription;
            _voiceCraftClient.OnMuteUpdated -= ClientOnMuteUpdated;
            _voiceCraftClient.OnDeafenUpdated -= ClientOnDeafenUpdated;
            _voiceCraftClient.OnServerMuteUpdated -= ClientOnServerMuteUpdated;
            _voiceCraftClient.OnServerDeafenUpdated -= ClientOnServerDeafenUpdated;
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
    public event Action<bool>? OnUpdateServerMute;
    public event Action<bool>? OnUpdateServerDeafen;
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

    private IAudioRecorder InitializeAudioRecorder(string inputDevice)
    {
        var audioRecorder =
            audioService.CreateAudioRecorder(Constants.SampleRate, Constants.Channels, Constants.Format);
        audioRecorder.BufferMilliseconds = Constants.FrameSizeMs;
        audioRecorder.SelectedDevice = inputDevice == "Default" ? null : inputDevice;
        audioRecorder.OnDataAvailable += Write;
        audioRecorder.OnRecordingStopped += OnRecordingStopped;
        audioRecorder.Initialize();
        return audioRecorder;
    }

    private IAudioPlayer InitializeAudioPlayer(string outputDevice)
    {
        var audioPlayer = audioService.CreateAudioPlayer(Constants.SampleRate, 2, Constants.Format);
        audioPlayer.BufferMilliseconds = 100;
        audioPlayer.SelectedDevice = outputDevice == "Default" ? null : outputDevice;
        audioPlayer.OnPlaybackStopped += OnPlaybackStopped;
        audioPlayer.Initialize(Read);
        return audioPlayer;
    }

    private McWssServer InitializeMcWssServer()
    {
        var mcWssServer = new McWssServer(_voiceCraftClient);
        mcWssServer.OnConnected += OnMcWssConnected;
        mcWssServer.OnDisconnected += OnMcWssDisconnected;
        return mcWssServer;
    }

    private void StopAudioRecorder(IAudioRecorder recorder)
    {
        try
        {
            recorder.OnDataAvailable -= Write;
            recorder.OnRecordingStopped -= OnRecordingStopped;
            if (recorder.CaptureState == CaptureState.Capturing)
                recorder.Stop();
        }
        catch (Exception ex)
        {
            LogService.Log(ex);
        }
    }

    private void StopAudioPlayer(IAudioPlayer player)
    {
        try
        {
            player.OnPlaybackStopped -= OnPlaybackStopped;
            if (player.PlaybackState == PlaybackState.Playing)
                player.Stop();
        }
        catch (Exception ex)
        {
            LogService.Log(ex);
        }
    }

    private void StopMcWssServer(McWssServer server)
    {
        try
        {
            server.OnDisconnected -= OnMcWssDisconnected;
            server.OnConnected -= OnMcWssConnected;
            server.Stop();
        }
        catch (Exception ex)
        {
            LogService.Log(ex);
        }
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

    private void ClientOnMuteUpdated(bool mute, VoiceCraftEntity _)
    {
        OnUpdateMute?.Invoke(mute);
    }

    private void ClientOnDeafenUpdated(bool deafen, VoiceCraftEntity _)
    {
        OnUpdateDeafen?.Invoke(deafen);
    }

    private void ClientOnSpeakingUpdated(bool speaking)
    {
        OnUpdateSpeaking?.Invoke(speaking);
    }

    private void ClientOnServerMuteUpdated(bool mute)
    {
        OnUpdateServerMute?.Invoke(mute);
    }

    private void ClientOnServerDeafenUpdated(bool deafen)
    {
        OnUpdateServerDeafen?.Invoke(deafen);
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

    private void OnMcWssConnected(string playerName)
    {
        notificationService.SendSuccessNotification(Localizer.Get($"Notification.McWss.Connected:{playerName}"),
            Localizer.Get("Notification.McWss.Badge"));
    }

    private void OnMcWssDisconnected()
    {
        notificationService.SendNotification(Localizer.Get("Notification.McWss.Disconnected"),
            Localizer.Get("Notification.McWss.Badge"));
    }
}