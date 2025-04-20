using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;

namespace VoiceCraft.Client.Browser
{
    sealed class Program
    {
        private static Task Main(string[] _) => BuildAvaloniaApp()
            .WithInterFont()
            .StartBrowserAppAsync("out");

        public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>();
    }
}