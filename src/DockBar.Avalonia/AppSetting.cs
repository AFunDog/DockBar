using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using DockBar.Avalonia.Structs;
using DockBar.Shared.Helpers;
using Mapster;
using MessagePack;
using Serilog;

namespace DockBar.Avalonia;

internal sealed partial class AppSetting : ObservableObject
{
    private ILogger Logger { get; } = Log.Logger;

    [ObservableProperty]
    public partial double DockItemSize { get; set; } = 108;

    [ObservableProperty]
    public partial bool DockItemIsShowName { get; set; } = true;

    [ObservableProperty]
    public partial double DockItemNameFontSize { get; set; } = 14;

    [ObservableProperty]
    public partial double DockItemSpacing { get; set; } = 16;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DockItemExtendSize))]
    public partial double DockItemExtendRate { get; set; } = 0.2;

    public double DockItemExtendSize => DockItemSize * (1 + DockItemExtendRate);

    [ObservableProperty]
    public partial bool IsAutoPosition { get; set; } = true;

    [ObservableProperty]
    public partial DockPanelPositionType DockPanelPosition { get; set; } = DockPanelPositionType.Center;

    [ObservableProperty]
    public partial double AutoPositionBottom { get; set; } = 108;

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
            MessagePackSerializer.Deserialize<Dictionary<string, object>>(fs).Adapt(this);
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
            MessagePackSerializer.Serialize(fs, this.Adapt<Dictionary<string, object>>());
            Logger.Information("保存全局设置到 {FilePath} 成功", filePath);
        }
        catch (Exception e)
        {
            Logger.Error(e, "保存全局设置到 {FilePath} 失败", filePath);
        }
    }
}
