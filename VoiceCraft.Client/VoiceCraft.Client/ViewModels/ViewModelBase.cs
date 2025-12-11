using CommunityToolkit.Mvvm.ComponentModel;

namespace VoiceCraft.Client.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
    public virtual bool DisableBackButton { get; protected set; }

    public virtual void OnAppearing(object? data = null)
    {
    }

    public virtual void OnDisappearing()
    {
    }
}