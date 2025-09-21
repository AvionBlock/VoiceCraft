using System;
using System.Linq;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Jeek.Avalonia.Localization;

namespace VoiceCraft.Client.Locales;

public class LocalizeExtension(object arg)
{
    public object ProvideValue(IServiceProvider serviceProvider)
    {
        if (arg is string key)
            return new MultiBinding()
            {
                Bindings = [new Binding { Source = key }, new Binding { Source = Localizer.Language }],
                Mode = BindingMode.TwoWay,
                Converter = new FuncMultiValueConverter<string, string>(x => Localizer.Get(x.ElementAt(0) ?? ""))
            };

        if (arg is not IBinding binding)
            throw new Exception("Argument must be of type IBinding or string!");

        var mb = new MultiBinding()
        {
            Bindings = [binding, new Binding { Source = Localizer.Language }],
            Mode = BindingMode.TwoWay,
            Converter = new FuncMultiValueConverter<string, string>(x => Localizer.Get(x.ElementAt(0) ?? ""))
        };
        return mb;
    }
}