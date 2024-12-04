using System;
using System.Collections.Generic;
using System.Linq;
using VoiceCraft.Client.ViewModels;

namespace VoiceCraft.Client.Services
{
    public sealed class NavigationService(Func<Type, ViewModelBase> createViewModel, uint historyMaxSize = 100)
    {
        private int _historyIndex = -1;
        private List<ViewModelBase> _history = [];
        private ViewModelBase _currentViewModel = default!;
        public event Action<ViewModelBase>? OnViewModelChanged;

        private ViewModelBase CurrentViewModel
        {
            set
            {
                if (value == _currentViewModel) return;
                _currentViewModel = value;
                OnViewModelChanged?.Invoke(value);
            }
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public bool HasNext => _history.Count > 0 && _historyIndex < _history.Count - 1;
        // ReSharper disable once MemberCanBePrivate.Global
        public bool HasPrev => _historyIndex > 0;

        private void Push(ViewModelBase item)
        {
            if (HasNext)
            {
                for (var i = _historyIndex + 1; i < _history.Count; i++)
                {
                    // ReSharper disable once SuspiciousTypeConversion.Global
                    if (_history.ElementAt(i) is IDisposable disposablePage)
                        disposablePage.Dispose(); //Moving it off the stack, We dispose it if it implements IDisposable
                }

                _history = _history.Take(_historyIndex + 1).ToList();
            }

            _history.Add(item);
            _historyIndex = _history.Count - 1;
            if (_history.Count <= historyMaxSize) return;
            // ReSharper disable once SuspiciousTypeConversion.Global
            if (_history.ElementAt(0) is IDisposable disposable)
                disposable.Dispose(); //Moving off the stack. We dispose it if it implemented IDisposable.
            _history.RemoveAt(0);
        }

        // ReSharper disable once MemberCanBePrivate.Global
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

        public T NavigateTo<T>() where T : ViewModelBase
        {
            var viewModel = InstantiateViewModel<T>();
            CurrentViewModel = viewModel;
            Push(viewModel);
            return viewModel;
        }

        private T InstantiateViewModel<T>() where T : ViewModelBase
        {
            return (T)Convert.ChangeType(createViewModel(typeof(T)), typeof(T));
        }
    }
}