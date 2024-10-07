using Avalonia.Controls;
using VoiceCraft.Client.PDK.ViewModels;

namespace VoiceCraft.Client.PDK.Views
{
    public abstract class ViewBase : UserControl
    {
        public abstract ViewModelBase ViewModel { get; }
    }
}
