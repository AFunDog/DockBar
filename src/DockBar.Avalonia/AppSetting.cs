using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using DockBar.Avalonia.Structs;
using DockBar.Shared.Helpers;
using MessagePack;
using Serilog;

namespace DockBar.Avalonia;

[MessagePackObject(keyAsPropertyName: true)]
public sealed partial class AppSetting : ObservableObject
{
    private ILogger Logger { get; } = Log.Logger;

    #region 停靠项目尺寸

    /// <summary>
    /// 停靠项目基本尺寸
    /// </summary>
    [ObservableProperty]
    public partial double DockItemSize { get; set; } = 108;

    /// <summary>
    /// 停靠项目是否显示名称
    /// </summary>
    [ObservableProperty]
    public partial bool DockItemIsShowName { get; set; } = true;

    /// <summary>
    /// 停靠项目名称字体大小
    /// </summary>
    [ObservableProperty]
    public partial double DockItemNameFontSize { get; set; } = 14;

    /// <summary>
    /// 停靠项目间距
    /// </summary>
    [ObservableProperty]
    public partial double DockItemSpacing { get; set; } = 16;

    /// <summary>
    /// 停靠项目扩大倍数
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DockItemExtendSize))]
    public partial double DockItemExtendRate { get; set; } = 0.2;

    [IgnoreMember]
    public double DockItemExtendSize => DockItemSize * (1 + DockItemExtendRate);
    #endregion

    #region 自动定位


    /// <summary>
    /// 是否启用自动定位
    /// </summary>
    [ObservableProperty]
    public partial bool IsAutoPosition { get; set; } = true;

    /// <summary>
    /// 停靠项目自动定位位置
    /// </summary>
    [ObservableProperty]
    public partial DockPanelPositionType DockPanelPosition { get; set; } = DockPanelPositionType.Center;

    /// <summary>
    /// 自动定位距底部距离
    /// </summary>
    [ObservableProperty]
    public partial double AutoPositionBottom { get; set; } = 108;
    #endregion

    #region 颜色

    /// <summary>
    /// 停靠面板背景颜色
    /// </summary>
    /// <remarks>
    /// 这个属性不会被存储，而是会被转化为 <see cref="DockPanelBackgroundColorUInt"/> 进行存储
    /// </remarks>
    [ObservableProperty]
    [IgnoreMember]
    public partial Color DockPanelBackgroundColor { get; set; } = Colors.Black;

    /// <summary>
    /// 实际存储
    /// </summary>
    public uint DockPanelBackgroundColorUInt
    {
        get => DockPanelBackgroundColor.ToUInt32();
        set => DockPanelBackgroundColor = Color.FromUInt32(value);
    }

    [ObservableProperty]
    public partial double DockPanelBackgroundOpacity { get; set; } = 0.6;

    #endregion

    public AppSetting() { }

    public AppSetting(ILogger logger)
    {
        Logger = logger;
    }

    public void LoadSetting(string filePath)
    {
        using var _ = LogHelper.Trace();
        if (File.Exists(filePath) is false)
        {
            Logger.Warning("全局设置文件 {FilePath} 不存在 加载跳过", filePath);
            return;
        }
        try
        {
            using var fs = File.OpenRead(filePath);
            var localSetting = MessagePackSerializer.Deserialize<AppSetting>(fs);
            #region 停靠项目尺寸

            DockItemSize = localSetting.DockItemSize;
            DockItemIsShowName = localSetting.DockItemIsShowName;
            DockItemNameFontSize = localSetting.DockItemNameFontSize;
            DockItemSpacing = localSetting.DockItemSpacing;
            DockItemExtendRate = localSetting.DockItemExtendRate;
            #endregion
            #region 自动定位

            IsAutoPosition = localSetting.IsAutoPosition;
            DockPanelPosition = localSetting.DockPanelPosition;
            AutoPositionBottom = localSetting.AutoPositionBottom;

            #endregion
            #region 颜色

            DockPanelBackgroundColor = localSetting.DockPanelBackgroundColor;
            DockPanelBackgroundOpacity = localSetting.DockPanelBackgroundOpacity;

            #endregion
            Logger.Information("从 {FilePath} 加载全局设置成功", filePath);
        }
        catch (Exception e)
        {
            Logger.Error(e, "从 {FilePath} 加载全局" + "设置失败", filePath);
        }
    }

    public void SaveSetting(string filePath)
    {
        using var _ = LogHelper.Trace();
        try
        {
            using var fs = File.OpenWrite(filePath);
            MessagePackSerializer.Serialize(fs, this);
            Logger.Information("保存全局设置到 {FilePath} 成功", filePath);
        }
        catch (Exception e)
        {
            Logger.Error(e, "保存全局设置到 {FilePath} 失败", filePath);
        }
    }
}
