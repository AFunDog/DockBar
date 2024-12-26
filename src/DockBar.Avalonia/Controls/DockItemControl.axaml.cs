using System;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using DockBar.Avalonia.ViewModels;
using DockBar.Core;

namespace DockBar.Avalonia.Controls;

internal class DockItemControl : TemplatedControl
{
    public static readonly DirectProperty<DockItemControl, IImage?> DockIconProperty = AvaloniaProperty.RegisterDirect<
        DockItemControl,
        IImage?
    >(nameof(DockIcon), o => o.DockIcon);

    public static readonly DirectProperty<DockItemControl, string> ShowNameProperty = AvaloniaProperty.RegisterDirect<
        DockItemControl,
        string
    >(nameof(ShowName), o => o.ShowName);

    public static readonly StyledProperty<IDockItem> DockItemProperty = AvaloniaProperty.Register<DockItemControl, IDockItem>(
        nameof(DockItem)
    );

    public static readonly StyledProperty<double> SizeProperty = AvaloniaProperty.Register<DockItemControl, double>(nameof(Size));

    public static readonly DirectProperty<DockItemControl, double> BigSizeProperty = AvaloniaProperty.RegisterDirect<
        DockItemControl,
        double
    >(nameof(BigSize), o => o.Size * (1 + GlobalViewModel.Instance.DockItemExtendRate));

    public IImage? DockIcon
    {
        get =>
            string.IsNullOrEmpty(DockItem.IconPath)
                ? new Bitmap(AssetLoader.Open(new Uri("avares://DockBar/Assets/icon.png")))
                : new Bitmap(DockItem.IconPath);
    }

    public string ShowName
    {
        get => DockItem.ShowName;
    }

    public IDockItem DockItem
    {
        get => GetValue(DockItemProperty);
        set
        {
            SetValue(DockItemProperty, value);
            RaisePropertyChanged(DockIconProperty, null, DockIcon);
            RaisePropertyChanged(ShowNameProperty, "", ShowName);
        }
    }

    public double Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public double BigSize
    {
        get => GetValue(BigSizeProperty);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        //const string ExePath = "C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe";

        if (e.Pointer.Type is PointerType.Mouse && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            DockItem.Start();
        }
    }
}
