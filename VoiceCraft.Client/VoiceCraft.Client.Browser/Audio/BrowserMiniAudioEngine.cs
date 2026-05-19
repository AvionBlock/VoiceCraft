using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using SoundFlow.Abstracts;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Backends.MiniAudio.Devices;
using SoundFlow.Enums;
using SoundFlow.Structs;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Browser.Audio;

public sealed class BrowserMiniAudioEngine : AudioEngine
{
    private static readonly NativeDataFormat[] DefaultFormats =
    [
        new() { Format = SampleFormat.F32, Channels = 1, SampleRate = 48_000, Flags = 0 },
        new() { Format = SampleFormat.F32, Channels = 2, SampleRate = 48_000, Flags = 0 }
    ];

    internal static readonly DeviceInfo DefaultPlaybackDevice = new()
    {
        Id = 1,
        Name = "Default",
        IsDefault = true,
        SupportedDataFormats = DefaultFormats
    };

    internal static readonly DeviceInfo DefaultCaptureDevice = new()
    {
        Id = 2,
        Name = "Default",
        IsDefault = true,
        SupportedDataFormats = DefaultFormats
    };

    private readonly List<IDisposable> _activeDevices = [];

    public BrowserMiniAudioEngine()
    {
        UpdateAudioDevicesInfo();
    }

    public override AudioPlaybackDevice InitializePlaybackDevice(
        DeviceInfo? deviceInfo,
        AudioFormat format,
        DeviceConfig? config = null)
    {
        var device = new BrowserAudioPlaybackDevice(this, deviceInfo ?? DefaultPlaybackDevice, format, config);
        _activeDevices.Add(device);
        device.OnDisposed += OnDeviceDisposed;
        return device;
    }

    public override AudioCaptureDevice InitializeCaptureDevice(
        DeviceInfo? deviceInfo,
        AudioFormat format,
        DeviceConfig? config = null)
    {
        var device = new BrowserAudioCaptureDevice(this, deviceInfo ?? DefaultCaptureDevice, format, config);
        _activeDevices.Add(device);
        device.OnDisposed += OnDeviceDisposed;
        return device;
    }

    public override FullDuplexDevice InitializeFullDuplexDevice(
        DeviceInfo? playbackDeviceInfo,
        DeviceInfo? captureDeviceInfo,
        AudioFormat format,
        DeviceConfig? config = null)
    {
        throw new NotSupportedException("Full duplex mode is not supported by the browser audio engine.");
    }

    public override AudioCaptureDevice InitializeLoopbackDevice(AudioFormat format, DeviceConfig? config = null)
    {
        throw new NotSupportedException("Loopback capture is not supported by the browser audio engine.");
    }

    public override AudioPlaybackDevice SwitchDevice(
        AudioPlaybackDevice oldDevice,
        DeviceInfo newDeviceInfo,
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
        DeviceInfo newDeviceInfo,
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
        DeviceInfo? newPlaybackInfo,
        DeviceInfo? newCaptureInfo,
        DeviceConfig? config = null)
    {
        throw new NotSupportedException("Full duplex mode is not supported by the browser audio engine.");
    }

    public override void UpdateAudioDevicesInfo()
    {
        PlaybackDevices = ReadDevices(JsAudio.GetOutputDevices(), DefaultPlaybackDevice);
        CaptureDevices = ReadDevices(JsAudio.GetInputDevices(), DefaultCaptureDevice);
    }

    protected override void CleanupBackend()
    {
        foreach (var device in _activeDevices.ToArray())
            device.Dispose();

        _activeDevices.Clear();
    }

