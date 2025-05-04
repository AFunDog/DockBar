using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Zeng.CoreLibrary.Toolkit.Contacts;
using DockBar.Core.Helpers;
using DockBar.Core.Structs;
using MessagePack;
using Serilog;

namespace DockBar.AvaloniaApp.Services;

public class AppSettingProvider : IDataProvider<AppSetting>
{
    private ILogger Logger { get; set; }

    public AppSettingProvider()
        : this(Log.Logger)
    {
    }

    public AppSettingProvider(ILogger logger)
    {
        Logger = logger;
    }

    public void LoadData()
    {
        using var _ = LogHelper.Trace();
        var filePath = App.SettingFile;
        if (File.Exists(filePath) is false)
        {
            Logger.Warning("全局设置文件 {FilePath} 不存在 加载跳过", filePath);
            return;
        }

        try
        {
            using var fs = File.OpenRead(filePath);
            AppSetting = MessagePackSerializer.Deserialize<AppSetting>(fs);
            DataChanged?.Invoke(this);
            Logger.Information("从 {FilePath} 加载全局设置成功", filePath);
        }
        catch (Exception e)
        {
            Logger.Error(e, "从 {FilePath} 加载全局" + "设置失败", filePath);
        }
    }

    public Task LoadDataAsync()
        => Task.Run(LoadData);

    private AppSetting? AppSetting { get; set; }

    public IEnumerable<AppSetting> Datas => AppSetting is null ? [] : [AppSetting];
    public event Action<IDataProvider<AppSetting>>? DataChanged;
}