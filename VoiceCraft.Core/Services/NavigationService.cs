using System;
using System.Collections.Generic;

namespace VoiceCraft.Core.Services
{
    public class NavigationService<TViewModelBase> where TViewModelBase : class
    {
        public TViewModelBase CurrentPage => _navigationStack[_index];

        private int _index = 0;
        private List<TViewModelBase> _navigationStack = new List<TViewModelBase>();

        public void Next()
        {
            if (_index < _navigationStack.Count - 1)
                _index++;
        }

        public void Back()
        {
            if (_index > 0)
                _index--;
        }

        public void GoTo<T>(object? data = null) where T : TViewModelBase
        {
            _navigationStack.RemoveRange(_index, _navigationStack.Count - _index);
            _navigationStack.Add((T)Activator.CreateInstance(typeof(T)));
            _index = _navigationStack.Count - 1;
        }
    }
}
