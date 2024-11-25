using VoiceCraft.Client.PDK.ViewModels;

namespace VoiceCraft.Client.PDK.Services
{
    public class NavigationService
    {
        private int _historyIndex = -1;
        private List<ViewModelBase> _history = new List<ViewModelBase>();
        private readonly uint _historyMaxSize;
        private ViewModelBase _currentViewModel = default!;
        private readonly Func<Type, ViewModelBase> _createViewModel;
        public event Action<ViewModelBase>? OnViewModelChanged;
        protected ViewModelBase CurrentViewModel
        {
            set
            {
                if (value == _currentViewModel) return;
                _currentViewModel = value;
                OnViewModelChanged?.Invoke(value);
            }
        }

        public bool HasNext => _history.Count > 0 && _historyIndex < _history.Count - 1;
        public bool HasPrev => _historyIndex > 0;

        public NavigationService(Func<Type, ViewModelBase> createViewModel, uint historyMaxSize = 100)
        {
            _historyMaxSize = historyMaxSize;
            _createViewModel = createViewModel;
        }

        public void Push(ViewModelBase item)
        {
            if (HasNext)
            {
                for(var i = _historyIndex + 1; i < _history.Count; i++)
                {
                    if (_history.ElementAt(i) is IDisposable disposable)
                        disposable.Dispose(); //Moving it off the stack, We dispose it if it implements IDisposable
                }

                _history = _history.Take(_historyIndex + 1).ToList();
            }

            _history.Add(item);
            _historyIndex = _history.Count - 1;
            if (_history.Count > _historyMaxSize)
            {
                if(_history.ElementAt(0) is IDisposable disposable)
                    disposable.Dispose(); //Moving off the stack. We dispose it if it implemented IDisposable.
                _history.RemoveAt(0);
            }
        }

        public ViewModelBase? Go(int offset = 0)
        {
            if (offset == 0)
            {
                return default;
            }

            var newIndex = _historyIndex + offset;
            if (newIndex < 0 || newIndex > _history.Count - 1)
            {
                return default;
            }

            _historyIndex = newIndex;
            var viewModel = _history.ElementAt(_historyIndex);
            CurrentViewModel = viewModel;
            return viewModel;
        }

        public ViewModelBase? Back() => HasPrev ? Go(-1) : default;

        public ViewModelBase? Forward() => HasNext ? Go(1) : default;

        public virtual T NavigateTo<T>() where T : ViewModelBase
        {
            var viewModel = InstantiateViewModel<T>();
            CurrentViewModel = viewModel;
            Push(viewModel);
            return viewModel;
        }

        private T InstantiateViewModel<T>() where T : ViewModelBase
        {
            return (T)Convert.ChangeType(_createViewModel(typeof(T)), typeof(T));
        }
    }
}