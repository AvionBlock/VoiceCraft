using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Android.Media;
using Android.OS;
using SoundFlow.Abstracts;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Backends.MiniAudio.Devices;
using SoundFlow.Backends.MiniAudio.Enums;
using SoundFlow.Enums;
using VoiceCraft.Core.Audio;

namespace VoiceCraft.Client.Android.Audio;

public class AndroidMiniAudioEngine : AudioEngine
{
    private readonly AudioManager _audioManager;
    private readonly List<AudioDevice> _activeDevices = [];

    public AndroidMiniAudioEngine(AudioManager audioManager)
    {
        _audioManager = audioManager;
        UpdateAudioDevicesInfo();
    }

    protected override void CleanupBackend()
    {
        foreach (var device in _activeDevices.ToList())
        {
            device.Dispose();
        }

        _activeDevices.Clear();
    }

    public override AudioPlaybackDevice InitializePlaybackDevice(
        SoundFlow.Structs.DeviceInfo? deviceInfo,
        SoundFlow.Structs.AudioFormat format,
        DeviceConfig? config = null)
    {
        if (config != null && config is not MiniAudioDeviceConfig)
            throw new ArgumentException($"config must be of type {typeof(MiniAudioDeviceConfig)}");
        config ??= new MiniAudioDeviceConfig();

        var device = new AndroidAudioPlaybackDevice(_audioManager, this, deviceInfo, format, config);
        _activeDevices.Add(device);
        device.OnDisposed += OnDeviceDisposing;
        return device;
    }

    public override AudioCaptureDevice InitializeCaptureDevice(
        SoundFlow.Structs.DeviceInfo? deviceInfo,
        SoundFlow.Structs.AudioFormat format,
        DeviceConfig? config = null)
    {
        if (config != null && config is not MiniAudioDeviceConfig)
            throw new ArgumentException($"config must be of type {typeof(MiniAudioDeviceConfig)}");
        config ??= new MiniAudioDeviceConfig();

        var device = new AndroidAudioCaptureDevice(_audioManager, this, deviceInfo, format, config);
        _activeDevices.Add(device);
        device.OnDisposed += OnDeviceDisposing;
        return device;
    }

    public override FullDuplexDevice InitializeFullDuplexDevice(
        SoundFlow.Structs.DeviceInfo? playbackDeviceInfo,
        SoundFlow.Structs.DeviceInfo? captureDeviceInfo,
        SoundFlow.Structs.AudioFormat format, DeviceConfig? config = null)
    {
        throw new NotSupportedException("Full duplex mode is not supported by the Android audio engine.");
    }

    public override AudioCaptureDevice InitializeLoopbackDevice(
        SoundFlow.Structs.AudioFormat format,
        DeviceConfig? config = null)
    {
        throw new NotSupportedException("Loopback capture is not supported on Android.");
    }

    public override AudioPlaybackDevice SwitchDevice(
        AudioPlaybackDevice oldDevice,
        SoundFlow.Structs.DeviceInfo newDeviceInfo,
        DeviceConfig? config = null)
    {
        var shouldRun = oldDevice.IsRunning;
        oldDevice.Stop();
        oldDevice.Dispose();

        var next = InitializePlaybackDevice(newDeviceInfo, oldDevice.Format, config);
        if (shouldRun)
            next.Start();
        return next;
    }

    public override AudioCaptureDevice SwitchDevice(
        AudioCaptureDevice oldDevice,
        SoundFlow.Structs.DeviceInfo newDeviceInfo,
        DeviceConfig? config = null)
    {
        var shouldRun = oldDevice.IsRunning;
        oldDevice.Stop();
        oldDevice.Dispose();

        var next = InitializeCaptureDevice(newDeviceInfo, oldDevice.Format, config);
        if (shouldRun)
            next.Start();
        return next;
    }

