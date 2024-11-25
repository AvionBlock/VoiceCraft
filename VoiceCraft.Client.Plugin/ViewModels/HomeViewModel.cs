using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DialogHostAvalonia;
using System.Collections.ObjectModel;
using VoiceCraft.Client.PDK.ViewModels;
using VoiceCraft.Client.Plugin.ViewModels.Home;

namespace VoiceCraft.Client.Plugin.ViewModels
{
    public partial class HomeViewModel : ViewModelBase, IDisposable
    {
        public override string Title => "Home";

        [ObservableProperty]
        private ViewModelBase _content = default!;

        [ObservableProperty]
        private ObservableCollection<ListItemTemplate> _items = new ObservableCollection<ListItemTemplate>();

        [ObservableProperty]
        private ListItemTemplate? _selectedListItem = null;

        public HomeViewModel(ServersViewModel servers, SettingsViewModel settings, CreditsViewModel credits, AddServerViewModel addServer, PluginsViewModel plugins)
        {
            _items.Add(new ListItemTemplate(servers, "home_regular"));
            _items.Add(new ListItemTemplate(settings, "mic_settings_regular"));
            _items.Add(new ListItemTemplate(credits, "book_information_regular"));
            _items.Add(new ListItemTemplate(addServer, "add_regular"));
            _items.Add(new ListItemTemplate(plugins, "extension_regular"));


            SelectedListItem = _items[0];
            _content = _items[0].Content;
        }

        partial void OnSelectedListItemChanged(ListItemTemplate? value)
        {
            if (value == null) return; //I don't know why this is a thing...
            if (Content != null)
                Content.OnDisappearing(this);

            Content = value.Content;

            if (Content != null)
                Content.OnAppearing(this);
        }

        [RelayCommand]
        public void Test()
        {
            DialogHost.Show("test", "MessageBoxDialog");
        }

        public void Dispose()
        {
            foreach(var vm in Items)
            {
                if(vm is IDisposable disposable)
                    disposable.Dispose();
            }
        }
    }

    public class ListItemTemplate
    {
        public ListItemTemplate(ViewModelBase control, string iconKey)
        {
            Content = control;
            Application.Current!.TryFindResource(iconKey, out var icon);
            Icon = (StreamGeometry)icon!;
        }

        public ViewModelBase Content { get; }
        public StreamGeometry? Icon { get; }
    }
}