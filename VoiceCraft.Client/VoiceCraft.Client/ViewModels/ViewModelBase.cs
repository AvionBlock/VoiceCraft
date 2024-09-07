using CommunityToolkit.Mvvm.ComponentModel;

namespace VoiceCraft.Client.ViewModels
{
    public abstract class ViewModelBase : ObservableObject
    {
        public abstract string Title { get; protected set; }
    }
}
