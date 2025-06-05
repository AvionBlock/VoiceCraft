using System;
using System.Collections.Generic;
using System.Linq;
using VoiceCraft.Client.ViewModels;

namespace VoiceCraft.Client.Services;

public sealed class NavigationService(Func<Type, ViewModelBase> createViewModel, uint historyMaxSize = 100)
{
    private ViewModelBase? _currentViewModel;
    private List<ViewModelBase> _history = [];
    private int _historyIndex = -1;

    // ReSharper disable once MemberCanBePrivate.Global
    public bool HasNext => _history.Count > 0 && _historyIndex < _history.Count - 1;

    // ReSharper disable once MemberCanBePrivate.Global
    public bool HasPrev => _historyIndex > 0;
    public event Action<ViewModelBase>? OnViewModelChanged;

    private void Push(ViewModelBase item)
    {
        if (HasNext)
        {
            for (var i = _historyIndex + 1; i < _history.Count; i++)
                // ReSharper disable once SuspiciousTypeConversion.Global
                if (_history.ElementAt(i) is IDisposable disposablePage)
                    disposablePage.Dispose(); //Moving it off the stack, We dispose it if it implements IDisposable

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

    private void SetCurrentViewModel(ViewModelBase viewModel, object? data = null)
    {
        if (viewModel == _currentViewModel) return;
        _currentViewModel?.OnDisappearing();
        _currentViewModel = viewModel;
        _currentViewModel.OnAppearing(data);
        OnViewModelChanged?.Invoke(viewModel);
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public ViewModelBase? Go(int offset = 0, bool checkBackButton = false)
    {
        if (checkBackButton && (_currentViewModel?.DisableBackButton ?? false))
            return _currentViewModel;

        if (offset == 0)
            return null;

        var newIndex = _historyIndex + offset;
        if (newIndex < 0 || newIndex > _history.Count - 1)
            return null;

        _historyIndex = newIndex;
        var viewModel = _history.ElementAt(_historyIndex);
        SetCurrentViewModel(viewModel);
        return viewModel;
    }

    public ViewModelBase? Back(bool checkBackButton = false)
    {
        return HasPrev ? Go(-1, checkBackButton) : null;
    }

    public ViewModelBase? Forward()
    {
        return HasNext ? Go(1) : null;
    }

    public void NavigateTo<T>(object? data = null) where T : ViewModelBase
    {
        var viewModel = InstantiateViewModel<T>();
        SetCurrentViewModel(viewModel, data);
        Push(viewModel);
    }

    private T InstantiateViewModel<T>() where T : ViewModelBase
    {
        return (T)Convert.ChangeType(createViewModel(typeof(T)), typeof(T));
    }
}