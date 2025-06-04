using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Metadata;

namespace DockBar.AvaloniaApp.Controls;

public class SettingCard : TemplatedControl
{
    public static readonly StyledProperty<object?> HeaderProperty
        = AvaloniaProperty.Register<SettingCard, object?>(nameof(Header));

    public static readonly StyledProperty<object?> ContentProperty
        = AvaloniaProperty.Register<SettingCard, object?>(nameof(Content));

    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    [Content]
    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }
}