using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using VoiceCraft.Client.PDK;
using VoiceCraft.Client.PDK.ViewModels;

namespace VoiceCraft.Client.Plugin.ViewModels.Home
{
    public partial class PluginsViewModel : ViewModelBase
    {
        public override string Title => "Plugins";

        [ObservableProperty]
        public ObservableCollection<PluginDisplay> _plugins;

        public PluginsViewModel()
        {
            _plugins = new ObservableCollection<PluginDisplay>(PluginLoader.Plugins.Select(x => new PluginDisplay(x.Name, x.Description)));
        }
    }

    public partial class PluginDisplay : ObservableObject
    {
        [ObservableProperty]
        public string _name;
        [ObservableProperty]
        public string _description;

        public PluginDisplay(string name, string description)
        {
            _name = name;
            _description = description;
        }
    }
}
