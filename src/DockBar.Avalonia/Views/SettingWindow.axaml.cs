using Avalonia.Controls;
using Avalonia.Interactivity;
using DockBar.Avalonia.ViewModels;

namespace DockBar.Avalonia;

public partial class SettingWindow : Window
{
    internal SettingWindowViewModel? ViewModel => DataContext as SettingWindowViewModel;

    public SettingWindow()
    {
        InitializeComponent();
    }

    private void CloseButton_Clicked(object? sender, RoutedEventArgs e)
    {
        Close();
        e.Handled = true;
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        ViewModel?.GlobalSetting?.SaveSetting(App.SettingFile);
    }
}
