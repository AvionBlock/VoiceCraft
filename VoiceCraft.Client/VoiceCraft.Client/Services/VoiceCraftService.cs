using System;
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

public class VoiceCraftService(
    VoiceCraftClient client,
    AudioService audioService,
    SettingsService settingsService,
    NotificationService notificationService)
{
    private McWssServer? _mcWssServer;
    private string _title = string.Empty;
    private string _description = string.Empty;

    //Audio
    //private AudioPlayer? _audioPlayer;
    //private AudioRecorder? _audioRecorder;
    private IAudioClipper? _audioClipper;

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
            var inputSettings = settingsService.InputSettings;
            var outputSettings = settingsService.OutputSettings;
            var networkSettings = settingsService.NetworkSettings;

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

            //_audioRecorder = StartAudioRecorder(inputSettings.InputDevice);
            //_audioPlayer = StartAudioPlayer(outputSettings.OutputDevice);
            _audioClipper = audioService.GetAudioClipper(outputSettings.AudioClipper)?.Instantiate();

            //Start.
            _mcWssServer?.Start(networkSettings.McWssListenIp, networkSettings.McWssHostPort);

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

        /*
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
        */

        if (_mcWssServer != null)
        {
            StopMcWssServer(_mcWssServer);
            _mcWssServer.Dispose();
            _mcWssServer = null;
        }
    }

    //Audio
    private void Write(Span<float> buffer)
    {
        client.Write(buffer);
    }

    private int Read(Span<float> buffer)
    {
        var read = client.Read(buffer);
        if (_audioClipper != null)
            read = _audioClipper.Read(buffer[..read]);
        read = SampleVolume.Read(buffer[..read], client.OutputVolume);
        return read;
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

        //_audioRecorder?.StartRecording(client.Write); //Try restart recorder.
    }

    private void OnPlaybackStopped(Exception? ex)
    {
        if (ex != null)
        {
            client.DisconnectAsync(ex.Message);
            return;
        }

        //_audioPlayer?.StartPlaying(client.Read); //Restart player.
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

    //Setup Functions.
    /*
    private AudioRecorder StartAudioRecorder(string inputDevice)
    {
        var recorder = new AudioRecorder(Constants.SampleRate, Constants.FrameSize, Constants.RecordingChannels);
        recorder.SelectedDevice = inputDevice;
        recorder.OnRecordingStopped += OnRecordingStopped;
        recorder.StartRecording(Write);
        return recorder;
    }

    private AudioPlayer StartAudioPlayer(string outputDevice)
    {
        var player = new AudioPlayer(Constants.SampleRate, Constants.FrameSize, Constants.PlaybackChannels);
        player.SelectedDevice = outputDevice;
        player.OnPlaybackStopped += OnPlaybackStopped;
        player.StartPlaying(Read);
        return player;
    }
    */

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
            notificationService.SendNotification(Localizer.Get($"VoiceCraft.Status.Disconnected:{reason}"),
                Localizer.Get("Notification.VoiceCraft.Badge"));
            OnDisconnected?.Invoke();
        }
    }

    /*
    private void StopAudioRecorder(AudioRecorder recorder)
    {
        try
        {
            recorder.OnRecordingStopped -= OnRecordingStopped;
            recorder.StopRecording();
        }
        catch (Exception ex)
        {
            LogService.Log(ex);
        }
    }

    private void StopAudioPlayer(AudioPlayer player)
    {
        try
        {
            player.OnPlaybackStopped -= OnPlaybackStopped;
            player.StopPlaying();
        }
        catch (Exception ex)
        {
            LogService.Log(ex);
        }
    }
    */

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