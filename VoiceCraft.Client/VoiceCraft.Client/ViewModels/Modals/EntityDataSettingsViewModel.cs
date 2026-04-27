using CommunityToolkit.Mvvm.ComponentModel;
using VoiceCraft.Client.Models;
using VoiceCraft.Client.ViewModels.Data;

namespace VoiceCraft.Client.ViewModels.Modals;

public partial class EntityDataSettingsViewModel : ViewModelBase
{
    [ObservableProperty] public partial EntityDataViewModel? Entity { get; private set; }

    public override void OnAppearing(object? data = null)
    {
        if (data is EntityDataSettingsNavigationData navigationData)
        {
            Entity = navigationData.Entity;
        }
    }
}
