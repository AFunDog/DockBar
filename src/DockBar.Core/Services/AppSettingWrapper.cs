using System.ComponentModel;
using DockBar.Core.Contacts;
using DockBar.Core.Structs;
using MessagePack;
using Serilog;
using Zeng.CoreLibrary.Toolkit.Contacts;
using Zeng.CoreLibrary.Toolkit.Logging;

namespace DockBar.Core.Services;

internal sealed class AppSettingWrapper : IAppSettingWrapper
{
    private ILogger Logger { get; }

    public AppSetting Data { get; }

    public AppSettingWrapper(ILogger logger, IDataProvider<AppSetting> appSettingProvider)
    {
        Logger = logger.ForContext<AppSettingWrapper>();
        Data = appSettingProvider.Data ?? new();

        Data.PropertyChanged += OnDataPropertyChanged;
    }

    private void OnDataPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Log.Logger.Trace().Debug("AppSetting 属性改变 {Property}", e.PropertyName);
    }


    public void Save(string filePath)
    {
        try
        {
            using var fs = File.OpenWrite(filePath);
            MessagePackSerializer.Serialize(fs, Data);

            Logger.Trace().Information("保存全局设置到 {FilePath} 成功", filePath);
        }
        catch (Exception e)
        {
            Logger.Trace().Error(e, "保存全局设置到 {FilePath} 失败", filePath);
        }
    }
}