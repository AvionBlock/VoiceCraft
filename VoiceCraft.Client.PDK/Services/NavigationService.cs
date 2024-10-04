using Avalonia.Controls;
using VoiceCraft.Client.PDK.ViewModels;

namespace VoiceCraft.Client.PDK.Services
{
    public class NavigationService
    {
        public Control CurrentPage { get; private set; } = default!;
        public int MaxHistory { get; }

        public event EventHandler<Control>? OnPageChanging;
        public event EventHandler<Control>? OnPageChanged;

        private Func<Type, Control> _createView;
        private List<Control> _history;

        public NavigationService(Func<Type, Control> createView, int maxHistory = 50)
        {
            _createView = createView;
            _history = new List<Control>();
            MaxHistory = maxHistory;
        }

        public TPage NavigateTo<TPage>() where TPage : Control
        {
            var page = InstantiatePage<TPage>();
            OnPageChanging?.Invoke(this, page);

            if (CurrentPage != null && CurrentPage.DataContext is ViewModelBase currentViewModel)
                currentViewModel.OnDisappearing(this);

            CurrentPage = page;
            if (_history.Count >= MaxHistory)
                _history.RemoveAt(0);
            _history.Add(page);

            if (page.DataContext is ViewModelBase newViewModel)
                newViewModel.OnAppearing(this);

            OnPageChanged?.Invoke(this, CurrentPage);
            return page;
        }

        public void Back()
        {
            if (_history.Count > 1)
            {
                _history.RemoveAt(_history.Count - 1);
                var page = _history.Last();
                OnPageChanging?.Invoke(this, page);
                if (CurrentPage != null && CurrentPage.DataContext is ViewModelBase currentViewModel)
                    currentViewModel.OnDisappearing(this);

                CurrentPage = page;

                if (page.DataContext is ViewModelBase newViewModel)
                    newViewModel.OnAppearing(this);
                OnPageChanged?.Invoke(this, CurrentPage);
            }
        }

        private T InstantiatePage<T>()
        {
            return (T)Convert.ChangeType(_createView(typeof(T)), typeof(T));
        }
    }
}
