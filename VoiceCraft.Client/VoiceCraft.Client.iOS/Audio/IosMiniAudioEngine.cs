using System;
using System.Buffers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using AVFoundation;
using Foundation;
using SoundFlow.Abstracts;
using SoundFlow.Abstracts.Devices;
using SoundFlow.Backends.MiniAudio.Devices;
using SoundFlow.Components;
using SoundFlow.Enums;
using SoundFlow.Structs;
using VoiceCraft.Core;

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
        return new IosAudioPlaybackDevice(this, deviceInfo ?? DefaultPlaybackDevice, format, config ?? new MiniAudioDeviceConfig());
    }

    public override AudioCaptureDevice InitializeCaptureDevice(DeviceInfo? deviceInfo, AudioFormat format, DeviceConfig? config = null)
    {
        return new IosAudioCaptureDevice(this, deviceInfo ?? DefaultCaptureDevice, format, config ?? new MiniAudioDeviceConfig());
    }

    public override FullDuplexDevice InitializeFullDuplexDevice(DeviceInfo? playbackDeviceInfo, DeviceInfo? captureDeviceInfo, AudioFormat format, DeviceConfig? config = null)
    {
        throw new NotSupportedException("Full duplex mode is not supported by the iOS audio engine.");
    }

    public override AudioCaptureDevice InitializeLoopbackDevice(AudioFormat format, DeviceConfig? config = null)
    {
        throw new NotSupportedException("Loopback capture is not supported on iOS.");
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
        throw new NotSupportedException("Full duplex mode is not supported by the iOS audio engine.");
    }

    public override void UpdateAudioDevicesInfo()
    {
        PlaybackDevices = [DefaultPlaybackDevice];
        CaptureDevices = [DefaultCaptureDevice];
    }
}

internal sealed class IosAudioCaptureDevice : AudioCaptureDevice
{
    private AVAudioEngine? _engine;
    private AVAudioFormat? _tapFormat;
    private bool _sessionActivatedByMe;
    private readonly uint _periodFrames;
    private readonly object _captureLock = new();
    private readonly int _chunkSamples;
    private float[] _stagingBuffer;
    private int _stagingCount;

    public IosAudioCaptureDevice(AudioEngine engine, DeviceInfo deviceInfo, AudioFormat format, DeviceConfig config)
        : base(engine, format, config)
    {
        Capability = Capability.Record;
        Info = deviceInfo;

        _periodFrames = config is MiniAudioDeviceConfig deviceConfig && deviceConfig.PeriodSizeInFrames > 0
            ? deviceConfig.PeriodSizeInFrames
            : 960u;
        _chunkSamples = Constants.FrameSize * Math.Max(1, format.Channels);
        _stagingBuffer = new float[_chunkSamples * 8];
        _stagingCount = 0;
    }

    public override void Start()
    {
        if (IsDisposed || IsRunning)
            return;

        ActivateSession();

        _engine = new AVAudioEngine();
        var inputNode = _engine.InputNode;
        _tapFormat = new AVAudioFormat(
            AVAudioCommonFormat.PCMFloat32,
            Format.SampleRate,
            (uint)Math.Max(1, Format.Channels),
            false);

        inputNode.InstallTapOnBus(0, _periodFrames, _tapFormat, OnInputBuffer);
        _engine.Prepare();

        NSError? startError;
        if (!_engine.StartAndReturnError(out startError))
        {
            inputNode.RemoveTapOnBus(0);
            _engine.Dispose();
            _engine = null;
            ThrowIfError(startError, "Failed to start iOS capture engine.");
        }

        IsRunning = true;
    }

