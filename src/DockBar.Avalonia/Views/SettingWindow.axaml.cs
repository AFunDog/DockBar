using Avalonia.Controls;
using Avalonia.Interactivity;

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
