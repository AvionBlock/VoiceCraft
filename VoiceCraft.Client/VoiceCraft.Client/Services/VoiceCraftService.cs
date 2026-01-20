using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using VoiceCraft.Core;
using VoiceCraft.Core.Audio;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Core.Locales;
using VoiceCraft.Core.World;
using VoiceCraft.Network;
using VoiceCraft.Client.Servers;
using VoiceCraft.Network.Clients;
using VoiceCraft.Network.World;

namespace VoiceCraft.Client.Services;

public class VoiceCraftService
{
    private readonly VoiceCraftClient _client;
    private McWssServer? _mcWssServer;
    private string _title = string.Empty;
    private string _description = string.Empty;

    //Services
    private readonly NotificationService _notificationService;
    private readonly SettingsService _settingsService;
    private readonly AudioService _audioService;

    //Audio
    private IAudioRecorder? _audioRecorder;
    private IAudioPlayer? _audioPlayer;
    private IDenoiser? _denoiser;
    private IEchoCanceler? _echoCanceler;
    private IAutomaticGainController? _gainController;

    public VcConnectionState ConnectionState => _client.ConnectionState;

    public bool Muted
    {
        get => _client.Muted;
        set => _client.Muted = value;
    }

    public bool Deafened
    {
        get => _client.Deafened;
        set => _client.Deafened = value;
    }

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

    public VoiceCraftService(
        VoiceCraftClient client,
        AudioService audioService,
        SettingsService settingsService,
        NotificationService notificationService)
    {
        _client = client;
        _settingsService = settingsService;
        _audioService = audioService;
        _notificationService = notificationService;

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
    }