    public override void Stop()
    {
        if (!IsRunning)
            return;

        if (_engine != null)
        {
            try
            {
                _engine.InputNode.RemoveTapOnBus(0);
            }
            catch
            {
                // ignored
            }

            _engine.Stop();
            _engine.Dispose();
            _engine = null;
        }

        DeactivateSessionIfOwned();
        lock (_captureLock)
        {
            _stagingCount = 0;
        }

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

    private void OnInputBuffer(AVAudioPcmBuffer buffer, AVAudioTime _)
    {
        if (!IsRunning || IsDisposed)
            return;

        var targetChannels = Math.Max(1, Format.Channels);
        var frameCount = (int)buffer.FrameLength;
        if (frameCount <= 0)
            return;

        var sourceChannels = (int)Math.Max(1, buffer.Format.ChannelCount);
        var sourcePointers = buffer.FloatChannelData;
        if (sourcePointers == IntPtr.Zero)
            return;

        var source = new float[sourceChannels][];
        for (var ch = 0; ch < sourceChannels; ch++)
        {
            source[ch] = ArrayPool<float>.Shared.Rent(frameCount);
            Array.Clear(source[ch], 0, frameCount);

            var channelPtr = Marshal.ReadIntPtr(sourcePointers, ch * IntPtr.Size);
            if (channelPtr != IntPtr.Zero)
            {
                Marshal.Copy(channelPtr, source[ch], 0, frameCount);
            }
        }

        var outputLength = frameCount * targetChannels;
        var outputBuffer = ArrayPool<float>.Shared.Rent(outputLength);
        try
        {
            var output = outputBuffer.AsSpan(0, outputLength);
            output.Clear();

            for (var frame = 0; frame < frameCount; frame++)
            {
                if (targetChannels == 1)
                {
                    var sum = 0f;
                    for (var ch = 0; ch < sourceChannels; ch++)
                    {
                        var sample = source[ch][frame];
                        if (!float.IsFinite(sample))
                            sample = 0f;

                        sum += sample;
                    }

                    output[frame] = sum / sourceChannels;
                    continue;
                }

                if (sourceChannels == 1)
                {
                    var sample = source[0][frame];
                    if (!float.IsFinite(sample))
                        sample = 0f;

                    for (var ch = 0; ch < targetChannels; ch++)
                        output[frame * targetChannels + ch] = sample;
                    continue;
                }

                for (var ch = 0; ch < targetChannels; ch++)
                {
                    var sourceIndex = Math.Min(sourceChannels - 1, ch);
                    var sample = source[sourceIndex][frame];
                    if (!float.IsFinite(sample))
                        sample = 0f;

                    output[frame * targetChannels + ch] = sample;
                }
            }

            PushCapturedSamples(output);
        }
        finally
        {
            foreach (var channel in source)
                ArrayPool<float>.Shared.Return(channel);
            ArrayPool<float>.Shared.Return(outputBuffer);
        }
    }

    private void PushCapturedSamples(ReadOnlySpan<float> samples)
    {
        lock (_captureLock)
        {
            EnsureCapacity(_stagingCount + samples.Length);
            samples.CopyTo(_stagingBuffer.AsSpan(_stagingCount));
            _stagingCount += samples.Length;

            while (_stagingCount >= _chunkSamples)
            {
                InvokeOnAudioProcessed(_stagingBuffer.AsSpan(0, _chunkSamples));

                var remaining = _stagingCount - _chunkSamples;
                if (remaining > 0)
                    _stagingBuffer.AsSpan(_chunkSamples, remaining).CopyTo(_stagingBuffer);
                _stagingCount = remaining;
            }
        }
    }

    private void EnsureCapacity(int required)
    {
        if (required <= _stagingBuffer.Length)
            return;

        var next = new float[Math.Max(required, _stagingBuffer.Length * 2)];
        _stagingBuffer.AsSpan(0, _stagingCount).CopyTo(next);
        _stagingBuffer = next;
    }

    private void ActivateSession()
    {
        var session = AVAudioSession.SharedInstance();
        NSError? error;

        session.SetCategory(
            AVAudioSessionCategory.PlayAndRecord,
            AVAudioSessionCategoryOptions.AllowBluetooth |
            AVAudioSessionCategoryOptions.AllowBluetoothA2DP |
            AVAudioSessionCategoryOptions.DefaultToSpeaker |
            AVAudioSessionCategoryOptions.MixWithOthers,
            out error);
        ThrowIfError(error, "Failed to set iOS audio category.");

        session.SetMode(AVAudioSessionMode.VoiceChat, out error);
        ThrowIfError(error, "Failed to set iOS audio mode.");

        session.SetPreferredSampleRate(Format.SampleRate, out error);
        ThrowIfError(error, "Failed to set preferred sample rate.");

        session.SetPreferredIOBufferDuration(Constants.FrameSizeMs / 1000d, out error);
        ThrowIfError(error, "Failed to set preferred I/O buffer duration.");

        session.SetActive(true, out error);
        ThrowIfError(error, "Failed to activate iOS audio session.");

        _sessionActivatedByMe = true;
    }

    private void DeactivateSessionIfOwned()
    {
        if (!_sessionActivatedByMe)
            return;

        var session = AVAudioSession.SharedInstance();
        NSError? error;
        session.SetActive(false, AVAudioSessionSetActiveOptions.NotifyOthersOnDeactivation, out error);
        _sessionActivatedByMe = false;
    }

    private static void ThrowIfError(NSError? error, string message)
    {
        if (error == null)
            return;

        throw new InvalidOperationException($"{message} {error.LocalizedDescription} ({error.Code})");
    }
}

internal sealed class IosAudioPlaybackDevice : AudioPlaybackDevice
{
    private delegate void MixerProcessDelegate(Mixer mixer, Span<float> outputBuffer, int channels);

