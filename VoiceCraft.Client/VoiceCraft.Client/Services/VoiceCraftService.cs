using System;
using System.Threading;
using System.Threading.Tasks;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Enums;
using VoiceCraft.Client.Audio;
using VoiceCraft.Client.Models.Settings;
using VoiceCraft.Core;
using VoiceCraft.Core.Audio;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Core.World;
using VoiceCraft.Network;
using VoiceCraft.Client.Servers;
using VoiceCraft.Network.Clients;
using VoiceCraft.Network.World;

namespace VoiceCraft.Client.Services;

public class VoiceCraftService(
    VoiceCraftClient client,
    AudioService audioService,
    SettingsService settingsService,
    NotificationService notificationService)
{
    private McWssServer? _mcWssServer;
    private int _disconnecting;
    private bool _pttEnabled;
    private bool _pttCue;

    //Audio
    private AudioPlaybackDevice? _audioPlayer;
    private AudioCaptureDevice? _audioRecorder;
    private CombinedAudioPreprocessor? _audioPreprocessor;
    private ToneProvider? _pttToneProvider;
    private IAudioClipper? _audioClipper;

    public VcConnectionState ConnectionState => client.ConnectionState;

    public bool Muted
    {
        get => client.Muted;
        set
        {
            if (_pttEnabled) return;
            client.Muted = value;
        }
    }

    public bool Deafened
    {
        get => client.Deafened;
        set => client.Deafened = value;
    }

    public bool PushToTalk
    {
        get;
        set
        {
            if (!_pttEnabled || field == value) return;
            field = value;
            client.Muted = !value;

            if (_pttCue)
                _pttToneProvider?.Play(TimeSpan.FromMilliseconds(80), value ? 880f : 620f);
        }
    }

    public string Title
    {
        get;
        private set
        {
            field = value;
            OnUpdateTitle?.Invoke(value);
        }
    } = string.Empty;

    public string Description
    {
        get;
        private set
        {
            field = value;
            OnUpdateDescription?.Invoke(value);
        }
    } = string.Empty;

    //Events
    public event Action? OnConnected;
    public event Action? OnDisconnected;
    public event Action<string>? OnUpdateTitle;
    public event Action<string>? OnUpdateDescription;
    public event Action<bool>? OnUpdateMute;
    public event Action<bool>? OnUpdateDeafen;
    public event Action<bool>? OnUpdateServerMute;
    public event Action<bool>? OnUpdateServerDeafen;
    public event Action<bool>? OnUpdateSpeaking;
    public event Action<VoiceCraftClientEntity>? OnEntityAdded;
    public event Action<VoiceCraftClientEntity>? OnEntityRemoved;

    public async Task ConnectAsync(string ip, int port)
    {
        if (client.ConnectionState != VcConnectionState.Disconnected) return;
        _disconnecting = 0;

        try
        {
            client.OnConnected += ClientOnConnected;
            client.OnDisconnected += ClientOnDisconnected;
            client.OnSetTitle += ClientOnSetTitle;
            client.OnSetDescription += ClientOnSetDescription;
            client.OnMuteUpdated += ClientOnMuteUpdated;
            client.OnDeafenUpdated += ClientOnDeafenUpdated;
            client.OnServerMuteUpdated += ClientOnServerMuteUpdated;
            client.OnServerDeafenUpdated += ClientOnServerDeafenUpdated;
            client.OnSpeakingUpdated += ClientOnSpeakingUpdated;
            client.World.OnEntityCreated += ClientWorldOnEntityCreated;
            client.World.OnEntityDestroyed += ClientWorldOnEntityDestroyed;

            Title = "VoiceCraft.Status.Initializing";
            Description = string.Empty;
            var localeSettings = settingsService.LocaleSettings;
            var inputSettings = settingsService.InputSettings;
            var outputSettings = settingsService.OutputSettings;
            var networkSettings = settingsService.NetworkSettings;

            _pttEnabled = inputSettings.PushToTalkEnabled;
            _pttCue = inputSettings.PushToTalkCue;

            //Setup McWss Server
            if (networkSettings.PositioningType == PositioningType.Client)
            {
                _mcWssServer = InitializeMcWssServer(client);
                Description =
                    $"VoiceCraft.DescriptionStatus.McWss:{networkSettings.McWssListenIp},{networkSettings.McWssHostPort}";
            }

            //Setup Client
            client.InputVolume = inputSettings.InputVolume;
            client.OutputVolume = outputSettings.OutputVolume;
            client.MicrophoneSensitivity = inputSettings.MicrophoneSensitivity;
            client.Muted = _pttEnabled;

            _audioRecorder = InitializeAudioRecorder(inputSettings.InputDevice, inputSettings.HardwarePreprocessorsEnabled);
            _audioPlayer = InitializeAudioPlayer(outputSettings.OutputDevice);
            _audioPreprocessor = InitializeAudioPreprocessor(inputSettings);
            _pttToneProvider = InitializeToneProvider(_audioPlayer);
            _audioClipper = audioService.GetAudioClipper(outputSettings.AudioClipper)?.Instantiate();

            //Start.
            _audioRecorder.Start();
            if (!_audioRecorder.IsRunning)
                throw new Exception("VoiceCraft.DisconnectReason.Error");
            _audioPlayer.Start();
            if (!_audioPlayer.IsRunning)
                throw new Exception("VoiceCraft.DisconnectReason.Error");
            _mcWssServer?.Start(networkSettings.McWssListenIp, networkSettings.McWssHostPort);

            Title = "VoiceCraft.Status.Connecting";
            var result = client.ConnectAsync(ip, port,
                settingsService.UserGuid,
                settingsService.ServerUserGuid,
                localeSettings.Culture,
                networkSettings.PositioningType);
            _ = Task.Run(UpdateLogicAsync);
            await result;
        }
        catch (Exception ex)
        {
            LogService.Log(ex);
            DisconnectAsync(ex.Message).GetAwaiter().GetResult();
        }
    }

    public async Task DisconnectAsync(string? reason = null)
    {
        if (Interlocked.Exchange(ref _disconnecting, 1) == 1) return;

        await StopClientAsync(client, reason);

        if (_audioRecorder != null)
        {
            StopAudioRecorder(_audioRecorder);
            _audioRecorder.Dispose();
            _audioRecorder = null;
        }

        if (_audioPlayer != null)
        {
            StopAudioPlayer(_audioPlayer);
            _audioPlayer.Dispose();
            _audioPlayer = null;
        }

        if (_audioPreprocessor != null)
        {
            _audioPreprocessor.Dispose();
            _audioPreprocessor = null;
        }

        if (_mcWssServer != null)
        {
            StopMcWssServer(_mcWssServer);
            _mcWssServer.Dispose();
            _mcWssServer = null;
        }
    }

    //Audio
    private void Write(Span<float> buffer, Capability _)
    {
        _audioPreprocessor?.Process(buffer);
        SampleVolume.Read(buffer, client.InputVolume);
        client.Write(buffer);
    }

    private int Read(Span<float> buffer)
    {
        var read = client.Read(buffer);
        if (_audioClipper != null)
            read = _audioClipper.Read(buffer[..read]);
        read = SampleVolume.Read(buffer[..read], client.OutputVolume);
        _audioPreprocessor?.ProcessPlayback(buffer);
        return read;
    }

    private async Task UpdateLogicAsync()
    {
        try
        {
            var startTime = DateTime.UtcNow;
            while (client.ConnectionState != VcConnectionState.Disconnected)
            {
                if (_audioRecorder is { IsRunning: false } ||
                    _audioPlayer is { IsRunning: false })
                    throw new Exception("VoiceCraft.DisconnectReason.Error");

                client.Update(); //Update all networking processes.
                var dist = DateTime.UtcNow - startTime;
                var delay = Constants.TickRate - dist.TotalMilliseconds;
                if (delay > 0)
                    await Task.Delay((int)delay);
                startTime = DateTime.UtcNow;
            }
        }
        catch (Exception ex)
        {
            LogService.Log(ex);
            await DisconnectAsync(ex.Message);
        }
    }

    //Event Handling
    private void ClientOnConnected()
    {
        Title = "VoiceCraft.Status.Connected";
        OnConnected?.Invoke();
    }

    private void ClientOnDisconnected(string? reason)
    {
        _ = DisconnectAsync(reason);
    }

    private void ClientOnSetTitle(string title)
    {
        Title = title;
    }

    private void ClientOnSetDescription(string description)
    {
        Description = description;
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
        OnEntityAdded?.Invoke(clientEntity);
    }

    private void ClientWorldOnEntityDestroyed(VoiceCraftEntity entity)
    {
        if (entity is not VoiceCraftClientEntity clientEntity) return;
        OnEntityRemoved?.Invoke(clientEntity);
    }

    private void OnMcWssConnected(string playerName)
    {
        notificationService.SendSuccessNotification(
            "McWss.Notification.Badge",
            $"McWss.Notification.Connected:{playerName}");
    }

    private void OnMcWssDisconnected()
    {
        notificationService.SendNotification(
            "McWss.Notification.Badge",
            "McWss.Notification.Disconnected");
    }

    //Setup Functions.
    private CombinedAudioPreprocessor InitializeAudioPreprocessor(InputSettings inputSettings)
    {
        var gainController = audioService.GetAudioPreprocessor(inputSettings.AutomaticGainController);
        var denoiser = audioService.GetAudioPreprocessor(inputSettings.Denoiser);
        var echoCanceler = audioService.GetAudioPreprocessor(inputSettings.EchoCanceler);
        return new CombinedAudioPreprocessor(gainController, denoiser, echoCanceler);
    }

    private static ToneProvider InitializeToneProvider(AudioPlaybackDevice playbackDevice)
    {
        var toneProvider = new ToneProvider(playbackDevice.Engine, playbackDevice.Format);
        playbackDevice.MasterMixer.AddComponent(toneProvider);
        return toneProvider;
    }

    private AudioCaptureDevice InitializeAudioRecorder(string inputDevice, bool hardwarePreprocessorsEnabled)
    {
        var recorder = audioService.InitializeCaptureDevice(Constants.SampleRate, Constants.RecordingChannels,
            Constants.FrameSize, inputDevice, hardwarePreprocessorsEnabled);
        recorder.OnAudioProcessed += Write;
        return recorder;
    }

    private AudioPlaybackDevice InitializeAudioPlayer(string outputDevice)
    {
        var player = audioService.InitializePlaybackDevice(Constants.SampleRate, Constants.PlaybackChannels,
            Constants.FrameSize, outputDevice);
        var callbackComponent = new CallbackProvider(player.Engine, player.Format, Read);
        player.MasterMixer.AddComponent(callbackComponent);
        return player;
    }

    private McWssServer InitializeMcWssServer(VoiceCraftClient vcClient)
    {
        var mcWssServer = new McWssServer(vcClient);
        mcWssServer.OnConnected += OnMcWssConnected;
        mcWssServer.OnDisconnected += OnMcWssDisconnected;
        return mcWssServer;
    }

    //Stoppers
    private async Task StopClientAsync(VoiceCraftClient vcClient, string? reason)
    {
        try
        {
            vcClient.OnConnected -= ClientOnConnected;
            vcClient.OnDisconnected -= ClientOnDisconnected;
            vcClient.OnSetTitle -= ClientOnSetTitle;
            vcClient.OnSetDescription -= ClientOnSetDescription;
            vcClient.OnMuteUpdated -= ClientOnMuteUpdated;
            vcClient.OnDeafenUpdated -= ClientOnDeafenUpdated;
            vcClient.OnServerMuteUpdated -= ClientOnServerMuteUpdated;
            vcClient.OnServerDeafenUpdated -= ClientOnServerDeafenUpdated;
            vcClient.OnSpeakingUpdated -= ClientOnSpeakingUpdated;
            vcClient.World.OnEntityCreated -= ClientWorldOnEntityCreated;
            vcClient.World.OnEntityDestroyed -= ClientWorldOnEntityDestroyed;
            await vcClient.DisconnectAsync(reason);
        }
        catch (Exception ex)
        {
            LogService.Log(ex);
        }
        finally
        {
            Title = $"VoiceCraft.Status.Disconnected:{reason}";
            Description = $"VoiceCraft.Status.Disconnected:{reason}";
            notificationService.SendNotification(
                "VoiceCraft.Notification.Badge",
                $"VoiceCraft.Notification.Disconnected:{reason}");
            OnDisconnected?.Invoke();
        }
    }

    private void StopAudioRecorder(AudioCaptureDevice recorder)
    {
        try
        {
            recorder.OnAudioProcessed -= Write;
            recorder.Stop();
        }
        catch (Exception ex)
        {
            LogService.Log(ex);
        }
    }

    private static void StopAudioPlayer(AudioPlaybackDevice player)
    {
        try
        {
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
            if (server.IsStarted)
                server.Stop();
        }
        catch (Exception ex)
        {
            LogService.Log(ex);
        }
    }
}