    private static DeviceInfo[] ReadDevices(string json, DeviceInfo defaultDevice)
    {
        var devices = new List<DeviceInfo> { defaultDevice };

        try
        {
            var browserDevices = JsonSerializer.Deserialize(json, BrowserAudioJsonContext.Default.BrowserDeviceArray) ?? [];
            var nextId = defaultDevice.Id + 1;
            foreach (var device in browserDevices)
            {
                if (string.IsNullOrWhiteSpace(device.DeviceId))
                    continue;

                devices.Add(new DeviceInfo
                {
                    Id = nextId++,
                    Name = device.DeviceId,
                    IsDefault = false,
                    SupportedDataFormats = DefaultFormats
                });
            }
        }
        catch (Exception ex)
        {
            LogService.Log(ex);
        }

        return devices.ToArray();
    }

    private void OnDeviceDisposed(object? sender, EventArgs _)
    {
        if (sender is IDisposable device)
            _activeDevices.Remove(device);
    }
}

internal sealed class BrowserAudioCaptureDevice : AudioCaptureDevice
{
    private const int DefaultFrameSize = 1024;
    private readonly Lock _lock = new();
    private readonly string _deviceId;
    private readonly float[] _buffer;
    private readonly int _frameSize;
    private CancellationTokenSource? _cts;

    public BrowserAudioCaptureDevice(AudioEngine engine, DeviceInfo deviceInfo, AudioFormat format, DeviceConfig? config)
        : base(engine, format, config ?? new MiniAudioDeviceConfig())
    {
        Capability = Capability.Record;
        Info = deviceInfo;
        _deviceId = deviceInfo.IsDefault ? string.Empty : deviceInfo.Name;
        _frameSize = BrowserAudioBuffer.NormalizeFrameSize(config is MiniAudioDeviceConfig { PeriodSizeInFrames: > 0 } deviceConfig
            ? (int)deviceConfig.PeriodSizeInFrames
            : DefaultFrameSize);
        _buffer = new float[Math.Max(1, format.Channels) * _frameSize];
    }

    public override void Start()
    {
        lock (_lock)
        {
            if (IsDisposed || IsRunning)
                return;

            JsAudio.StartCapture(Format.SampleRate, Format.Channels, _frameSize, _deviceId);
            IsRunning = true;
            _cts = new CancellationTokenSource();
            _ = CaptureLoopAsync(_cts.Token);
            Engine.RaiseDeviceStarted(this);
        }
    }

    public override void Stop()
    {
        lock (_lock)
        {
            if (IsDisposed || !IsRunning)
                return;

            IsRunning = false;
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            JsAudio.StopCapture();
            Engine.RaiseDeviceStopped(this);
        }
    }

    public override void Dispose()
    {
        lock (_lock)
        {
            if (IsDisposed) return;
            Stop();
            IsDisposed = true;
            OnDisposedHandler();
        }
    }

    private async Task CaptureLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var samplesJson = JsAudio.PollCapture();
                while (!string.IsNullOrEmpty(samplesJson))
                {
                    var samples = JsonSerializer.Deserialize(samplesJson, BrowserAudioJsonContext.Default.SingleArray) ?? [];
                    Array.Clear(_buffer);
                    samples.AsSpan(0, Math.Min(samples.Length, _buffer.Length)).CopyTo(_buffer);
                    InvokeOnAudioProcessed(_buffer);
                    samplesJson = JsAudio.PollCapture();
                }

                await Task.Delay(5, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during Stop().
        }
        catch (Exception ex)
        {
            LogService.Log(ex);
            Stop();
        }
    }
}

internal sealed class BrowserAudioPlaybackDevice : AudioPlaybackDevice
{
    private const int DefaultFrameSize = 1024;
    private readonly Lock _lock = new();
    private readonly string _deviceId;
    private readonly float[] _buffer;
    private readonly int _frameSize;
    private CancellationTokenSource? _cts;

