using CommunityToolkit.Mvvm.ComponentModel;

namespace VoiceCraft.Client.PDK.ViewModels
{
    public abstract class ViewModelBase : ObservableObject
    {
        public abstract string Title { get; }

        public virtual void OnAppearing(object? sender) { }

        public virtual void OnDisappearing(object? sender) { }
    }
}
