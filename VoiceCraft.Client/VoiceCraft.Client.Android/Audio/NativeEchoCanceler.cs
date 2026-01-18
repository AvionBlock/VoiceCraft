using System;
using Android.Media.Audiofx;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Core.Locales;

namespace VoiceCraft.Client.Android.Audio;

public class NativeEchoCanceler : IEchoCanceler
{
    private bool _disposed;

    private AcousticEchoCanceler? _echoCanceler;
    public bool IsNative => true;

    public void
        Initialize(IAudioRecorder recorder,
            IAudioPlayer player) //We don't need to have the audio player, but it's there for other compatibility reasons.
    {
        ThrowIfDisposed();

        if (recorder is not AudioRecorder audioRecorder)
            throw new InvalidOperationException(Localizer.Get("Audio.AEC.InitFailed"));

        CleanupEchoCanceler();
        _echoCanceler = AcousticEchoCanceler.Create(audioRecorder.SessionId);

        if (_echoCanceler == null || _echoCanceler.SetEnabled(true) != AudioEffectStatus.Success)
            throw new InvalidOperationException(Localizer.Get("Audio.AEC.InitFailed"));
    }

    public void EchoPlayback(Span<byte> buffer)
    {
        ThrowIfDisposed();
        ThrowIfNotInitialized();
    }

    public void EchoCancel(Span<byte> buffer)
    {
        ThrowIfDisposed();
        ThrowIfNotInitialized();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~NativeEchoCanceler()
    {
        Dispose(false);
    }

    private void CleanupEchoCanceler()
    {
        if (_echoCanceler == null) return;
        _echoCanceler.Dispose();
        _echoCanceler = null;
    }

    private void ThrowIfDisposed()
    {
        if (!_disposed) return;
        throw new ObjectDisposedException(typeof(NativeEchoCanceler).ToString());
    }

    private void ThrowIfNotInitialized()
    {
        if (_echoCanceler == null)
            throw new InvalidOperationException(Localizer.Get("Audio.AEC.Init"));
    }

    private void Dispose(bool disposing)
    {
        if (_disposed || !disposing) return;
        CleanupEchoCanceler();
        _disposed = true;
    }
}