    private static readonly MixerProcessDelegate ProcessMixer = BuildProcessDelegate();

    private AVAudioEngine? _engine;
    private AVAudioPlayerNode? _player;
    private AVAudioFormat? _playbackFormat;
    private Timer? _timer;
    private int _pendingBuffers;
    private bool _sessionActivatedByMe;
    private readonly uint _periodFrames;

    public IosAudioPlaybackDevice(AudioEngine engine, DeviceInfo deviceInfo, AudioFormat format, DeviceConfig config)
        : base(engine, format, config)
    {
        Capability = Capability.Playback;
        Info = deviceInfo;

        _periodFrames = config is MiniAudioDeviceConfig deviceConfig && deviceConfig.PeriodSizeInFrames > 0
            ? deviceConfig.PeriodSizeInFrames
            : 960u;
    }

    public override void Start()
    {
        if (IsDisposed || IsRunning)
            return;

        ActivateSession();

        _engine = new AVAudioEngine();
        _player = new AVAudioPlayerNode();
        _playbackFormat = new AVAudioFormat(
            AVAudioCommonFormat.PCMFloat32,
            Format.SampleRate,
            (uint)Math.Max(1, Format.Channels),
            false);

        _engine.AttachNode(_player);
        _engine.Connect(_player, _engine.MainMixerNode, _playbackFormat);
        _engine.Prepare();

        NSError? startError;
        if (!_engine.StartAndReturnError(out startError))
        {
            CleanupGraph();
            ThrowIfError(startError, "Failed to start iOS playback engine.");
        }

        _player.Play();
        _pendingBuffers = 0;
        _timer = new Timer(ScheduleAudio, null, 0, Math.Max(5, (int)Math.Round(_periodFrames * 1000d / Math.Max(1, Format.SampleRate))));
        IsRunning = true;
    }

