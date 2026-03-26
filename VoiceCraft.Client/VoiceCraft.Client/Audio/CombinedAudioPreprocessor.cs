using System;
using System.Collections.Generic;
using VoiceCraft.Client.Services;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Client.Audio;

public class CombinedAudioPreprocessor : IAudioPreprocessor
{
    private KeyValuePair<Guid, IAudioPreprocessor>? _gainController;
    private KeyValuePair<Guid, IAudioPreprocessor>? _denoiser;
    private KeyValuePair<Guid, IAudioPreprocessor>? _echoCanceller;

    public bool DenoiserEnabled { get; set; }
    public bool GainControllerEnabled { get; set; }
    public bool EchoCancellerEnabled { get; set; }
    public int TargetGain { get; set; }

    public CombinedAudioPreprocessor(
        RegisteredAudioPreprocessor? gainController,
        RegisteredAudioPreprocessor? denoiser,
        RegisteredAudioPreprocessor? echoCanceller)
    {
        if (gainController != null)
            SetGainController(gainController);
        if(denoiser != null)
            SetDenoiser(denoiser);
        if(echoCanceller != null)
            SetEchoCanceller(echoCanceller);
    }

    public void Dispose()
    {
        // TODO release managed resources here
    }

    public void Process(Span<float> buffer)
    {
        throw new NotImplementedException();
    }

    public void ProcessPlayback(Span<float> buffer)
    {
        throw new NotImplementedException();
    }

    private void SetGainController(RegisteredAudioPreprocessor gainController)
    {
        var gainControllerInstance = gainController.Instantiate();
        gainControllerInstance.GainControllerEnabled = true;
        gainControllerInstance.DenoiserEnabled = false;
        gainControllerInstance.EchoCancellerEnabled = false;
        _gainController = new KeyValuePair<Guid, IAudioPreprocessor>(gainController.Id, gainControllerInstance);
    }

    private void SetDenoiser(RegisteredAudioPreprocessor denoiser)
    {
        if (_gainController != null && denoiser.Id == _gainController.Value.Key)
        {
            _gainController.Value.Value.GainControllerEnabled = true;
            return;
        }
        
        var denoiserInstance = denoiser.Instantiate();
        denoiserInstance.GainControllerEnabled = false;
        denoiserInstance.DenoiserEnabled = true;
        denoiserInstance.EchoCancellerEnabled = false;
        _denoiser = new KeyValuePair<Guid, IAudioPreprocessor>(denoiser.Id, denoiserInstance);
    }

    private void SetEchoCanceller(RegisteredAudioPreprocessor echoCanceller)
    {
        if (_gainController != null && echoCanceller.Id == _gainController.Value.Key)
        {
            _gainController.Value.Value.EchoCancellerEnabled = true;
            return;
        }
        if (_denoiser != null && echoCanceller.Id == _denoiser.Value.Key)
        {
            _denoiser.Value.Value.EchoCancellerEnabled = false;
            return;
        }
        
        var echoCancellerInstance = echoCanceller.Instantiate();
        echoCancellerInstance.GainControllerEnabled = false;
        echoCancellerInstance.DenoiserEnabled = true;
        echoCancellerInstance.EchoCancellerEnabled = false;
        _echoCanceller = new KeyValuePair<Guid, IAudioPreprocessor>(echoCanceller.Id, echoCancellerInstance);
    }
}