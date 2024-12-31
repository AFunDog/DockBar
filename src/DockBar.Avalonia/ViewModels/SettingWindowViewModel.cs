namespace DockBar.Avalonia.ViewModels;

internal sealed class SettingWindowViewModel : ViewModelBase
{
    public GlobalSetting? GlobalSetting { get; set; }

    public SettingWindowViewModel() { }

    public SettingWindowViewModel(GlobalSetting globalSetting)
    {
        GlobalSetting = globalSetting;
    }
}