    public async Task ConnectAsync(string ip, int port, Guid userGuid, Guid serverUserGuid, string locale,
        PositioningType positioningType)
    {
        Title = "VoiceCraft.Status.Initializing";
        var audioSettings = _settingsService.AudioSettings;
        var networkSettings = _settingsService.NetworkSettings;

        _audioRecorder = InitializeAudioRecorder(audioSettings.InputDevice);
        _audioPlayer = InitializeAudioPlayer(audioSettings.OutputDevice);
        _echoCanceler = _audioService.GetEchoCanceler(audioSettings.EchoCanceler)?.Instantiate();
        _gainController = _audioService.GetAutomaticGainController(audioSettings.AutomaticGainController)
            ?.Instantiate();
        _denoiser = _audioService.GetDenoiser(audioSettings.Denoiser)?.Instantiate();

        //Setup McWss Server
        if (networkSettings.PositioningType == PositioningType.Client)
        {
            _mcWssServer = InitializeMcWssServer(_client);
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

        while (_audioRecorder.CaptureState == CaptureState.Starting ||
               _audioPlayer.PlaybackState == PlaybackState.Starting)
        {
            await Task.Delay(1);
        }

        Title = "VoiceCraft.Status.Connecting";
        await _client.ConnectAsync(ip, port, userGuid, serverUserGuid, locale, positioningType);
        _ = Task.Run(() => UpdateLogic(_client));
    }

    public async Task DisconnectAsync(string? reason = null)
    {
        await StopClientAsync(_client, reason);

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

        if (_mcWssServer != null)
        {
            StopMcWssServer(_mcWssServer);
            _mcWssServer.Dispose();
            _mcWssServer = null;
        }
    }

    //Audio
    private int Read(byte[] buffer, int count)
    {
        var shortBufferSpan = MemoryMarshal.Cast<byte, short>(buffer)[..(count / sizeof(short))];
        var floatBuffer = ArrayPool<float>.Shared.Rent(shortBufferSpan.Length);
        try
        {
            var floatBufferSpan = floatBuffer.AsSpan(0, shortBufferSpan.Length);
            floatBufferSpan.Clear();

            var read = _client.Read(floatBufferSpan);
            return SampleFloatTo16.Read(floatBufferSpan[..read], shortBufferSpan) * sizeof(short);
        }
        finally
        {
            ArrayPool<float>.Shared.Return(floatBuffer);
        }
    }

    private void Write(byte[] buffer, int bytesRead)
    {
        var shortBufferSpan = MemoryMarshal.Cast<byte, short>(buffer)[..(bytesRead / sizeof(short))];
        var floatBuffer = ArrayPool<float>.Shared.Rent(shortBufferSpan.Length);
        try
        {
            var floatBufferSpan = floatBuffer.AsSpan(0, shortBufferSpan.Length);
            floatBufferSpan.Clear();

            var read = Sample16ToFloat.Read(shortBufferSpan, floatBuffer);
            _client.Write(floatBufferSpan[..read]);
        }
        finally
        {
            ArrayPool<float>.Shared.Return(floatBuffer);
        }
    }

    private static void UpdateLogic(VoiceCraftClient client)
    {
        var startTime = DateTime.UtcNow;
        while (client.ConnectionState != VcConnectionState.Disconnected)
        {
            client.Update(); //Update all networking processes.
            var dist = DateTime.UtcNow - startTime;
            var delay = Constants.TickRate - dist.TotalMilliseconds;
            if (delay > 0)
                Thread.Sleep((int)delay);
            startTime = DateTime.UtcNow;
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

    private void OnRecordingStopped(Exception? ex)
    {
        if (ex != null)
        {
            _client.DisconnectAsync(ex.Message);
            return;
        }

        _audioRecorder?.Start(); //Try restart recorder.
    }

    private void OnPlaybackStopped(Exception? ex)
    {
        if (ex != null)
        {
            _client.DisconnectAsync(ex.Message);
            return;
        }

        _audioPlayer?.Play(); //Restart player.
    }

    private void OnMcWssConnected(string playerName)
    {
        _notificationService.SendSuccessNotification(Localizer.Get($"Notification.McWss.Connected:{playerName}"),
            Localizer.Get("Notification.McWss.Badge"));
    }

    private void OnMcWssDisconnected()
    {
        _notificationService.SendNotification(Localizer.Get("Notification.McWss.Disconnected"),
            Localizer.Get("Notification.McWss.Badge"));
    }

    private IAudioRecorder InitializeAudioRecorder(string inputDevice)
    {
        var audioRecorder =
            _audioService.CreateAudioRecorder(Constants.SampleRate, Constants.Channels, Constants.Format);
        audioRecorder.BufferMilliseconds = Constants.FrameSizeMs;
        audioRecorder.SelectedDevice = inputDevice == "Default" ? null : inputDevice;
        audioRecorder.OnDataAvailable += Write;
        audioRecorder.OnRecordingStopped += OnRecordingStopped;
        audioRecorder.Initialize();
        return audioRecorder;
    }

    private IAudioPlayer InitializeAudioPlayer(string outputDevice)
    {
        var audioPlayer = _audioService.CreateAudioPlayer(Constants.SampleRate, 2, Constants.Format);
        audioPlayer.BufferMilliseconds = 100;
        audioPlayer.SelectedDevice = outputDevice == "Default" ? null : outputDevice;
        audioPlayer.OnPlaybackStopped += OnPlaybackStopped;
        audioPlayer.Initialize(Read);
        return audioPlayer;
    }

    private McWssServer InitializeMcWssServer(VoiceCraftClient client)
    {
        var mcWssServer = new McWssServer(client);
        mcWssServer.OnConnected += OnMcWssConnected;
        mcWssServer.OnDisconnected += OnMcWssDisconnected;
        return mcWssServer;
    }

    //Stoppers
    private async Task StopClientAsync(VoiceCraftClient client, string? reason)
    {
        try
        {
            client.OnConnected -= ClientOnConnected;
            client.OnDisconnected -= ClientOnDisconnected;
            client.OnSetTitle -= ClientOnSetTitle;
            client.OnSetDescription -= ClientOnSetDescription;
            client.OnMuteUpdated -= ClientOnMuteUpdated;
            client.OnDeafenUpdated -= ClientOnDeafenUpdated;
            client.OnServerMuteUpdated -= ClientOnServerMuteUpdated;
            client.OnServerDeafenUpdated -= ClientOnServerDeafenUpdated;
            client.OnSpeakingUpdated -= ClientOnSpeakingUpdated;
            client.World.OnEntityCreated -= ClientWorldOnEntityCreated;
            client.World.OnEntityDestroyed -= ClientWorldOnEntityDestroyed;
            await client.DisconnectAsync(reason);
        }
        catch (Exception ex)
        {
            LogService.Log(ex);
        }
        finally
        {
            var localeReason = Localizer.Get($"VoiceCraft.Status.Disconnected:{reason}");
            Title = localeReason;
            Description = localeReason;
            _notificationService.SendNotification(localeReason, Localizer.Get("Notification.VoiceCraft.Badge"));
            OnDisconnected?.Invoke();
        }
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
            if (server.IsStarted)
                server.Stop();
        }
        catch (Exception ex)
        {
            LogService.Log(ex);
        }
    }
}