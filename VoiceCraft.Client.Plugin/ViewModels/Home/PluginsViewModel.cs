using Avalonia.Controls;
using Avalonia.Notification;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using VoiceCraft.Client.PDK;
using VoiceCraft.Client.PDK.Services;
using VoiceCraft.Client.PDK.ViewModels;
using VoiceCraft.Client.Plugin.Settings;

namespace VoiceCraft.Client.Plugin.ViewModels.Home
{
    public partial class PluginsViewModel : ViewModelBase
    {
        public override string Title => "Plugins";
        private IStorageProvider _storageProvider;
        private INotificationMessageManager _manager;
        private NotificationSettings _notificationSettings;

        [ObservableProperty]
        public ObservableCollection<PluginDisplay> _plugins;

        public PluginsViewModel(TopLevel topLevel, NotificationMessageManager manager, SettingsService settings)
        {
            _storageProvider = topLevel.StorageProvider;
            _manager = manager;
            _notificationSettings = settings.Get<NotificationSettings>(Plugin.PluginId);

            _plugins = new ObservableCollection<PluginDisplay>(PluginLoader.Plugins.Select(x => new PluginDisplay(x.PluginInformation.Id, x.PluginInformation.Name, x.PluginInformation.Description, x.Assembly.Version.ProductVersion)));
        }

        [RelayCommand]
        public async Task AddPlugin()
        {
            var files = await _storageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Add Plugin",
                AllowMultiple = false
                //TODO FOR FILE PICKING!
            });
        }

        [RelayCommand]
        public void RemovePlugin(PluginDisplay plugin)
        {
            if(PluginLoader.DeletePlugin(plugin.Id))
            {
                plugin.Deleted = true;
                _manager.CreateMessage()
                    .Accent(ThemesService.GetBrushResource("notificationAccentSuccessBrush"))
                    .Animates(true)
                    .Background(ThemesService.GetBrushResource("notificationBackgroundSuccessBrush"))
                    .HasBadge("Plugin")
                    .HasMessage($"{plugin.Name} plugin has been deleted. Restart the application for the changes to take effect!")
                    .Dismiss().WithDelay(TimeSpan.FromMilliseconds(_notificationSettings.DismissDelayMS))
                    .Dismiss().WithButton("Dismiss", (button) => { })
                    .Queue();
                return;
            }

            _manager.CreateMessage()
                .Accent(ThemesService.GetBrushResource("notificationAccentBrush"))
                .Animates(true)
                .Background(ThemesService.GetBrushResource("notificationBackgroundBrush"))
                .HasBadge("Plugin")
                .HasMessage($"{plugin.Name} plugin has already been removed or does not exist!")
                .Dismiss().WithDelay(TimeSpan.FromMilliseconds(_notificationSettings.DismissDelayMS))
                .Dismiss().WithButton("Dismiss", (button) => { })
                .Queue();
        }
    }

    public partial class PluginDisplay : ObservableObject
    {
        [ObservableProperty]
        private string _name;
        [ObservableProperty]
        private string _description;
        [ObservableProperty]
        private Guid _id;
        [ObservableProperty]
        private string _version;
        [ObservableProperty]
        private bool _deleted;

        public PluginDisplay(Guid id, string name, string description, string? version)
        {
            _name = name;
            _description = description;
            _id = id;
            _version = version ?? "N.A.";
        }
    }
}