    public override FullDuplexDevice SwitchDevice(
        FullDuplexDevice oldDevice,
        SoundFlow.Structs.DeviceInfo? newPlaybackInfo,
        SoundFlow.Structs.DeviceInfo? newCaptureInfo,
        DeviceConfig? config = null)
    {
        throw new NotSupportedException("Full duplex mode is not supported by the Android audio engine.");
    }

    public sealed override void UpdateAudioDevicesInfo()
    {
        UpdatePlaybackDevices();
        UpdateCaptureDevices();
    }

    private void UpdatePlaybackDevices()
    {
        var androidPlaybackDevices = _audioManager.GetDevices(GetDevicesTargets.Outputs);
        if (androidPlaybackDevices != null)
        {
            PlaybackDevices = androidPlaybackDevices.Select(device => new SoundFlow.Structs.DeviceInfo()
                { Name = $"{device.ProductName} - {device.Type}", Id = device.Id, IsDefault = false }).ToArray();
        }
        else
        {
            PlaybackDevices = [];
        }
    }

    private void UpdateCaptureDevices()
    {
        var androidCaptureDevices = _audioManager.GetDevices(GetDevicesTargets.Inputs);
        if (androidCaptureDevices != null)
        {
            CaptureDevices = androidCaptureDevices.Select(device => new SoundFlow.Structs.DeviceInfo()
                { Name = $"{device.ProductName} - {device.Type}", Id = device.Id, IsDefault = false }).ToArray();
        }
        else
        {
            CaptureDevices = [];
        }
    }

    private void OnDeviceDisposing(object? sender, EventArgs e)
    {
        if (sender is AudioDevice device)
        {
            _activeDevices.Remove(device);
        }
    }
}

internal sealed class AndroidAudioCaptureDevice : AudioCaptureDevice
{
    private readonly Lock _lock = new();
    private readonly AudioRecord _nativeRecorder;
    private readonly float[] _buffer;
    private readonly short[] _pcm16Buffer;
    private readonly bool _readPcm16;

    public AndroidAudioCaptureDevice(
        AudioManager audioManager,
        AudioEngine engine,
        SoundFlow.Structs.DeviceInfo? deviceInfo,
        SoundFlow.Structs.AudioFormat format,
        DeviceConfig config) : base(engine, format, config)
    {
        Capability = Capability.Record;
        Info = deviceInfo;

        var deviceConfig = config as MiniAudioDeviceConfig;
        var periodFrames = deviceConfig?.PeriodSizeInFrames switch
        {
            > 0 => deviceConfig.PeriodSizeInFrames,
            _ => 960u
        };
        var source = deviceConfig?.AAudio?.InputPreset switch
        {
            AAudioInputPreset.Camcorder => AudioSource.Camcorder,
            AAudioInputPreset.VoiceRecognition => AudioSource.VoiceRecognition,
            AAudioInputPreset.VoiceCommunication => AudioSource.VoiceCommunication,
            _ => AudioSource.Default
        };
        var channelMask = Format.Channels switch
        {
            1 => ChannelIn.Mono,
            2 => ChannelIn.Stereo,
            _ => throw new NotSupportedException()
        };
        var periods = deviceConfig?.Periods > 0 ? deviceConfig.Periods : 3;
        var bufferSize = periodFrames * format.Channels;
        _buffer = new float[bufferSize];
        _readPcm16 = IsGoogleDevice();
        _pcm16Buffer = _readPcm16 ? new short[bufferSize] : [];

        _nativeRecorder = new AudioRecord(
            source,
            format.SampleRate,
            channelMask,
            _readPcm16 ? Encoding.Pcm16bit : Encoding.PcmFloat,
            (int)(bufferSize * (_readPcm16 ? sizeof(short) : sizeof(float)) * periods));
        var device = audioManager.GetDevices(GetDevicesTargets.Inputs)
            ?.FirstOrDefault(device => device.Id == deviceInfo?.Id);
        _nativeRecorder.SetPreferredDevice(device);
    }

