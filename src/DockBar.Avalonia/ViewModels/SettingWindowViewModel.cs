namespace DockBar.Avalonia.ViewModels;

internal sealed class SettingWindowViewModel : ViewModelBase
{
    public AppSetting? GlobalSetting { get; set; }

    public SettingWindowViewModel() { }

    public SettingWindowViewModel(AppSetting globalSetting)
    {
        GlobalSetting = globalSetting;
    }
}
