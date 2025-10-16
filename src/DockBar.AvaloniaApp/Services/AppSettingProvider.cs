using System;
using System.IO;
using System.Threading.Tasks;
using Zeng.CoreLibrary.Toolkit.Contacts;
using DockBar.Core.Structs;
using MessagePack;
using Serilog;
using Zeng.CoreLibrary.Toolkit.Logging;
using Zeng.CoreLibrary.Toolkit.Structs;

namespace DockBar.AvaloniaApp.Services;

public class AppSettingProvider : IDataProvider<AppSetting>
{
    private ILogger Logger { get; set; }

    public AppSettingProvider() 
    {
    }

    public AppSettingProvider(ILogger logger)
    {
        Logger = logger.ForContext<AppSettingProvider>();
    }

    public void LoadData()
    {
        var filePath = App.SettingFile;
        if (File.Exists(filePath) is false)
        {
            Logger.Trace().Warning("全局设置文件 {FilePath} 不存在 加载跳过", filePath);
            return;
        }

        try
        {
            using var fs = File.OpenRead(filePath);
            AppSetting = MessagePackSerializer.Deserialize<AppSetting>(fs);
            DataChanged?.Invoke(this, new());
            Logger.Trace().Information("从 {FilePath} 加载全局设置成功", filePath);
        }
        catch (Exception e)
        {
            Logger.Trace().Error(e, "从 {FilePath} 加载全局" + "设置失败", filePath);
        }
    }

    public Task LoadDataAsync() => Task.Run(LoadData);

    private AppSetting AppSetting { get; set; } = new();

    public AppSetting? Data => AppSetting;
    public event Action<IDataProvider<AppSetting>, DataProviderDataChangedEventArgs<AppSetting>>? DataChanged;
}