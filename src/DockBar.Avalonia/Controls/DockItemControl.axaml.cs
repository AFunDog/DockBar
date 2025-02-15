using System;
using System.ComponentModel;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using DockBar.Avalonia.Extensions;
using DockBar.Avalonia.Structs;
using DockBar.Avalonia.ViewModels;
using DockBar.Core;
using DockBar.Core.DockItems;

namespace DockBar.Avalonia.Controls;

internal class DockItemControl : TemplatedControl
{
    #region DockItem

    public static readonly StyledProperty<DockItemBase?> DockItemProperty = AvaloniaProperty.Register<DockItemControl, DockItemBase?>(
        nameof(DockItem)
    );
    public DockItemBase? DockItem
    {
        get => GetValue(DockItemProperty);
        set => SetValue(DockItemProperty, value);
    }
    #endregion

    #region DockIcon


    public static readonly DirectProperty<DockItemControl, IImage?> DockIconProperty = AvaloniaProperty.RegisterDirect<
        DockItemControl,
        IImage?
    >(nameof(DockIcon), o => o.DockIcon);

    public IImage? DockIcon
    {
        get
        {
            if (DockItem is null || DockItem.IconData is null)
                return null;
            return new MemoryStream(DockItem.IconData).ToIImage();
        }
    }
    #endregion

    #region Size

    public static readonly StyledProperty<double> SizeProperty = AvaloniaProperty.Register<DockItemControl, double>(nameof(Size));
    public double Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }
    #endregion

    #region ExtendRate

    public static readonly StyledProperty<double> ExtendRateProperty = AvaloniaProperty.Register<DockItemControl, double>(
        nameof(ExtendRate)
    );

    public double ExtendRate
    {
        get => GetValue(ExtendRateProperty);
        set => SetValue(ExtendRateProperty, value);
    }
    #endregion

    #region ExtendSize

    public static readonly DirectProperty<DockItemControl, double> ExtendSizeProperty = AvaloniaProperty.RegisterDirect<
        DockItemControl,
        double
    >(nameof(ExtendSize), o => o.ExtendSize);

    public double ExtendSize
    {
        get => Size * (1 + ExtendRate);
    }

    #endregion

    #region IsShowName

    public static readonly StyledProperty<bool> IsShowNameProperty = AvaloniaProperty.Register<DockItemControl, bool>(nameof(IsShowName));

    public bool IsShowName
    {
        get => GetValue(IsShowNameProperty);
        set => SetValue(IsShowNameProperty, value);
    }
    #endregion

    #region ShowName

    public static readonly DirectProperty<DockItemControl, string?> ShowNameProperty = AvaloniaProperty.RegisterDirect<
        DockItemControl,
        string?
    >(nameof(ShowName), o => o.ShowName);

    public string? ShowName
    {
        get => DockItem?.ShowName;
    }
    #endregion



    public DockItemControl() { }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == DockItemProperty)
        {
            if (change.GetOldValue<DockItemBase?>() is DockItemBase oldDockItem)
            {
                oldDockItem.PropertyChanged -= OnDockItemPropertyChanged;
            }
            if (change.GetNewValue<DockItemBase?>() is DockItemBase dockItem)
            {
                dockItem.PropertyChanged += OnDockItemPropertyChanged;
            }
            RaisePropertyChanged(DockIconProperty, null, DockIcon);
            RaisePropertyChanged(ShowNameProperty, null, ShowName);
        }
    }

    private void OnDockItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DockItemBase.IconData))
        {
            RaisePropertyChanged(DockIconProperty, null, DockIcon);
        }
        else if (e.PropertyName == nameof(DockItemBase.ShowName))
        {
            RaisePropertyChanged(ShowNameProperty, null, ShowName);
        }
    }

    //protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    //{
    //    base.OnApplyTemplate(e);
    //}

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        //const string ExePath = "C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe";

        if (e.Pointer.Type is PointerType.Mouse && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            DockItem?.Start();
        }
    }
}
