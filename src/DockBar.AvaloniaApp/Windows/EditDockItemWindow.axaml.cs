using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Messaging;
using DockBar.AvaloniaApp.Structs;
using Microsoft.Extensions.DependencyInjection;

namespace DockBar.AvaloniaApp.Windows;

internal partial class EditDockItemWindow : Window
{
    // internal EditDockItemWindowViewModel ViewModel => (DataContext as EditDockItemWindowViewModel)!;

    public EditDockItemWindow()
    {
        InitializeComponent();


        RootGrid.AddHandler(DragDrop.DropEvent, OnDrop);
        RootGrid.AddHandler(DragDrop.DragEnterEvent, OnDragEnter);
        RootGrid.AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);

        WeakReferenceMessenger.Default.Register<EditDockItemWindow, OpenFilePickerMessage, string>(
            this,
            "EditDockItemWindow.OpenFilePicker",
            (s, e) =>
            {
                e.Reply(OpenFilePicker());

                Task<IReadOnlyList<IStorageFile>> OpenFilePicker()
                {
                    return StorageProvider.OpenFilePickerAsync(e.Options);
                }
            }
        );

        WeakReferenceMessenger.Default.Register<EditDockItemWindow, OpenEditDockItemWindowMessage, string>(
            this,
            "EditDockItemWindow.OpenWindow",
            (s, e) =>
            {
                e.Reply(
                    ShowDialog<EditDockItemWindowReply?>(
                        Program.ServiceProvider.GetRequiredKeyedService<Window>(nameof(MainWindow))
                    )
                );
            }
        );
        WeakReferenceMessenger.Default.Register<EditDockItemWindow, EditDockItemWindowReply, string>(
            this,
            "EditDockItemWindow.Confirm",
            (s, e) => { Close(e); }
        );
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }

    private void OnDragLeave(object? sender, DragEventArgs e) { }

    private void OnDragEnter(object? sender, DragEventArgs e) { }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        WeakReferenceMessenger.Default.Send(e, "EditDockItemWindow.OnDrop");
    }

    // private void CloseButton_Clicked(object? sender, RoutedEventArgs e)
    // {
    //     Close(null);
    //     e.Handled = true;
    // }

    // protected override void OnDataContextChanged(EventArgs e)
    // {
    //     base.OnDataContextChanged(e);
    //
    //     // ViewModel.Confirmed += Close;
    //
    //     // StorageProvider.OpenFilePickerAsync()
    // }
    //
    // protected override void OnClosing(WindowClosingEventArgs e)
    // {
    //     base.OnClosing(e);
    //     //if (ViewModel?.IsAddMode ?? false)
    //     // ViewModel.DockItemService?.SaveData(App.StorageFile);
    // }


    // void OpenIconFile()
    // {
    //     var toplevel = TopLevel.GetTopLevel(this);
    //     
    //     toplevel.StorageProvider
    //
    // }
}