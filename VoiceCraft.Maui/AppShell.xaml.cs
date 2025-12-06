using VoiceCraft.Maui.Interfaces;

namespace VoiceCraft.Maui;

/// <summary>
/// Application shell that handles navigation and routing for mobile and desktop platforms.
/// </summary>
public partial class AppShell : Shell
{
    private readonly INavigationService _navigationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppShell"/> class.
    /// Sets up platform-specific routing for mobile and desktop views.
    /// </summary>
    /// <param name="navigationService">The navigation service.</param>
    public AppShell(INavigationService navigationService)
    {
        _navigationService = navigationService;
        InitializeComponent();

        // Platform-specific routing
        if (DeviceInfo.Idiom == DeviceIdiom.Phone)
        {
            tabBar.Items.Add(new ShellContent { Title = "Servers", Icon = "server.png", ContentTemplate = new DataTemplate(typeof(Views.Mobile.Servers)), Route = nameof(Views.Mobile.Servers) });
            tabBar.Items.Add(new ShellContent { Title = "Settings", Icon = "cog.png", ContentTemplate = new DataTemplate(typeof(Views.Mobile.Settings)), Route = nameof(Views.Mobile.Settings) });
            tabBar.Items.Add(new ShellContent { Title = "Credits", Icon = "information.png", ContentTemplate = new DataTemplate(typeof(Views.Mobile.Credits)), Route = nameof(Views.Mobile.Credits) });

            Routing.RegisterRoute(nameof(Views.Mobile.ServerDetails), typeof(Views.Mobile.ServerDetails));
            Routing.RegisterRoute(nameof(Views.Mobile.AddServer), typeof(Views.Mobile.AddServer));
            Routing.RegisterRoute(nameof(Views.Mobile.Voice), typeof(Views.Mobile.Voice));
            Routing.RegisterRoute(nameof(Views.Mobile.EditServer), typeof(Views.Mobile.EditServer));
        }
        else
        {
            flyoutItem.Items.Add(new ShellContent { Title = "Servers", Icon = "server.png", ContentTemplate = new DataTemplate(typeof(Views.Desktop.Servers)), Route = nameof(Views.Desktop.Servers) });
            flyoutItem.Items.Add(new ShellContent { Title = "Settings", Icon = "cog.png", ContentTemplate = new DataTemplate(typeof(Views.Desktop.Settings)), Route = nameof(Views.Desktop.Settings) });
            flyoutItem.Items.Add(new ShellContent { Title = "Credits", Icon = "information.png", ContentTemplate = new DataTemplate(typeof(Views.Desktop.Credits)), Route = nameof(Views.Desktop.Credits) });

            Routing.RegisterRoute(nameof(Views.Desktop.ServerDetails), typeof(Views.Desktop.ServerDetails));
            Routing.RegisterRoute(nameof(Views.Desktop.AddServer), typeof(Views.Desktop.AddServer));
            Routing.RegisterRoute(nameof(Views.Desktop.Voice), typeof(Views.Desktop.Voice));
            Routing.RegisterRoute(nameof(Views.Desktop.EditServer), typeof(Views.Desktop.EditServer));
        }
    }

    /// <inheritdoc/>
    protected override void OnAppearing()
    {
#if !WINDOWS
        if (Preferences.Get("VoipServiceRunning", false) && Current.CurrentPage?.BindingContext is not ViewModels.VoiceViewModel)
        {
            MainThread.BeginInvokeOnMainThread(async () => await _navigationService.NavigateTo(nameof(Views.Desktop.Voice)));
        }
#endif
        base.OnAppearing();
    }

    /// <inheritdoc/>
    protected override void OnDisappearing()
    {
#if WINDOWS
        Preferences.Set("VoipServiceRunning", false);
#endif
        base.OnDisappearing();
    }
}