    public override void Stop()
    {
        if (!IsRunning)
            return;

        _timer?.Dispose();
        _timer = null;

        if (_player != null)
            _player.Stop();

        CleanupGraph();
        DeactivateSessionIfOwned();

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

    private void ScheduleAudio(object? _)
    {
        if (!IsRunning || IsDisposed || _player is null || _playbackFormat is null)
            return;

        if (Interlocked.CompareExchange(ref _pendingBuffers, 0, 0) >= 3)
            return;

        var channels = Math.Max(1, Format.Channels);
        var frameCount = (int)Math.Max(1u, _periodFrames);
        var totalSamples = frameCount * channels;

        var mixed = ArrayPool<float>.Shared.Rent(totalSamples);
        try
        {
            var mixSpan = mixed.AsSpan(0, totalSamples);
            mixSpan.Clear();
            ProcessMixer(MasterMixer, mixSpan, channels);

            using var pcmBuffer = new AVAudioPcmBuffer(_playbackFormat, (uint)frameCount);
            pcmBuffer.FrameLength = (uint)frameCount;

            var channelData = pcmBuffer.FloatChannelData;
            if (channelData == IntPtr.Zero)
                return;

            for (var ch = 0; ch < channels; ch++)
            {
                var channel = ArrayPool<float>.Shared.Rent(frameCount);
                try
                {
                    for (var frame = 0; frame < frameCount; frame++)
                        channel[frame] = mixSpan[frame * channels + ch];
                    var channelPtr = Marshal.ReadIntPtr(channelData, ch * IntPtr.Size);
                    if (channelPtr == IntPtr.Zero)
                        continue;

                    Marshal.Copy(channel, 0, channelPtr, frameCount);
                }
                finally
                {
                    ArrayPool<float>.Shared.Return(channel);
                }
            }

            Interlocked.Increment(ref _pendingBuffers);
            _player.ScheduleBuffer(pcmBuffer, () => Interlocked.Decrement(ref _pendingBuffers));
        }
        finally
        {
            ArrayPool<float>.Shared.Return(mixed);
        }
    }

    private static MixerProcessDelegate BuildProcessDelegate()
    {
        var method = typeof(SoundComponent).GetMethod("Process", BindingFlags.Instance | BindingFlags.NonPublic);
        if (method == null)
            throw new InvalidOperationException("SoundFlow mixer process method not found.");

        return (MixerProcessDelegate)method.CreateDelegate(typeof(MixerProcessDelegate));
    }

    private void CleanupGraph()
    {
        if (_engine != null && _player != null)
        {
            try
            {
                _engine.DisconnectNodeInput(_player);
            }
            catch
            {
                // ignored
            }
        }

        _player?.Dispose();
        _player = null;

        _engine?.Stop();
        _engine?.Dispose();
        _engine = null;
        _playbackFormat = null;
    }

    private void ActivateSession()
    {
        var session = AVAudioSession.SharedInstance();
        NSError? error;

        session.SetCategory(
            AVAudioSessionCategory.PlayAndRecord,
            AVAudioSessionCategoryOptions.AllowBluetooth |
            AVAudioSessionCategoryOptions.AllowBluetoothA2DP |
            AVAudioSessionCategoryOptions.DefaultToSpeaker |
            AVAudioSessionCategoryOptions.MixWithOthers,
            out error);
        ThrowIfError(error, "Failed to set iOS audio category.");

        session.SetMode(AVAudioSessionMode.VoiceChat, out error);
        ThrowIfError(error, "Failed to set iOS audio mode.");

        session.SetPreferredSampleRate(Format.SampleRate, out error);
        ThrowIfError(error, "Failed to set preferred sample rate.");

        session.SetActive(true, out error);
        ThrowIfError(error, "Failed to activate iOS audio session.");

        _sessionActivatedByMe = true;
    }

    private void DeactivateSessionIfOwned()
    {
        if (!_sessionActivatedByMe)
            return;

        var session = AVAudioSession.SharedInstance();
        NSError? error;
        session.SetActive(false, AVAudioSessionSetActiveOptions.NotifyOthersOnDeactivation, out error);
        _sessionActivatedByMe = false;
    }

    private static void ThrowIfError(NSError? error, string message)
    {
        if (error == null)
            return;

        throw new InvalidOperationException($"{message} {error.LocalizedDescription} ({error.Code})");
    }
}
