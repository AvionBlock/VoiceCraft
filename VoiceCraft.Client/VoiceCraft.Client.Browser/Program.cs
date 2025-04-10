using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;

using VoiceCraft.Client;
using VoiceCraft.Client.Services;
using VoiceCraft.Client.Browser.Audio;
using VoiceCraft.Client.Browser.Permissions;
using VoiceCraft.Client.Browser;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;
using Exception = System.Exception;

// using System.Runtime.InteropServices.JavaScript;
using System.Runtime.InteropServices;

using OpenTK.Audio.OpenAL;

namespace VoiceCraft.Client.Browser
{
    sealed class Program
    {
        private static Task Main(string[] _) {
            OpenALLibraryNameContainer.OverridePath = "openal";
            // OpenALLibraryNameContainer.OverridePath = "libalol";
            // OpenALLibraryNameContainer.OverridePath = "__Internal";
            // OpenALLibraryNameContainer.OverridePath = "__Internal_emscripten";

            Trace.Listeners.Add(new ConsoleTraceListener());

            App.ServiceCollection.AddSingleton<AudioService, NativeAudioService>();
            App.ServiceCollection.AddSingleton<BackgroundService, NativeBackgroundService>();
            App.ServiceCollection.AddTransient<Microsoft.Maui.ApplicationModel.Permissions.Microphone, Microphone>();

            return BuildAvaloniaApp()
                .LogToTrace()
                .WithInterFont()
                .StartBrowserAppAsync("out");
        }

        public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>();
    }
}
