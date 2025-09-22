using System;
using System.Linq;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using VoiceCraft.Core.Locales;

namespace VoiceCraft.Client.Locales;

public class LocalizeExtension(object arg) : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (arg is string key)
            return new MultiBinding()
            {
                Bindings =
                [
                    new Binding() { Source = key },
                    new Binding(nameof(Localizer.Instance.Language)) { Source = Localizer.Instance }
                ],
                Mode = BindingMode.TwoWay,
                Converter = new FuncMultiValueConverter<string, string>(x =>
                    Localizer.Get(x.ElementAtOrDefault(0) ?? ""))
            };

        if (arg is not IBinding binding)
            throw new Exception("Argument must be of type IBinding or string!");

        var mb = new MultiBinding()
        {
            Bindings = [binding, new Binding(nameof(Localizer.Instance.Language)) { Source = Localizer.Instance }],
            Mode = BindingMode.TwoWay,
            Converter = new FuncMultiValueConverter<string, string>(x => Localizer.Get(x.ElementAtOrDefault(0) ?? ""))
        };

        return mb;
    }
}