    public override void Start()
    {
        lock (_lock)
        {
            if (IsDisposed || IsRunning)
                return;

            _nativeRecorder.StartRecording();
            Task.Run(RecordingLogic);
            IsRunning = true;
        }
    }

    public override void Stop()
    {
        lock (_lock)
        {
            if (IsDisposed || !IsRunning)
                return;

            if (_nativeRecorder is { RecordingState: RecordState.Recording, State: State.Initialized })
                _nativeRecorder.Stop();

            IsRunning = false;
        }
    }

    public override void Dispose()
    {
        lock (_lock)
        {
            if (IsDisposed) return;
            OnDisposedHandler();
            _nativeRecorder.Dispose();
            IsDisposed = true;
        }
    }

    private void RecordingLogic()
    {
        while (_nativeRecorder is { RecordingState: RecordState.Recording, State: State.Initialized })
        {
            Array.Clear(_buffer, 0, _buffer.Length);
            var read = 0;
            try
            {
                lock (_lock)
                {
                    if (_readPcm16)
                    {
                        read = _nativeRecorder.Read(_pcm16Buffer, 0, _pcm16Buffer.Length, 0);
                        if (read > 0)
                            Sample16ToFloat.Read(_pcm16Buffer.AsSpan(0, read), _buffer.AsSpan(0, read));
                    }
                    else
                    {
                        read = _nativeRecorder.Read(_buffer, 0, _buffer.Length, 0);
                    }
                }
            }
            catch (Exception)
            {
                //Do Nothing
            }

            if (read <= 0)
                continue;
            
            InvokeOnAudioProcessed(_buffer);
        }

        Stop();
    }

    private static bool IsGoogleDevice()
    {
        var manufacturer = Build.Manufacturer ?? string.Empty;
        var brand = Build.Brand ?? string.Empty;
        return manufacturer.Equals("Google", StringComparison.OrdinalIgnoreCase) ||
               brand.Equals("google", StringComparison.OrdinalIgnoreCase);
    }
}

internal sealed class AndroidAudioPlaybackDevice : AudioPlaybackDevice
{
    private delegate void SoundComponentProcessDelegate(SoundComponent component, Span<float> outputBuffer,
        int channels);

    private static readonly SoundComponentProcessDelegate ProcessComponent = BuildProcessDelegate();

    private readonly Lock _lock = new();
    private readonly AudioTrack _nativePlayer;
    private readonly float[] _buffer;

