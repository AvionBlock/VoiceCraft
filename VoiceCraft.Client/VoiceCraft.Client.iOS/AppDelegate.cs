using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.iOS;
using Foundation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;
using UIKit;
using VoiceCraft.Client.Services;
using VoiceCraft.Core;
using VoiceCraft.Core.Interfaces;
using VoiceCraft.Network.Clients;

namespace VoiceCraft.Client.iOS;

[Register("AppDelegate")]
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public partial class AppDelegate : AvaloniaAppDelegate<App>
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix
{
    public AppDelegate()
    {
        EnsureMiniAudioLoaded();

        var nativeStorage = new NativeStorageService();
        LogService.NativeStorageService = nativeStorage;
        LogService.Load();
        AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;

        App.ServiceCollection.AddSingleton<StorageService>(nativeStorage);
        App.ServiceCollection.AddSingleton<HotKeyService, NativeHotKeyService>();
        App.ServiceCollection.AddSingleton<IBackgroundService>(x =>
            new NativeBackgroundService(
                x.GetRequiredService<PermissionsService>(),
                x.GetRequiredService));
        App.ServiceCollection.AddSingleton<RegisteredAudioPreprocessor>(_ =>
            new RegisteredAudioPreprocessor(
                Constants.NativeVoiceProcessingPreprocessorGuid,
                "iOS Voice Processing",
                () => new IosAudioPreprocessor(),
                true,
                true,
                true));
        App.ServiceCollection.AddTransient<VoiceCraftClient>(x =>
            new LiteNetVoiceCraftClient(
                x.GetRequiredService<IAudioEncoder>(),
                x.GetRequiredService<IAudioDecoder>));
        App.ServiceCollection.AddTransient<Permissions.Microphone>();
    }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder)
    {
        return base.CustomizeAppBuilder(builder);
    }

    private static void EnsureMiniAudioLoaded()
    {
        try
        {
            if (NativeLibrary.TryLoad("miniaudio", out _))
                return;

            var baseDir = AppContext.BaseDirectory;
            var candidates = new[]
            {
                Path.Combine(baseDir, "Frameworks", "miniaudio.framework", "miniaudio"),
                Path.Combine(baseDir, "runtimes", "ios-arm64", "native", "miniaudio.framework", "miniaudio"),
                Path.Combine(baseDir, "runtimes", "iOS-arm64", "native", "miniaudio.framework", "miniaudio")
            };

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate) && NativeLibrary.TryLoad(candidate, out _))
                    return;
            }

            throw new DllNotFoundException("miniaudio");
        }
        catch (Exception ex)
        {
            LogService.Log(ex);
            throw;
        }
    }

    private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        try
        {
            if (e.ExceptionObject is Exception ex)
                LogService.LogCrash(ex);
        }
        catch (Exception writeEx)
        {
            Debug.WriteLine(writeEx);
        }
    }
}
