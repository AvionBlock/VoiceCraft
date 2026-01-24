using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
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

public class VoiceCraftService(
    PermissionsService permissionsService,
    VoiceCraftClient client,
    AudioService audioService,
    SettingsService settingsService,
    NotificationService notificationService)
{
    private McWssServer? _mcWssServer;
    private string _title = string.Empty;
    private string _description = string.Empty;

    //Audio
    private IAudioRecorder? _audioRecorder;
    private IAudioPlayer? _audioPlayer;
    private IDenoiser? _denoiser;
    private IEchoCanceler? _echoCanceler;
    private IAutomaticGainController? _gainController;

    public VcConnectionState ConnectionState => client.ConnectionState;

    public bool Muted
    {
        get => client.Muted;
        set => client.Muted = value;
    }

    public bool Deafened
    {
        get => client.Deafened;
        set => client.Deafened = value;
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

    public async Task ConnectAsync(string ip, int port)
    {
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
            var audioSettings = settingsService.AudioSettings;
            var networkSettings = settingsService.NetworkSettings;

            //Setup Client
            client.MicrophoneSensitivity = audioSettings.MicrophoneSensitivity;
            client.OutputVolume = audioSettings.OutputVolume;

            _audioRecorder = InitializeAudioRecorder(audioSettings.InputDevice);
            _audioPlayer = InitializeAudioPlayer(audioSettings.OutputDevice);
            _echoCanceler = audioService.GetEchoCanceler(audioSettings.EchoCanceler)?.Instantiate();
            _gainController = audioService.GetAutomaticGainController(audioSettings.AutomaticGainController)
                ?.Instantiate();
            _denoiser = audioService.GetDenoiser(audioSettings.Denoiser)?.Instantiate();

            //Setup McWss Server
            if (networkSettings.PositioningType == PositioningType.Client)
            {
                _mcWssServer = InitializeMcWssServer(client);
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
            var result = client.ConnectAsync(ip, port,
                settingsService.UserGuid,
                settingsService.ServerUserGuid,
                localeSettings.Culture,
                networkSettings.PositioningType);
            _ = Task.Run(() => UpdateLogic(client));
            await result;
        }
        catch (Exception ex)
        {
            DisconnectAsync(ex.Message).GetAwaiter().GetResult();
        }
    }

    public async Task DisconnectAsync(string? reason = null)
    {
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
        var shortBufferSpan = MemoryMarshal.Cast<byte, short>(buffer);
        var floatBuffer = ArrayPool<float>.Shared.Rent(shortBufferSpan.Length);
        var floatBufferSpan = floatBuffer.AsSpan(0, shortBufferSpan.Length);
        floatBufferSpan.Clear();

        try
        {
            var read = Sample16ToFloat.Read(shortBufferSpan, floatBufferSpan);
            read = client.Read(floatBufferSpan[..read]);
            read = SampleFloatTo16.Read(floatBufferSpan[..read], shortBufferSpan);
            if (read >= shortBufferSpan.Length) return read * sizeof(short);
            shortBufferSpan[read..].Clear();
            return count;
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
            client.Write(floatBufferSpan[..read]);
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
            client.DisconnectAsync(ex.Message);
            return;
        }

        _audioRecorder?.Start(); //Try restart recorder.
    }

    private void OnPlaybackStopped(Exception? ex)
    {
        if (ex != null)
        {
            client.DisconnectAsync(ex.Message);
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
            var localeReason = Localizer.Get($"VoiceCraft.Status.Disconnected:{reason}");
            Title = localeReason;
            Description = localeReason;
            notificationService.SendNotification(localeReason, Localizer.Get("Notification.VoiceCraft.Badge"));
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