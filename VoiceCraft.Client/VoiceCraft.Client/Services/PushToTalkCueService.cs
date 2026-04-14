using System;
using System.Threading.Tasks;
using SoundFlow.Abstracts.Devices;
using VoiceCraft.Client.Audio;
using VoiceCraft.Core;

namespace VoiceCraft.Client.Services;

public sealed class PushToTalkCueService
{
    private readonly AudioService _audioService;
    private readonly SettingsService _settingsService;
    private readonly object _lock = new();
    private AudioPlaybackDevice? _playbackDevice;
    private ToneProvider? _toneProvider;

    public PushToTalkCueService(AudioService audioService, SettingsService settingsService)
    {
        _audioService = audioService;
        _settingsService = settingsService;
    }

    public void PlayActivatedCue()
    {
        PlayCue(880f);
    }

    public void PlayReleasedCue()
    {
        PlayCue(620f);
    }

    private void PlayCue(float frequency)
    {
        if (!_settingsService.HotKeySettings.PlayPushToTalkCues)
            return;

        try
        {
            lock (_lock)
            {
                StopPlayback();

                _playbackDevice = _audioService.InitializePlaybackDevice(
                    Constants.SampleRate,
                    Constants.PlaybackChannels,
                    Constants.FrameSize,
                    _settingsService.OutputSettings.OutputDevice);

                _toneProvider = new ToneProvider(
                    _playbackDevice.Engine,
                    _playbackDevice.Format,
                    frequency,
                    TimeSpan.FromMilliseconds(80));

                _playbackDevice.MasterMixer.AddComponent(_toneProvider);
                _playbackDevice.Start();
                _ = StopAfterCueAsync();
            }
        }
        catch (Exception ex)
        {
            LogService.Log(ex);
            lock (_lock)
                StopPlayback();
        }
    }

    private async Task StopAfterCueAsync()
    {
        await Task.Delay(160);
        lock (_lock)
            StopPlayback();
    }

    private void StopPlayback()
    {
        _toneProvider?.Dispose();
        _toneProvider = null;

        if (_playbackDevice == null)
            return;

        _playbackDevice.Stop();
        _playbackDevice.Dispose();
        _playbackDevice = null;
    }
}
