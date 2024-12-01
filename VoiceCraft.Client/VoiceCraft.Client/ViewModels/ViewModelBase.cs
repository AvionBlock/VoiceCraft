using CommunityToolkit.Mvvm.ComponentModel;

namespace VoiceCraft.Client.ViewModels
{
    public abstract class ViewModelBase : ObservableObject
    {
        public virtual void OnAppearing()
        {
        }

        public virtual void OnDisappearing()
        {
        }
    }
}