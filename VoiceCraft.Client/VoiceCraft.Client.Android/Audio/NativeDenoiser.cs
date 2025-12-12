using System;
using Android.Media.Audiofx;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Core.Locales;

namespace VoiceCraft.Client.Android.Audio;

public class NativeDenoiser : IDenoiser
{
    private NoiseSuppressor? _denoiser;
    private bool _disposed;
    public bool IsNative => true;

    public void Initialize(IAudioRecorder recorder)
    {
        ThrowIfDisposed();

        if (recorder is not AudioRecorder audioRecorder)
            throw new InvalidOperationException(Localizer.Get("Audio.DN.InitFailed"));

        CleanupDenoiser();
        _denoiser = NoiseSuppressor.Create(audioRecorder.SessionId);

        if (_denoiser == null || _denoiser.SetEnabled(true) != AudioEffectStatus.Success)
            throw new InvalidOperationException(Localizer.Get("Audio.DN.InitFailed"));
    }

    public void Denoise(byte[] buffer)
    {
        Denoise(buffer.AsSpan());
    }

    public void Denoise(Span<byte> buffer)
    {
        ThrowIfDisposed();
        ThrowIfNotInitialized();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~NativeDenoiser()
    {
        Dispose(false);
    }

    private void CleanupDenoiser()
    {
        if (_denoiser == null) return;
        _denoiser.Dispose();
        _denoiser = null;
    }

    private void ThrowIfDisposed()
    {
        if (!_disposed) return;
        throw new ObjectDisposedException(typeof(NativeDenoiser).ToString());
    }

    private void ThrowIfNotInitialized()
    {
        if (_denoiser == null)
            throw new InvalidOperationException(Localizer.Get("Audio.DN.Init"));
    }

    private void Dispose(bool disposing)
    {
        if (_disposed || !disposing) return;
        CleanupDenoiser();
        _disposed = true;
    }
}