using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using VoiceCraft.Client.Services;

namespace VoiceCraft.Client.Browser.Audio;

public sealed class BrowserAudioService : IVoiceCraftAudioService
{
    private readonly IEnumerable<RegisteredAudioPreprocessor> _preprocessors;
    private readonly IEnumerable<RegisteredAudioClipper> _clippers;

    public BrowserAudioService(
        IEnumerable<RegisteredAudioPreprocessor> preprocessors,
        IEnumerable<RegisteredAudioClipper> clippers)
    {
        _preprocessors = preprocessors;
        _clippers = clippers;
    }

    public IEnumerable<RegisteredAudioPreprocessor> RegisteredAudioPreprocessors => _preprocessors;
    public IEnumerable<RegisteredAudioClipper> RegisteredAudioClippers => _clippers;

    public RegisteredAudioPreprocessor? GetAudioPreprocessor(Guid id)
    {
        return id == Guid.Empty ? null : null;
    }

    public RegisteredAudioClipper? GetAudioClipper(Guid id)
    {
        return id == Guid.Empty ? null : null;
    }

    public IEnumerable<AudioDeviceInfo> GetInputDevices()
    {
        return ReadDevices(JsAudio.GetInputDevices(), "Default");
    }

    public IEnumerable<AudioDeviceInfo> GetOutputDevices()
    {
        return ReadDevices(JsAudio.GetOutputDevices(), "Default");
    }

    public IAudioCaptureSession InitializeCaptureSession(
        int sampleRate,
        int channels,
        uint frameSize,
        string inputDevice,
        bool hardwarePreprocessorsEnabled)
    {
        return new BrowserCaptureSession(sampleRate, channels, frameSize, inputDevice);
    }

    public IAudioPlaybackSession InitializePlaybackSession(
        int sampleRate,
        int channels,
        uint frameSize,
        string outputDevice,
        Func<Span<float>, int> read)
    {
        return new BrowserPlaybackSession(sampleRate, channels, frameSize, outputDevice, read);
    }

    public IAudioPlaybackSession InitializeTonePlaybackSession(
        int sampleRate,
        int channels,
        uint frameSize,
        string outputDevice,
        Func<Span<float>, int> read)
    {
        return new BrowserPlaybackSession(sampleRate, channels, frameSize, outputDevice, read);
    }

    private static IEnumerable<AudioDeviceInfo> ReadDevices(string json, string defaultDisplayName)
    {
        var devices = new List<AudioDeviceInfo>
        {
            new("Default", defaultDisplayName, true)
        };

        try
        {
            foreach (var device in JsonSerializer.Deserialize(json, BrowserAudioJsonContext.Default.BrowserDeviceArray) ?? [])
                devices.Add(new AudioDeviceInfo(device.DeviceId, string.IsNullOrWhiteSpace(device.Label) ? device.DeviceId : device.Label, false));
        }
        catch (Exception ex)
        {
            LogService.Log(ex);
        }

        return devices;
    }

    private sealed class BrowserCaptureSession(int sampleRate, int channels, uint frameSize, string inputDevice)
        : IAudioCaptureSession
    {
        private CancellationTokenSource? _cts;
        private readonly float[] _buffer = new float[frameSize * channels];

        public bool IsRunning { get; private set; }
        public event AudioCaptureFrameHandler? OnAudioProcessed;

        public void Start()
        {
            if (IsRunning) return;
            JsAudio.StartCapture(sampleRate, channels, (int)frameSize, inputDevice == "Default" ? string.Empty : inputDevice);
            IsRunning = true;
            _cts = new CancellationTokenSource();
            _ = PumpAsync(_cts.Token);
        }

        public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            JsAudio.StopCapture();
        }

        public void Dispose()
        {
            Stop();
        }

        private async Task PumpAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var samplesJson = JsAudio.PollCapture();
                    while (!string.IsNullOrEmpty(samplesJson))
                    {
                        var samples = JsonSerializer.Deserialize(samplesJson, BrowserAudioJsonContext.Default.SingleArray) ?? [];
                        samples.AsSpan(0, Math.Min(samples.Length, _buffer.Length)).CopyTo(_buffer);
                        OnAudioProcessed?.Invoke(_buffer);
                        samplesJson = JsAudio.PollCapture();
                    }
                }
                catch (Exception ex)
                {
                    LogService.Log(ex);
                    IsRunning = false;
                    return;
                }

                await Task.Delay(5, cancellationToken);
            }
        }
    }

    private sealed class BrowserPlaybackSession(
        int sampleRate,
        int channels,
        uint frameSize,
        string outputDevice,
        Func<Span<float>, int> read)
        : IAudioPlaybackSession
    {
        private readonly float[] _buffer = new float[frameSize * channels];

        public bool IsRunning { get; private set; }

        public void Start()
        {
            if (IsRunning) return;
            JsAudio.StartPlayback(sampleRate, channels, (int)frameSize, outputDevice == "Default" ? string.Empty : outputDevice);
            IsRunning = true;
        }

        public void Stop()
        {
            if (!IsRunning) return;
            IsRunning = false;
            JsAudio.StopPlayback();
        }

        public void Pump()
        {
            if (!IsRunning) return;
            var readCount = read(_buffer);
            if (readCount > 0)
                JsAudio.EnqueuePlayback(JsonSerializer.Serialize(_buffer[..readCount], BrowserAudioJsonContext.Default.SingleArray));
        }

        public void PlayTone(TimeSpan duration, float frequency)
        {
            JsAudio.PlayTone(duration.TotalMilliseconds, frequency);
        }

        public void Dispose()
        {
            Stop();
        }
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

    [JSImport("playTone", "audio.js")]
    internal static partial void PlayTone(double durationMs, float frequency);
}
