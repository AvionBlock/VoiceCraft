using VoiceCraft.Client.PDK.Views;

namespace VoiceCraft.Client.PDK.Services
{
    public class NavigationService
    {
        public ViewBase CurrentPage { get; private set; } = default!;
        public int MaxHistory { get; }

        public event EventHandler<ViewBase>? OnPageChanging;
        public event EventHandler<ViewBase>? OnPageChanged;

        private Func<Type, ViewBase> _createView;
        private List<ViewBase> _history;

        public NavigationService(Func<Type, ViewBase> createView, int maxHistory = 50)
        {
            _createView = createView;
            _history = new List<ViewBase>();
            MaxHistory = maxHistory;
        }

        public TPage NavigateTo<TPage>() where TPage : ViewBase
        {
            var page = InstantiatePage<TPage>();
            OnPageChanging?.Invoke(this, page);

            if (CurrentPage != null)
                CurrentPage.ViewModel.OnDisappearing(this);

            CurrentPage = page;
            if (_history.Count >= MaxHistory)
                _history.RemoveAt(0);
            _history.Add(page);

            CurrentPage.ViewModel.OnAppearing(this);
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
                if (CurrentPage != null)
                    CurrentPage.ViewModel.OnDisappearing(this);

                CurrentPage = page;
                CurrentPage.ViewModel.OnAppearing(this);
                OnPageChanged?.Invoke(this, CurrentPage);
            }
        }

        private T InstantiatePage<T>()
        {
            return (T)Convert.ChangeType(_createView(typeof(T)), typeof(T));
        }
    }
}
