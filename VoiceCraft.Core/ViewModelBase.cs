using CommunityToolkit.Mvvm.ComponentModel;

namespace VoiceCraft.Core
{
    public abstract class ViewModelBase : ObservableObject
    {
        public abstract string Title { get; protected set; }
    }
}
