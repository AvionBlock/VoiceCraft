using OpusSharp.Core;

namespace VoiceCraft.Maui;

/// <summary>
/// Main application class for VoiceCraft MAUI client.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Gets the application version string.
    /// </summary>
    public static string Version { get; } = AppInfo.Current.VersionString;

    /// <summary>
    /// Gets the Opus codec version string.
    /// </summary>
    public static string OpusVersion { get; } = OpusInfo.Version();

    private readonly AppShell _shell;

    /// <summary>
    /// Initializes a new instance of the <see cref="App"/> class.
    /// </summary>
    /// <param name="shell">The application shell.</param>
    public App(AppShell shell)
    {
        InitializeComponent();
        _shell = shell;
    }

    /// <inheritdoc/>
    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(_shell);
    }
}
