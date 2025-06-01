using System;
using Android.Media.Audiofx;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Client.Android.Audio;

public class NativeAutomaticGainController : IAutomaticGainController
{
    private bool _disposed;

    private AutomaticGainControl? _gainController;
    public bool IsNative => true;

    public void Initialize(IAudioRecorder recorder)
    {
        ThrowIfDisposed();

        if (recorder is not AudioRecorder audioRecorder)
            throw new InvalidOperationException(Locales.Locales.Audio_AGC_InitFailed);

        CleanupGainController();
        _gainController = AutomaticGainControl.Create(audioRecorder.SessionId);

        if (_gainController == null)
            throw new InvalidOperationException(Locales.Locales.Audio_AEC_InitFailed);
    }

    public void Process(byte[] buffer)
    {
        Process(buffer.AsSpan());
    }

    public void Process(Span<byte> buffer)
    {
        ThrowIfDisposed();
        ThrowIfNotInitialized();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~NativeAutomaticGainController()
    {
        Dispose(false);
    }

    private void CleanupGainController()
    {
        if (_gainController == null) return;
        _gainController.Dispose();
        _gainController = null;
    }

    private void ThrowIfDisposed()
    {
        if (!_disposed) return;
        throw new ObjectDisposedException(typeof(NativeAutomaticGainController).ToString());
    }

    private void ThrowIfNotInitialized()
    {
        if (_gainController == null)
            throw new InvalidOperationException(Locales.Locales.Audio_AGC_Init);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed || !disposing) return;
        CleanupGainController();
        _disposed = true;
    }
}