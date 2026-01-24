using System;
using System.Linq;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Data.Core;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings;
using VoiceCraft.Core.Locales;

namespace VoiceCraft.Client.Locales;

public class LocalizeExtension(object arg) : MarkupExtension
{
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var path = new CompiledBindingPathBuilder()
            .Property(
                new ClrPropertyInfo(nameof(Localizer.Instance.Language),
                    _ => Localizer.Instance.Language,
                    null,
                    typeof(string)),
                PropertyInfoAccessorFactory.CreateInpcPropertyAccessor)
            .Build();

        if (arg is string key)
        {
            return new CompiledBindingExtension(path)
            {
                Source = Localizer.Instance,
                Mode = BindingMode.OneWay,
                Converter = new FuncValueConverter<string, string>(_ => Localizer.Get(key))
            };
        }

        if (arg is not IBinding binding)
            throw new Exception("Argument must be of type IBinding or string!");

        var mb = new MultiBinding
        {
            Bindings =
            [
                binding, new CompiledBindingExtension(path) { Source = Localizer.Instance, Mode = BindingMode.OneWay }
            ],
            Mode = BindingMode.OneWay,
            Converter = new FuncMultiValueConverter<string, string>(x => Localizer.Get(x.ElementAtOrDefault(0) ?? ""))
        };

        return mb;
    }
}