using System;
using System.Buffers;
using System.Threading;
using SoundFlow.Abstracts;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Backends.MiniAudio.Devices;
using SoundFlow.Enums;
using SoundFlow.Structs;

namespace VoiceCraft.Client.iOS.Audio;

public sealed class IosMiniAudioEngine : AudioEngine
{
    private static readonly NativeDataFormat[] DefaultFormats =
    [
        new() { Format = SampleFormat.F32, Channels = 1, SampleRate = 48_000, Flags = 0 },
        new() { Format = SampleFormat.F32, Channels = 2, SampleRate = 48_000, Flags = 0 }
    ];

    private static readonly DeviceInfo DefaultPlaybackDevice = new()
    {
        Id = 1,
        Name = "iOS Default Output",
        IsDefault = true,
        SupportedDataFormats = DefaultFormats
    };

    private static readonly DeviceInfo DefaultCaptureDevice = new()
    {
        Id = 2,
        Name = "iOS Default Input",
        IsDefault = true,
        SupportedDataFormats = DefaultFormats
    };

    public IosMiniAudioEngine()
    {
        UpdateAudioDevicesInfo();
    }

    protected override void CleanupBackend()
    {
    }

    public override AudioPlaybackDevice InitializePlaybackDevice(DeviceInfo? deviceInfo, AudioFormat format, DeviceConfig? config = null)
    {
        return new IosFallbackAudioPlaybackDevice(this, deviceInfo ?? DefaultPlaybackDevice, format, config ?? new MiniAudioDeviceConfig());
    }

    public override AudioCaptureDevice InitializeCaptureDevice(DeviceInfo? deviceInfo, AudioFormat format, DeviceConfig? config = null)
    {
        return new IosFallbackAudioCaptureDevice(this, deviceInfo ?? DefaultCaptureDevice, format, config ?? new MiniAudioDeviceConfig());
    }

    public override FullDuplexDevice InitializeFullDuplexDevice(DeviceInfo? playbackDeviceInfo, DeviceInfo? captureDeviceInfo, AudioFormat format, DeviceConfig? config = null)
    {
        throw new NotSupportedException("Full duplex mode is not supported by the iOS fallback audio engine.");
    }

    public override AudioCaptureDevice InitializeLoopbackDevice(AudioFormat format, DeviceConfig? config = null)
    {
        return InitializeCaptureDevice(DefaultCaptureDevice, format, config);
    }

    public override AudioPlaybackDevice SwitchDevice(AudioPlaybackDevice oldDevice, DeviceInfo newDeviceInfo, DeviceConfig? config = null)
    {
        var shouldRun = oldDevice.IsRunning;
        oldDevice.Stop();
        oldDevice.Dispose();

        var next = InitializePlaybackDevice(newDeviceInfo, oldDevice.Format, config);
        if (shouldRun)
            next.Start();
        return next;
    }

    public override AudioCaptureDevice SwitchDevice(AudioCaptureDevice oldDevice, DeviceInfo newDeviceInfo, DeviceConfig? config = null)
    {
        var shouldRun = oldDevice.IsRunning;
        oldDevice.Stop();
        oldDevice.Dispose();

        var next = InitializeCaptureDevice(newDeviceInfo, oldDevice.Format, config);
        if (shouldRun)
            next.Start();
        return next;
    }

    public override FullDuplexDevice SwitchDevice(FullDuplexDevice oldDevice, DeviceInfo? newPlaybackInfo, DeviceInfo? newCaptureInfo, DeviceConfig? config = null)
    {
        throw new NotSupportedException("Full duplex mode is not supported by the iOS fallback audio engine.");
    }

    public override void UpdateAudioDevicesInfo()
    {
        PlaybackDevices = [DefaultPlaybackDevice];
        CaptureDevices = [DefaultCaptureDevice];
    }
}

internal sealed class IosFallbackAudioCaptureDevice : AudioCaptureDevice
{
    private Timer? _timer;
    private readonly int _periodMs;
    private readonly int _frameSamples;

    public IosFallbackAudioCaptureDevice(AudioEngine engine, DeviceInfo deviceInfo, AudioFormat format, DeviceConfig config)
        : base(engine, format, config)
    {
        Capability = Capability.Record;
        Info = deviceInfo;

        var channels = Math.Max(1, format.Channels);
        var frames = config is MiniAudioDeviceConfig deviceConfig
            ? deviceConfig.PeriodSizeInFrames
            : 960u;
        _frameSamples = (int)(Math.Max(1u, frames) * (uint)channels);
        _periodMs = Math.Max(5, (int)Math.Round((double)Math.Max(1u, frames) / Math.Max(1, format.SampleRate) * 1000d));
    }

    public override void Start()
    {
        if (IsDisposed || IsRunning)
            return;

        _timer = new Timer(Tick, null, 0, _periodMs);
        IsRunning = true;
    }

    public override void Stop()
    {
        if (!IsRunning)
            return;

        _timer?.Dispose();
        _timer = null;
        IsRunning = false;
    }

    public override void Dispose()
    {
        if (IsDisposed)
            return;

        Stop();
        IsDisposed = true;
        OnDisposedHandler();
    }

    private void Tick(object? _)
    {
        if (!IsRunning || IsDisposed)
            return;

        var rented = ArrayPool<float>.Shared.Rent(_frameSamples);
        try
        {
            var span = rented.AsSpan(0, _frameSamples);
            span.Clear();
            InvokeOnAudioProcessed(span);
        }
        finally
        {
            ArrayPool<float>.Shared.Return(rented);
        }
    }
}

internal sealed class IosFallbackAudioPlaybackDevice : AudioPlaybackDevice
{
    public IosFallbackAudioPlaybackDevice(AudioEngine engine, DeviceInfo deviceInfo, AudioFormat format, DeviceConfig config)
        : base(engine, format, config)
    {
        Capability = Capability.Playback;
        Info = deviceInfo;
    }

    public override void Start()
    {
        if (IsDisposed || IsRunning)
            return;

        IsRunning = true;
    }

    public override void Stop()
    {
        if (!IsRunning)
            return;

        IsRunning = false;
    }

    public override void Dispose()
    {
        if (IsDisposed)
            return;

        Stop();
        MasterMixer.Dispose();
        IsDisposed = true;
        OnDisposedHandler();
    }
}
