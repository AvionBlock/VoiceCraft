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
    private AVAudioEngine? _engine;
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
            StartAudioEngine();
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
            StopAudioEngine();
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

    private void StartAudioEngine()
    {
        _engine = new AVAudioEngine();
        var input = _engine.InputNode;
        var inputFormat = input.GetBusOutputFormat(0);

        // Tap keeps the input pipeline active while app is backgrounded.
        input.InstallTapOnBus(0, 1024, inputFormat, static (_, _) => { });

        _engine.Prepare();
        NSError? error;
        var started = _engine.StartAndReturnError(out error);
        if (!started || error != null)
        {
            StopAudioEngine();
            ThrowIfError(error);
            throw new InvalidOperationException("Failed to start iOS background audio engine.");
        }
    }

    private void StopAudioEngine()
    {
        if (_engine == null)
            return;

        try
        {
            _engine.InputNode.RemoveTapOnBus(0);
        }
        catch
        {
            // Do nothing.
        }

        _engine.Stop();
        _engine.Dispose();
        _engine = null;
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
