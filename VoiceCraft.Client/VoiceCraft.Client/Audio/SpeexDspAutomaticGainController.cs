using System;
using SpeexDSPSharp.Core;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Client.Audio;

public class SpeexDspAutomaticGainController : IAutomaticGainController
{
    private bool _disposed;
    private SpeexDSPPreprocessor? _gainController;
    public bool IsNative => false;

    public void Initialize(IAudioRecorder recorder)
    {
        ThrowIfDisposed();

        if (recorder.Channels != 1)
            throw new InvalidOperationException(Locales.Locales.Audio_AGC_InitFailed);

        CleanupGainController();

        _gainController = new SpeexDSPPreprocessor(recorder.BufferMilliseconds * recorder.SampleRate / 1000, recorder.SampleRate);

        var @false = 0;
        var @true = 1;
        var targetGain = 26000;
        _gainController.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_AGC, ref @true);
        _gainController.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_DEREVERB, ref @false);
        _gainController.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_VAD, ref @false);
        _gainController.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_DENOISE, ref @true);
        _gainController.Ctl(PreprocessorCtl.SPEEX_PREPROCESS_SET_AGC_TARGET, ref targetGain);
    }

    public void Process(byte[] buffer)
    {
        Process(buffer.AsSpan());
    }

    public void Process(Span<byte> buffer)
    {
        ThrowIfDisposed();
        ThrowIfNotInitialized();
        _gainController?.Run(buffer);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    ~SpeexDspAutomaticGainController()
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
        throw new ObjectDisposedException(typeof(SpeexDspAutomaticGainController).ToString());
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