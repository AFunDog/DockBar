using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using DockBar.Avalonia.Structs;
using DockBar.Avalonia.ViewModels;
using DockBar.Core.DockItems;
using DockBar.Core.Structs;

namespace DockBar.Avalonia;

public partial class EditDockItemWindow : Window
{
    internal EditDockItemWindowViewModel ViewModel => (EditDockItemWindowViewModel)DataContext!;

    public EditDockItemWindow()
    {
        InitializeComponent();

        AddGrid.AddHandler(DragDrop.DropEvent, OnDrop);
        AddGrid.AddHandler(DragDrop.DragEnterEvent, OnDragEnter);
        AddGrid.AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
    }

    private void OnDragLeave(object? sender, DragEventArgs e) { }

    private void OnDragEnter(object? sender, DragEventArgs e) { }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        var datas = e.Data.GetFiles();

        if (datas is not null && datas.FirstOrDefault() is IStorageItem data)
        {
            ViewModel.CurrentDockItem = new WrappedDockItem
            {
                DockItem = new DockLinkItem { LinkPath = data.Path.LocalPath },
                Index = ViewModel.Index
            };
        }
    }

    private void CloseButton_Clicked(object? sender, RoutedEventArgs e)
    {
        Close();
        e.Handled = true;
    }
}
