using CommunityToolkit.Mvvm.ComponentModel;
using DockBar.Core.Contacts;
using DockBar.Core.Structs;
using Serilog;

namespace DockBar.AvaloniaApp.ViewModels;

internal sealed partial class SettingViewModel : ViewModelBase
{
    public ILogger Logger { get; }
    public AppSetting AppSetting { get; set; }

    //public IEnumerable<MenuItemData> MenuItems { get; } = [new("主页",)];

    [ObservableProperty]
    public partial object? Content { get; set; }

    public SettingViewModel() : this(Log.Logger, IAppSettingWrapper.Empty)
    {
    }

    public SettingViewModel(ILogger logger, IAppSettingWrapper appSettingWrapper)
    {
        Logger = logger;
        AppSetting = appSettingWrapper.Data;
    }
}