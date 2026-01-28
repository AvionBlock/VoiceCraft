using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using VoiceCraft.Core.Interfaces;

namespace VoiceCraft.Core.Locales
{
    public sealed class Localizer : INotifyPropertyChanged, INotifyPropertyChanging
    {
        private static IBaseLocalizer _baseLocalizer = new EmptyBaseLocalizer();

        //Private set language
        private string _language = "";
        public static Localizer Instance { get; } = new();

        public static IBaseLocalizer BaseLocalizer
        {
            get => _baseLocalizer;
            set
            {
                if (value == _baseLocalizer) return;
                _baseLocalizer = value;
                Instance.Language = Instance.Language;
            }
        }

        public string Language
        {
            get => _language;
            set
            {
                value = _baseLocalizer.Reload(value);
                Instance.SetField(ref _language, value);
            }
        }

        public static ObservableCollection<string> Languages => _baseLocalizer.Languages;
        public event PropertyChangedEventHandler? PropertyChanged;

        //Property Changed Events
        public event PropertyChangingEventHandler? PropertyChanging;
        public event Action<string>? OnLanguageChanged;

        public static string Get(string key)
        {
            return _baseLocalizer.Get(key);
        }

        private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return;
            OnPropertyChanging(propertyName);
            field = value;
            OnPropertyChanged(propertyName);
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            switch (propertyName)
            {
                case nameof(Language):
                    OnLanguageChanged?.Invoke(Language);
                    break;
            }
        }

        private void OnPropertyChanging([CallerMemberName] string? propertyName = null)
        {
            PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
        }
    }

    internal class EmptyBaseLocalizer : IBaseLocalizer
    {
        public string FallbackLanguage => throw new NotSupportedException();
        public ObservableCollection<string> Languages => throw new NotSupportedException();

        public string Get(string key)
        {
            throw new NotSupportedException();
        }

        public string Reload(string language)
        {
            throw new NotSupportedException();
        }
    }
}