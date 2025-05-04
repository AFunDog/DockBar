using DockBar.Core.Contacts;
using DockBar.Core.Helpers;
using DockBar.Core.Structs;
using MessagePack;
using Serilog;
using Zeng.CoreLibrary.Toolkit.Contacts;

namespace DockBar.Core.Services;

internal sealed class AppSettingWrapper(ILogger logger,IDataProvider<AppSetting> appSettingProvider) : IAppSettingWrapper
{
    private ILogger Logger { get; } = logger;
    
    public AppSetting Data { get; } = appSettingProvider.Datas.FirstOrDefault() ?? new();
    public void Save(string filePath)
    {
        using var _ = LogHelper.Trace();
        try
        {
            using var fs = File.OpenWrite(filePath);
            MessagePackSerializer.Serialize(fs, this.Data);
            
            Logger.Information("保存全局设置到 {FilePath} 成功", filePath);
        }
        catch (Exception e)
        {
            Logger.Error(e, "保存全局设置到 {FilePath} 失败", filePath);
        }
    }
}