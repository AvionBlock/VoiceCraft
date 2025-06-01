using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using VoiceCraft.Client.ViewModels;

namespace VoiceCraft.Client.Services;

public class ViewLocatorService(Func<string, Control?> getView) : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param is null)
            return null;

        var name = param.GetType().FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
        var view = getView(name);

        return view ?? new TextBlock { Text = "Not Found: " + name };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}