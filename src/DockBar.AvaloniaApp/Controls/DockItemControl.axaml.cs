using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.VisualTree;
using DockBar.AvaloniaApp.Extensions;
using DockBar.Core.Helpers;
using DockBar.DockItem.Items;
using MouseButton = Avalonia.Input.MouseButton;

namespace DockBar.AvaloniaApp.Controls;

//internal sealed class DockItemEventArgs(RoutedEvent? routedEvent) : RoutedEventArgs(routedEvent) { }

internal sealed partial class DockItemControl : TemplatedControl
{
    #region Avalonia 属性

    public static readonly StyledProperty<int> DockItemKeyProperty
        = AvaloniaProperty.Register<DockItemControl, int>(nameof(DockItemKey));

    public int DockItemKey
    {
        get => GetValue(DockItemKeyProperty);
        set => SetValue(DockItemKeyProperty, value);
    }

    public static readonly StyledProperty<IImage?> DockIconProperty
        = AvaloniaProperty.Register<DockItemControl, IImage?>(nameof(DockIcon));

    public IImage? DockIcon
    {
        get => GetValue(DockIconProperty);
        set => SetValue(DockIconProperty, value);
    }

    public static readonly StyledProperty<string?> ShowNameProperty
        = AvaloniaProperty.Register<DockItemControl, string?>(nameof(ShowName));

    public string? ShowName
    {
        get => GetValue(ShowNameProperty);
        set => SetValue(ShowNameProperty, value);
    }

    public static readonly DirectProperty<DockItemControl, bool> IsPressedProperty
        = Button.IsPressedProperty.AddOwner<DockItemControl>(o => o.IsPressed);


    public bool IsPressed
    {
        get;
        private set => SetAndRaise(IsPressedProperty, ref field, value);
    }

    public static readonly DirectProperty<DockItemControl, bool> IsDragingProperty
        = AvaloniaProperty.RegisterDirect<DockItemControl, bool>(nameof(IsDraging), o => o.IsDraging);


    public bool IsDraging
    {
        get;
        private set
        {
            if (SetAndRaise(IsDragingProperty, ref field, value))
            {
                if (field)
                    RaiseEvent(new RoutedEventArgs(DockItemStartDragEvent));
                else
                    RaiseEvent(new RoutedEventArgs(DockItemEndDragEvent));
            }
        }
    }

    public static readonly StyledProperty<ICommand?> CommandProperty
        = Button.CommandProperty.AddOwner<DockItemControl>();

    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public static readonly StyledProperty<object?> CommandParameterProperty
        = Button.CommandParameterProperty.AddOwner<DockItemControl>();

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public static readonly StyledProperty<bool> CanDragProperty
        = AvaloniaProperty.Register<DockItemControl, bool>(nameof(CanDrag), false);

    public bool CanDrag
    {
        get => GetValue(CanDragProperty);
        set => SetValue(CanDragProperty, value);
    }

    #endregion

    #region Avalonia 事件

    public static readonly RoutedEvent<RoutedEventArgs> DockItemStartDragEvent
        = RoutedEvent.Register<DockItemControl, RoutedEventArgs>(
            nameof(DockItemStartDragEvent),
            RoutingStrategies.Bubble
        );

    public static readonly RoutedEvent<RoutedEventArgs> DockItemEndDragEvent
        = RoutedEvent.Register<DockItemControl, RoutedEventArgs>(
            nameof(DockItemEndDragEvent),
            RoutingStrategies.Bubble
        );

    #endregion

    public DockItemControl()
    {
        AddHandler(DragDrop.DropEvent, OnDrop);
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        using var _ = LogHelper.Trace();

        e.Source = this;

        //
        // if (e.Data.Contains(DataFormats.Files))
        // {
        //     foreach (var data in e.Data.GetFiles() ?? [])
        //     {
        //         var item = new DockLinkItem { LinkPath = data.Path.LocalPath };
        //         ViewModel.DockItemService.RegisterDockItem(item);
        //         ViewModel.DockItemService.Root.Insert(targetIndex, item);
        //         e.Handled = true;
        //     }
        // }
        // else if (e.Data.Contains("key"))
        // {
        //     // 由于不是插入所以按整个 DockItem 进行位置索引转化
        //     targetIndex = PosXToIndex(e.GetPosition(DockItemPanel).X -
        //                               ViewModel.AppSetting.DockItemSize / 2);
        //     ViewModel.Logger.Verbose("Drop 字符串数据 在 {Index}", targetIndex);
        //     var data = e.Data.Get("key");
        //     if (data is int key && ViewModel.DockItemService.GetDockItem(key) is { } item)
        //     {
        //         // ViewModel.DockItemService.MoveDockItemTo(key, targetIndex);
        //         // ViewModel.DockItemService.Root.Move(key, targetIndex);
        //         ViewModel.DockItemService.Root.Remove(item);
        //         ViewModel.DockItemService.Root.Insert(targetIndex, item);
        //         e.Handled = true;
        //     }
        // }
        //
        // ViewModel.IsDragMode = false;
        // ViewModel.Logger.Debug("OnDrop {Index}", targetIndex);
    }

    /// <summary>
    /// 用这个来记录按下时指针的位置
    /// </summary>
    private Point PointerPos { get; set; }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        // 当有左键按下时，进行标记并记录按下位置
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            IsPressed = true;
            PointerPos = e.GetPosition(null);
            e.Handled = true;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        // 这里进行拖拽操作

        async void ToDragDrop()
        {
            try
            {
                var data = new DataObject();
                data.Set("key", DockItemKey);
                var result = await DragDrop.DoDragDrop(e, data, DragDropEffects.Move);
            }
            catch (Exception ex)
            {
                App.Instance.Logger.Error(ex, "DockItemControl 尝试拖拽");
            }

            IsDraging = false;
        }

        if (CanDrag is false)
            return;

        // 如果指针移动位置了再切换到拖放模式 以免打不开 DockItem
        if (IsPressed && !IsDraging && PointerPos != e.GetPosition(null))
        {
            IsDraging = true;
            ToDragDrop();
            e.Handled = true;
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        // 这里进行 Click 操作

        if (IsPressed && e.InitialPressMouseButton == MouseButton.Left)
        {
            IsPressed = false;
            e.Handled = true;
            if (this.GetVisualsAt(e.GetPosition(this)).Any((Visual c) => this == c || this.IsVisualAncestorOf(c)))
                OnClick();
        }
    }

    private void OnClick()
    {
        Command?.Execute(CommandParameter);
    }

    /// <inheritdoc/>
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);

        IsPressed = false;
    }

    /// <inheritdoc/>
    protected override void OnLostFocus(RoutedEventArgs e)
    {
        base.OnLostFocus(e);

        IsPressed = false;
    }
}