using System;
using System.Collections.Generic;
using System.Threading;
using VoiceCraft.Client.Services;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Client.Audio;

public class CombinedAudioPreprocessor : IAudioPreprocessor
{
    private KeyValuePair<Guid, IAudioPreprocessor>? _gainController;
    private KeyValuePair<Guid, IAudioPreprocessor>? _denoiser;
    private KeyValuePair<Guid, IAudioPreprocessor>? _echoCanceller;
    private readonly Lock _lock = new();
    private bool _disposed;

    public bool DenoiserEnabled
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public bool GainControllerEnabled
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public bool EchoCancelerEnabled
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public int TargetGain
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public CombinedAudioPreprocessor(
        RegisteredAudioPreprocessor? gainController,
        RegisteredAudioPreprocessor? denoiser,
        RegisteredAudioPreprocessor? echoCanceller)
    {
        if (gainController != null)
            SetGainController(gainController);
        if (denoiser != null)
            SetDenoiser(denoiser);
        if (echoCanceller != null)
            SetEchoCanceller(echoCanceller);
    }

    ~CombinedAudioPreprocessor()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public void Process(Span<float> buffer)
    {
        ThrowIfDisposed();
        lock (_lock)
        {
            _gainController?.Value.Process(buffer);
            _denoiser?.Value.Process(buffer);
            _echoCanceller?.Value.Process(buffer);
        }
    }

    public void ProcessPlayback(Span<float> buffer)
    {
        ThrowIfDisposed();
        lock (_lock)
        {
            _echoCanceller?.Value.ProcessPlayback(buffer);
        }
    }

    private void SetGainController(RegisteredAudioPreprocessor gainController)
    {
        var gainControllerInstance = gainController.Instantiate();
        gainControllerInstance.GainControllerEnabled = true;
        gainControllerInstance.DenoiserEnabled = false;
        gainControllerInstance.EchoCancelerEnabled = false;
        _gainController = new KeyValuePair<Guid, IAudioPreprocessor>(gainController.Id, gainControllerInstance);
    }

    private void SetDenoiser(RegisteredAudioPreprocessor denoiser)
    {
        if (_gainController != null && denoiser.Id == _gainController.Value.Key)
        {
            _gainController.Value.Value.DenoiserEnabled = true;
            return;
        }

        var denoiserInstance = denoiser.Instantiate();
        denoiserInstance.GainControllerEnabled = false;
        denoiserInstance.DenoiserEnabled = true;
        denoiserInstance.EchoCancelerEnabled = false;
        _denoiser = new KeyValuePair<Guid, IAudioPreprocessor>(denoiser.Id, denoiserInstance);
    }

    private void SetEchoCanceller(RegisteredAudioPreprocessor echoCanceller)
    {
        if (_gainController != null && echoCanceller.Id == _gainController.Value.Key)
        {
            _gainController.Value.Value.EchoCancelerEnabled = true;
            return;
        }

        if (_denoiser != null && echoCanceller.Id == _denoiser.Value.Key)
        {
            _denoiser.Value.Value.EchoCancelerEnabled = true;
            return;
        }

        var echoCancellerInstance = echoCanceller.Instantiate();
        echoCancellerInstance.GainControllerEnabled = false;
        echoCancellerInstance.DenoiserEnabled = false;
        echoCancellerInstance.EchoCancelerEnabled = true;
        _echoCanceller = new KeyValuePair<Guid, IAudioPreprocessor>(echoCanceller.Id, echoCancellerInstance);
    }

    private void ThrowIfDisposed()
    {
        if (!_disposed) return;
        throw new ObjectDisposedException(typeof(CombinedAudioPreprocessor).ToString());
    }

    private void Dispose(bool disposing)
    {
        if (_disposed) return;
        lock (_lock)
        {
            if (disposing)
            {
                _gainController?.Value.Dispose();
                _denoiser?.Value.Dispose();
                _echoCanceller?.Value.Dispose();
            }

            _disposed = true;
        }
    }
}
