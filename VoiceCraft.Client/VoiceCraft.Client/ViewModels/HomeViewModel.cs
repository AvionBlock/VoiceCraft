using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using VoiceCraft.Client.ViewModels.HomeViews;
using VoiceCraft.Core;

namespace VoiceCraft.Client.ViewModels
{
    public partial class HomeViewModel : ViewModelBase
    {
        public override string Title { get => "Home"; protected set => throw new NotSupportedException(); }

        [ObservableProperty]
        private ViewModelBase _content = default!;

        [ObservableProperty]
        private ObservableCollection<ListItemTemplate> _items = new ObservableCollection<ListItemTemplate>();

        [ObservableProperty]
        private ListItemTemplate? _selectedListItem = null;

        public HomeViewModel(ServersViewModel servers, SettingsViewModel settings, CreditsViewModel credits)
        {
            _items.Add(new ListItemTemplate(servers, "home_regular"));
            _items.Add(new ListItemTemplate(settings, "mic_settings_regular"));
            _items.Add(new ListItemTemplate(credits, "book_information_regular"));
            //_items.Add(new ListItemTemplate(addServer, "add_regular"));
            

            SelectedListItem = _items[0];
            _content = _items[0].ViewModel;
        }

        partial void OnSelectedListItemChanged(ListItemTemplate? value)
        {
            if (value == null) return;
            Content = value.ViewModel;
        }
    }

    public partial class ListItemTemplate
    {
        public ListItemTemplate(ViewModelBase viewModel, string iconKey)
        {
            ViewModel = viewModel;

            Application.Current!.TryFindResource(iconKey, out var icon);
            Icon = (StreamGeometry)icon!;
        }

        public ViewModelBase ViewModel { get; }
        public StreamGeometry? Icon { get; }
    }
}
