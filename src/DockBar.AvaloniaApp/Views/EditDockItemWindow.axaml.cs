using System;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using DockBar.AvaloniaApp.Structs;
using DockBar.AvaloniaApp.ViewModels;
using DockBar.Core.Helpers;
using DockBar.DockItem.Items;

namespace DockBar.AvaloniaApp.Views;

internal partial class EditDockItemWindow : Window
{
    internal EditDockItemWindowViewModel ViewModel => (DataContext as EditDockItemWindowViewModel)!;

    public EditDockItemWindow() : this(new EditDockItemWindowViewModel())
    {
    }

    public EditDockItemWindow(EditDockItemWindowViewModel viewModel)
    {
        using var _ = LogHelper.Trace();
        // 将 StorageProvider 传递给 ViewModel，这部分不能使用依赖注入挺麻烦的
        viewModel.StorageProvider = StorageProvider;
        DataContext = viewModel;
        InitializeComponent();

        rootGrid.AddHandler(DragDrop.DropEvent, OnDrop);
        rootGrid.AddHandler(DragDrop.DragEnterEvent, OnDragEnter);
        rootGrid.AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);

        ViewModel.Logger.Verbose("EditDockItemWindow 启动");
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        // 关闭时保存数据
        //ViewModel.DockItemService.SaveData(App.StorageFile);
        ViewModel.Logger.Verbose("EditDockItemWindow 关闭");
    }

    private void OnDragLeave(object? sender, DragEventArgs e)
    {
    }

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        var datas = e.Data.GetFiles();

        if (datas is not null && datas.FirstOrDefault() is IStorageItem data)
            ViewModel.CurrentDockItem = new DockLinkItem { LinkPath = data.Path.LocalPath, LinkType = LinkType.File };
    }

    private void CloseButton_Clicked(object? sender, RoutedEventArgs e)
    {
        Close();
        e.Handled = true;
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        ViewModel.Confirmed += Close;
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        //if (ViewModel?.IsAddMode ?? false)
        ViewModel.DockItemService?.SaveData(App.StorageFile);
    }

    // void OpenIconFile()
    // {
    //     var toplevel = TopLevel.GetTopLevel(this);
    //     
    //     toplevel.StorageProvider
    //
    // }
}