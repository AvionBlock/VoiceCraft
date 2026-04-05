using System;
using AVFoundation;
using Foundation;
using UIKit;
using VoiceCraft.Client.Services;
using VoiceCraft.Core;

namespace VoiceCraft.Client.iOS;

internal sealed class IosBackgroundExecutionKeeper : IDisposable
{
    private readonly object _lock = new();
    private NSObject? _didEnterBackgroundObserver;
    private NSObject? _willEnterForegroundObserver;
    private nint _backgroundTaskId = UIApplication.BackgroundTaskInvalid;
    private bool _isStarted;

    public void Start()
    {
        lock (_lock)
        {
            if (_isStarted)
                return;

            ConfigureAudioSession();
            RegisterLifecycleObservers();
            BeginBackgroundTask();
            _isStarted = true;
        }
    }

    public void Stop()
    {
        lock (_lock)
        {
            if (!_isStarted)
                return;

            UnregisterLifecycleObservers();
            EndBackgroundTask();
            DeactivateAudioSession();
            _isStarted = false;
        }
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }

    private static void ConfigureAudioSession()
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
        ThrowIfError(error);

        session.SetMode(AVAudioSessionMode.VoiceChat, out error);
        ThrowIfError(error);

        session.SetPreferredSampleRate(Constants.SampleRate, out error);
        ThrowIfError(error);

        session.SetPreferredIOBufferDuration(Constants.FrameSizeMs / 1000d, out error);
        ThrowIfError(error);

        session.SetActive(true, out error);
        ThrowIfError(error);
    }

    private static void DeactivateAudioSession()
    {
        var session = AVAudioSession.SharedInstance();
        NSError? error;
        session.SetActive(false, AVAudioSessionSetActiveOptions.NotifyOthersOnDeactivation, out error);
        if (error != null)
            LogService.Log(new InvalidOperationException($"Failed to deactivate iOS audio session: {error.LocalizedDescription} ({error.Code})"));
    }

    private void RegisterLifecycleObservers()
    {
        _didEnterBackgroundObserver = NSNotificationCenter.DefaultCenter.AddObserver(
            UIApplication.DidEnterBackgroundNotification,
            _ => BeginBackgroundTask());

        _willEnterForegroundObserver = NSNotificationCenter.DefaultCenter.AddObserver(
            UIApplication.WillEnterForegroundNotification,
            _ => EndBackgroundTask());
    }

    private void UnregisterLifecycleObservers()
    {
        if (_didEnterBackgroundObserver != null)
        {
            NSNotificationCenter.DefaultCenter.RemoveObserver(_didEnterBackgroundObserver);
            _didEnterBackgroundObserver = null;
        }

        if (_willEnterForegroundObserver != null)
        {
            NSNotificationCenter.DefaultCenter.RemoveObserver(_willEnterForegroundObserver);
            _willEnterForegroundObserver = null;
        }
    }

    private void BeginBackgroundTask()
    {
        if (_backgroundTaskId != UIApplication.BackgroundTaskInvalid)
            return;

        _backgroundTaskId = UIApplication.SharedApplication.BeginBackgroundTask(() =>
        {
            lock (_lock)
            {
                EndBackgroundTask();
            }
        });
    }

    private void EndBackgroundTask()
    {
        if (_backgroundTaskId == UIApplication.BackgroundTaskInvalid)
            return;

        UIApplication.SharedApplication.EndBackgroundTask(_backgroundTaskId);
        _backgroundTaskId = UIApplication.BackgroundTaskInvalid;
    }

    private static void ThrowIfError(NSError? error)
    {
        if (error == null)
            return;

        throw new InvalidOperationException($"iOS audio session error: {error.LocalizedDescription} ({error.Code})");
    }
}