    public BrowserAudioPlaybackDevice(AudioEngine engine, DeviceInfo deviceInfo, AudioFormat format, DeviceConfig? config)
        : base(engine, format, config ?? new MiniAudioDeviceConfig())
    {
        Capability = Capability.Playback;
        Info = deviceInfo;
        _deviceId = deviceInfo.IsDefault ? string.Empty : deviceInfo.Name;
        _frameSize = BrowserAudioBuffer.NormalizeFrameSize(config is MiniAudioDeviceConfig { PeriodSizeInFrames: > 0 } deviceConfig
            ? (int)deviceConfig.PeriodSizeInFrames
            : DefaultFrameSize);
        _buffer = new float[Math.Max(1, format.Channels) * _frameSize];
    }

    public override void Start()
    {
        lock (_lock)
        {
            if (IsDisposed || IsRunning)
                return;

            JsAudio.StartPlayback(Format.SampleRate, Format.Channels, _frameSize, _deviceId);
            IsRunning = true;
            _cts = new CancellationTokenSource();
            _ = PlaybackLoopAsync(_cts.Token);
            Engine.RaiseDeviceStarted(this);
        }
    }

    public override void Stop()
    {
        lock (_lock)
        {
            if (IsDisposed || !IsRunning)
                return;

            IsRunning = false;
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            JsAudio.StopPlayback();
            Engine.RaiseDeviceStopped(this);
        }
    }

    public override void Dispose()
    {
        lock (_lock)
        {
            if (IsDisposed) return;
            Stop();
            MasterMixer.Dispose();
            IsDisposed = true;
            OnDisposedHandler();
        }
    }

    private async Task PlaybackLoopAsync(CancellationToken cancellationToken)
    {
        var delayMs = Math.Max(1, (int)Math.Round(_frameSize * 1000d / Math.Max(1, Format.SampleRate)));

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Array.Clear(_buffer);
                var soloed = Engine.GetSoloedComponent();
                if (soloed != null)
                    soloed.Process(_buffer.AsSpan(), Format.Channels);
                else
                    MasterMixer.Process(_buffer.AsSpan(), Format.Channels);

                JsAudio.EnqueuePlayback(JsonSerializer.Serialize(_buffer, BrowserAudioJsonContext.Default.SingleArray));
                Engine.RaiseAudioFramesRendered(CachedRenderEventArgs);
                await Task.Delay(delayMs, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected during Stop().
        }
        catch (Exception ex)
        {
            LogService.Log(ex);
            Stop();
        }
    }
}

internal static class BrowserAudioBuffer
{
    public static int NormalizeFrameSize(int frameSize)
    {
        var clamped = Math.Clamp(frameSize, 256, 16_384);
        if (IsPowerOfTwo(clamped))
            return clamped;

        var value = 256;
        while (value < clamped && value < 16_384)
            value <<= 1;

        return value;
    }

    private static bool IsPowerOfTwo(int value)
    {
        return value > 0 && (value & (value - 1)) == 0;
    }
}

internal sealed class BrowserDevice
{
    public string DeviceId { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}

[JsonSerializable(typeof(BrowserDevice[]))]
[JsonSerializable(typeof(float[]))]
internal partial class BrowserAudioJsonContext : JsonSerializerContext;

internal static partial class JsAudio
{
    [JSImport("getInputDevices", "audio.js")]
    internal static partial string GetInputDevices();

    [JSImport("getOutputDevices", "audio.js")]
    internal static partial string GetOutputDevices();

    [JSImport("startCapture", "audio.js")]
    internal static partial void StartCapture(int sampleRate, int channels, int frameSize, string deviceId);

    [JSImport("pollCapture", "audio.js")]
    internal static partial string PollCapture();

    [JSImport("stopCapture", "audio.js")]
    internal static partial void StopCapture();

    [JSImport("startPlayback", "audio.js")]
    internal static partial void StartPlayback(int sampleRate, int channels, int frameSize, string deviceId);

    [JSImport("enqueuePlayback", "audio.js")]
    internal static partial void EnqueuePlayback(string samplesJson);

    [JSImport("stopPlayback", "audio.js")]
    internal static partial void StopPlayback();
}
