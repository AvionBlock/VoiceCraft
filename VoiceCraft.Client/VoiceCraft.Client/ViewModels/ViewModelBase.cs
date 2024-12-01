using CommunityToolkit.Mvvm.ComponentModel;

namespace VoiceCraft.Client.ViewModels
{
    public abstract class ViewModelBase : ObservableObject
    {
        public abstract string Title { get; protected set; }

        public virtual void OnAppearing() { }

        public virtual void OnDisappearing() { }
    }
}