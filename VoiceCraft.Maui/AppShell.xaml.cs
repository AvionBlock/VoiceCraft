using VoiceCraft.Maui.Services;
using VoiceCraft.Maui.Interfaces;

namespace VoiceCraft.Maui
{
    public partial class AppShell : Shell
    {
        private readonly INavigationService _navigationService;

        public AppShell(INavigationService navigationService)
        {
            _navigationService = navigationService;
            InitializeComponent();

            //Routing
            if(DeviceInfo.Idiom == DeviceIdiom.Phone)
            {
                tabBar.Items.Add(new ShellContent() { Title = "Servers", Icon = "server.png", ContentTemplate = new DataTemplate(typeof(Views.Mobile.Servers)), Route = nameof(Views.Mobile.Servers) });
                tabBar.Items.Add(new ShellContent() { Title = "Settings", Icon = "cog.png", ContentTemplate = new DataTemplate(typeof(Views.Mobile.Settings)), Route = nameof(Views.Mobile.Settings) });
                tabBar.Items.Add(new ShellContent() { Title = "Credits", Icon = "information.png", ContentTemplate = new DataTemplate(typeof(Views.Mobile.Credits)), Route = nameof(Views.Mobile.Credits) });

                Routing.RegisterRoute(nameof(Views.Mobile.ServerDetails), typeof(Views.Mobile.ServerDetails));
                Routing.RegisterRoute(nameof(Views.Mobile.AddServer), typeof(Views.Mobile.AddServer));
                Routing.RegisterRoute(nameof(Views.Mobile.Voice), typeof(Views.Mobile.Voice));
                Routing.RegisterRoute(nameof(Views.Mobile.EditServer), typeof(Views.Mobile.EditServer));
            }
            else
            {
                flyoutItem.Items.Add(new ShellContent() { Title = "Servers", Icon = "server.png", ContentTemplate = new DataTemplate(typeof(Views.Desktop.Servers)), Route = nameof(Views.Desktop.Servers) });
                flyoutItem.Items.Add(new ShellContent() { Title = "Settings", Icon = "cog.png", ContentTemplate = new DataTemplate(typeof(Views.Desktop.Settings)), Route = nameof(Views.Desktop.Settings) });
                flyoutItem.Items.Add(new ShellContent() { Title = "Credits", Icon = "information.png", ContentTemplate = new DataTemplate(typeof(Views.Desktop.Credits)), Route = nameof(Views.Desktop.Credits) });

                Routing.RegisterRoute(nameof(Views.Desktop.ServerDetails), typeof(Views.Desktop.ServerDetails));
                Routing.RegisterRoute(nameof(Views.Desktop.AddServer), typeof(Views.Desktop.AddServer));
                Routing.RegisterRoute(nameof(Views.Desktop.Voice), typeof(Views.Desktop.Voice));
                Routing.RegisterRoute(nameof(Views.Desktop.EditServer), typeof(Views.Desktop.EditServer));
            }
        }

        protected override void OnAppearing()
        {
#if !WINDOWS
            if (Preferences.Get("VoipServiceRunning", false) && AppShell.Current.CurrentPage?.BindingContext is not ViewModels.VoiceViewModel)
            {
                MainThread.BeginInvokeOnMainThread(async () => await _navigationService.NavigateTo(nameof(Views.Desktop.Voice)));
            }
#endif
            base.OnAppearing();
        }

        protected override void OnDisappearing()
        {
#if WINDOWS
            Preferences.Set("VoipServiceRunning", false);
#endif
            base.OnDisappearing();
        }
    }
}
