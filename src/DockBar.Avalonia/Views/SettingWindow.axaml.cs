using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace DockBar.Avalonia;

public partial class SettingWindow : Window
{
    public SettingWindow()
    {
        InitializeComponent();
    }

    private void CloseButton_Clicked(object? sender, RoutedEventArgs e)
    {
        Close();
        e.Handled = true;
    }
}
