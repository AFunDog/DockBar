using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Win32.Input;
using DockBar.AvaloniaApp.Structs;
using DockBar.Core.Structs;
using DockBar.AvaloniaApp.Extensions;

namespace DockBar.AvaloniaApp.Controls;

[TemplatePart("PART_ClearButton", typeof(Button))]
public sealed partial class HotKeyEditBox : TemplatedControl
{
    public static readonly StyledProperty<HotKeyInfo> HotKeyProperty
        = AvaloniaProperty.Register<HotKeyEditBox, HotKeyInfo>(nameof(HotKey));

    public HotKeyInfo HotKey
    {
        get => GetValue(HotKeyProperty);
        set => SetValue(HotKeyProperty, value);
    }

    public HotKeyEditBox()
    {
        // HotKeyManager
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        var partButton = this.GetTemplateChildren().OfType<Button>().First(c => c.Name == "PART_ClearButton");
        partButton.Click += (_, _) => { ClearHotKey(); };
    }

    private void ClearHotKey()
    {
        HotKey = default;
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        // e.KeyModifiers
        // App.Instance.Logger.Debug("OnKeyUp {Key}", HotKeyInfo.FromKey(e.Key,e.KeyModifiers));
        // App.Instance.Logger.Debug("OnKeyUp {Mod} {Key}",e.KeyModifiers,e.Key);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        // App.Instance.Logger.Debug("OnKeyDown {Key}", HotKeyInfo.FromKey(e.Key,e.KeyModifiers));
        // App.Instance.Logger.Debug("OnKeyDown {Mod} {Key}", e.KeyModifiers,e.Key);
        var hotKey = new HotKeyInfo().FromKey(e.Key, e.KeyModifiers);
        if (hotKey.IsValid())
            HotKey = hotKey;
        e.Handled = true;
    }
}