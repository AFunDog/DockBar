using System.IO;
using Avalonia;
using CoreLibrary.Toolkit.Services.Setting;
using CoreLibrary.Toolkit.Services.Setting.Structs;

namespace DockBar.Avalonia.ViewModels;

internal enum DockPanelPositionType
{
    Left,
    Center,
    Right,
}

internal sealed partial class GlobalViewModel : ViewModelBase
{
    const string StorageFile = ".settings";

    public static GlobalViewModel Instance { get; private set; }

    ISettingService SettingService { get; }

    public static readonly SettingProperty<double> DockItemSizeSettingProperty = new SettingProperty<double>(
        typeof(GlobalViewModel),
        nameof(DockItemSize),
        108
    );

    public static readonly SettingProperty<double> DockItemSpacingSettingProperty = new SettingProperty<double>(
        typeof(GlobalViewModel),
        nameof(DockItemSpacing),
        16
    );

    public static readonly SettingProperty<double> DockItemExtendRateSettingProperty = new SettingProperty<double>(
        typeof(GlobalViewModel),
        nameof(DockItemExtendRate),
        0.2
    );

    public static readonly SettingProperty<bool> IsAutoPositionSettingProperty = new SettingProperty<bool>(
        typeof(GlobalViewModel),
        nameof(IsAutoPosition),
        true
    );

    public static readonly SettingProperty<DockPanelPositionType> DockPanelPositionSettingProperty =
        new SettingProperty<DockPanelPositionType>(typeof(GlobalViewModel), nameof(DockPanelPosition), DockPanelPositionType.Center);

    public static readonly SettingProperty<double> AutoPositionBottomSettingProperty = new SettingProperty<double>(
        typeof(GlobalViewModel),
        nameof(AutoPositionBottom),
        108
    );

    public GlobalViewModel(ISettingService settingService)
    {
        SettingService = settingService;
        SettingService.RegisterModel(typeof(GlobalViewModel));
        if (File.Exists(StorageFile))
            SettingService.LoadData(StorageFile);
        else
            File.Create(StorageFile).Close();
        Instance = this;
    }

    public bool IsAutoPosition
    {
        get => IsAutoPositionSettingProperty.GetValue(SettingService);
        set
        {
            IsAutoPositionSettingProperty.SetValue(SettingService, value);
            OnPropertyChanged();
        }
    }

    public DockPanelPositionType DockPanelPosition
    {
        get => DockPanelPositionSettingProperty.GetValue(SettingService);
        set
        {
            DockPanelPositionSettingProperty.SetValue(SettingService, value);
            OnPropertyChanged();
        }
    }

    public double AutoPositionBottom
    {
        get => AutoPositionBottomSettingProperty.GetValue(SettingService);
        set
        {
            AutoPositionBottomSettingProperty.SetValue(SettingService, value);
            OnPropertyChanged();
        }
    }

    public double DockItemSize
    {
        get => DockItemSizeSettingProperty.GetValue(SettingService);
        set
        {
            DockItemSizeSettingProperty.SetValue(SettingService, value);
            OnPropertyChanged();
        }
    }

    public double DockItemSpacing
    {
        get => DockItemSpacingSettingProperty.GetValue(SettingService);
        set
        {
            DockItemSpacingSettingProperty.SetValue(SettingService, value);
            OnPropertyChanged();
            OnPropertyChanged(nameof(DockItemListMargin));
        }
    }

    public double DockItemExtendRate
    {
        get => DockItemExtendRateSettingProperty.GetValue(SettingService);
        set
        {
            DockItemExtendRateSettingProperty.SetValue(SettingService, value);
            OnPropertyChanged();
        }
    }

    public Thickness DockItemListMargin => new(DockItemSpacing, 0, DockItemSpacing, 0);

    public void SaveSettings()
    {
        SettingService.SaveData(StorageFile);
    }
}
