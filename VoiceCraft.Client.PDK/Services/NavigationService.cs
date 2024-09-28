using Avalonia.Controls;

namespace VoiceCraft.Client.PDK.Services
{
    public class NavigationService
    {
        public Control CurrentPage { get; private set; } = default!;

        public event EventHandler<Control>? OnPageChanging;
        public event EventHandler<Control>? OnPageChanged;

        private Func<Type, Control> _createView;

        public NavigationService(Func<Type, Control> createView)
        {
            _createView = createView;
        }

        public TPage NavigateTo<TPage>() where TPage : Control
        {
            var page = InstantiatePage<TPage>();
            OnPageChanging?.Invoke(this, page);
            CurrentPage = page;
            OnPageChanged?.Invoke(this, CurrentPage);
            return page;
        }

        private T InstantiatePage<T>()
        {
            return (T)Convert.ChangeType(_createView(typeof(T)), typeof(T));
        }
    }
}