    public AndroidAudioPlaybackDevice(
        AudioManager audioManager,
        AudioEngine engine,
        SoundFlow.Structs.DeviceInfo? deviceInfo,
        SoundFlow.Structs.AudioFormat format,
        DeviceConfig config) : base(engine, format, config)
    {
        Capability = Capability.Playback;
        Info = deviceInfo;

        var deviceConfig = config as MiniAudioDeviceConfig;
        var periodFrames = deviceConfig?.PeriodSizeInFrames switch
        {
            > 0 => deviceConfig.PeriodSizeInFrames,
            _ => 960u
        };
        var usage = deviceConfig?.AAudio?.Usage switch
        {
            AAudioUsage.Media => AudioUsageKind.Media,
            AAudioUsage.VoiceCommunication => AudioUsageKind.VoiceCommunication,
            AAudioUsage.VoiceCommunicationSignalling => AudioUsageKind.VoiceCommunicationSignalling,
            AAudioUsage.Alarm => AudioUsageKind.Alarm,
            AAudioUsage.Notification => AudioUsageKind.Notification,
            AAudioUsage.NotificationRingtone => AudioUsageKind.NotificationRingtone,
            AAudioUsage.NotificationEvent => AudioUsageKind.NotificationEvent,
            AAudioUsage.AssistanceAccessibility => AudioUsageKind.AssistanceAccessibility,
            AAudioUsage.AssistanceNavigationGuidance => AudioUsageKind.AssistanceNavigationGuidance,
            AAudioUsage.AssistanceSonification => AudioUsageKind.AssistanceSonification,
            AAudioUsage.Game => AudioUsageKind.Game,
            _ => AudioUsageKind.Unknown
        };
        var content = deviceConfig?.AAudio?.ContentType switch
        {
            AAudioContentType.Speech => AudioContentType.Speech,
            AAudioContentType.Music => AudioContentType.Music,
            AAudioContentType.Movie => AudioContentType.Movie,
            AAudioContentType.Sonification => AudioContentType.Sonification,
            _ => AudioContentType.Unknown
        };
        var channelMask = Format.Channels switch
        {
            1 => ChannelOut.Mono,
            2 => ChannelOut.Stereo,
            _ => throw new NotSupportedException()
        };
        var periods = deviceConfig?.Periods > 0 ? deviceConfig.Periods : 3;
        var bufferSize = periodFrames * format.Channels;
        _buffer = new float[bufferSize];

        var attributes = new AudioAttributes.Builder()
            .SetUsage(usage)!
            .SetContentType(content)!
            .Build()!;
        var audioFormat = new AudioFormat.Builder()
            .SetEncoding(Encoding.PcmFloat)!
            .SetSampleRate(Format.SampleRate)!
            .SetChannelMask(channelMask)
            .Build()!;

        _nativePlayer = new AudioTrack.Builder()
            .SetAudioAttributes(attributes)
            .SetAudioFormat(audioFormat)
            .SetBufferSizeInBytes((int)(bufferSize * sizeof(float) * periods))
            .SetTransferMode(AudioTrackMode.Stream)
            .Build();
        _nativePlayer.SetVolume(1.0f);

        var device = audioManager.GetDevices(GetDevicesTargets.Outputs)
            ?.FirstOrDefault(device => device.Id == deviceInfo?.Id);
        _nativePlayer.SetPreferredDevice(device);
    }

    public override void Start()
    {
        lock (_lock)
        {
            if (IsDisposed || IsRunning)
                return;

            _nativePlayer.Play();
            Task.Run(PlaybackLogic);
            IsRunning = true;
        }
    }

    public override void Stop()
    {
        lock (_lock)
        {
            if (IsDisposed || !IsRunning)
                return;

            if (_nativePlayer is { PlayState: PlayState.Playing, State: AudioTrackState.Initialized })
            {
                _nativePlayer.Pause();
                _nativePlayer.Flush();
                _nativePlayer.Stop();
            }

            IsRunning = false;
        }
    }

    public override void Dispose()
    {
        lock (_lock)
        {
            if (IsDisposed) return;
            OnDisposedHandler();
            _nativePlayer.Dispose();
            IsDisposed = true;
        }
    }

    private void PlaybackLogic()
    {
        while (_nativePlayer is { PlayState: PlayState.Playing, State: AudioTrackState.Initialized })
        {
            Array.Clear(_buffer, 0, _buffer.Length);
            // Process the audio graph
            var soloed = Engine.GetSoloedComponent();
            if (soloed != null)
                ProcessComponent(soloed, _buffer, Format.Channels);
            else
                ProcessComponent(MasterMixer, _buffer, Format.Channels);

            try
            {
                lock (_lock)
                    _nativePlayer.Write(_buffer, 0, _buffer.Length, WriteMode.Blocking);
            }
            catch (Exception)
            {
                //Do Nothing
            }
        }

        Stop();
    }

    private static SoundComponentProcessDelegate BuildProcessDelegate()
    {
        var method = typeof(SoundComponent).GetMethod("Process", BindingFlags.Instance | BindingFlags.NonPublic);
        return method == null
            ? throw new InvalidOperationException("SoundFlow process method not found.")
            : method.CreateDelegate<SoundComponentProcessDelegate>();
    }
}
