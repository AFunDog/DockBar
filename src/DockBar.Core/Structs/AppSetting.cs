using CommunityToolkit.Mvvm.ComponentModel;
using DockBar.Core.Helpers;
using MessagePack;
using Serilog;

namespace DockBar.Core.Structs;

// TODO AppSetting 应该要能动态添加删除配置设置项目，这样太死板了

[MessagePackObject(AllowPrivate = true)]
public sealed partial class AppSetting : ObservableObject
{
    
    #region 停靠项目尺寸

    /// <summary>
    /// 停靠项目基本尺寸
    /// </summary>
    [Key(nameof(DockItemSize))]
    [ObservableProperty]
    // [NotifyPropertyChangedFor(nameof(DockPanelHeight))]
    public partial double DockItemSize { get; set; } = 64;

    // [IgnoreMember]
    // public double DockPanelHeight => DockItemSize + 4;

    /// <summary>
    /// 停靠项目名称字体大小
    /// </summary>
    [ObservableProperty]
    [Key(nameof(DockItemNameFontSize))]
    public partial double DockItemNameFontSize { get; set; } = 14;

    /// <summary>
    /// 停靠项目间距
    /// </summary>
    [ObservableProperty]
    [Key(nameof(DockItemSpacing))]
    public partial double DockItemSpacing { get; set; } = 0;

    #endregion
    

    #region 颜色

    /// <summary>
    /// 停靠面板背景颜色
    /// </summary>
    /// <remarks>
    /// 这个属性不会被存储，而是会被转化为 <see cref="DockPanelBackgroundColorUInt"/> 进行存储
    /// </remarks>
    [ObservableProperty]
    [Key(nameof(DockPanelBackgroundColor))]
    public partial ColorValue DockPanelBackgroundColor { get; set; } = new(16,16,16);
        

    [ObservableProperty]
    [Key(nameof(DockPanelBackgroundOpacity))]
    public partial double DockPanelBackgroundOpacity { get; set; } = 0.6;

    #endregion

    #region 快捷键

    /// <summary>
    /// 是否启用全局快捷键
    /// </summary>
    [ObservableProperty]
    [Key(nameof(IsEnableGlobalHotKey))]
    public partial bool IsEnableGlobalHotKey { get; set; } = true;

    [ObservableProperty]
    [Key(nameof(KeepMainWindowHotKey))]
    public partial HotKeyInfo KeepMainWindowHotKey { get; set; } = default;

    #endregion
    

}