using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using VoiceCraft.Client.PDK;
using VoiceCraft.Client.PDK.ViewModels;

namespace VoiceCraft.Client.Plugin.ViewModels.Home
{
    public partial class PluginsViewModel : ViewModelBase
    {
        public override string Title => "Plugins";
        private IStorageProvider _storageProvider;

        [ObservableProperty]
        public ObservableCollection<PluginDisplay> _plugins;

        public PluginsViewModel(TopLevel topLevel)
        {
            _plugins = new ObservableCollection<PluginDisplay>(PluginLoader.Plugins.Select(x => new PluginDisplay(x.PluginInformation.Name, x.PluginInformation.Description, x.PluginInformation.Id)));
            _storageProvider = topLevel.StorageProvider;
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
        private bool _markedForDeletion;

        public PluginDisplay(string name, string description, Guid id)
        {
            _name = name;
            _description = description;
            _id = id;
        }

        [RelayCommand]
        public void RemoveOrCancelPlugin()
        {
            if (!MarkedForDeletion)
            {
                PluginLoader.DeletePlugin(Id);
                MarkedForDeletion = true;
            }
            else
            {
                PluginLoader.CancelPluginDeletion(Id);
                MarkedForDeletion = false;
            }
        }
    }
}
