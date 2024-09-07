using Avalonia.SimpleRouter;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace VoiceCraft.Client.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        public override string Title { get => "Main View Model"; protected set => throw new NotSupportedException(); }

        [ObservableProperty]
        private ListItemTemplate? _selectedItem = null;

        [ObservableProperty]
        private ViewModelBase _content = default!;

        [ObservableProperty]
        private bool _paneOpen = false;

        public ObservableCollection<ListItemTemplate> Items { get; } = new ObservableCollection<ListItemTemplate>()
        {
                new ListItemTemplate(typeof(ServersViewModel))
        };

        public MainViewModel(HistoryRouter<ViewModelBase> router)
        {
            // register route changed event to set content to viewModel, whenever 
            // a route changes
            router.CurrentViewModelChanged += viewModel => Content = viewModel;
            router.CurrentViewModelChanged += viewModel => SelectedItem = Items.FirstOrDefault(x => x.ModelType == viewModel.GetType());

            // change to HomeView 
            var model = router.GoTo<ServersViewModel>().GetType();
        }

        [RelayCommand]
        public void TogglePane()
        {
            PaneOpen = !PaneOpen;
        }
    }

    public partial class ListItemTemplate
    {
        public ListItemTemplate(Type type)
        {
            ModelType = type;
            Label = type.Name.Replace("ViewModel", "");
        }

        public string Label { get; }
        public Type ModelType { get; }
    }
}
