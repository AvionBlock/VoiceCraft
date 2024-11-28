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
        private PluginLoader _pluginLoader;

        [ObservableProperty]
        public ObservableCollection<PluginDisplay> _plugins;

        public PluginsViewModel(TopLevel topLevel, NotificationMessageManager manager, SettingsService settings, PluginLoader pluginLoader)
        {
            _storageProvider = topLevel.StorageProvider;
            _manager = manager;
            _pluginLoader = pluginLoader;
            _notificationSettings = settings.Get<NotificationSettings>(Plugin.PluginId);

            _plugins = new ObservableCollection<PluginDisplay>(pluginLoader.Plugins.Select(x => new PluginDisplay(x.LoadedInstance.Id, x.LoadedInstance.Name, x.LoadedInstance.Description, x.Version.ToString())));
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
