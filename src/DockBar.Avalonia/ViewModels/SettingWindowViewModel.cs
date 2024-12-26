namespace DockBar.Avalonia.ViewModels;

internal sealed class SettingWindowViewModel : ViewModelBase
{
    public GlobalViewModel Global { get; }

    public SettingWindowViewModel(GlobalViewModel global)
    {
        Global = global;
    }
}
