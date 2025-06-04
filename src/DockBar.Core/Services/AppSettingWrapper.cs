using System.ComponentModel;
using DockBar.Core.Contacts;
using DockBar.Core.Helpers;
using DockBar.Core.Structs;
using MessagePack;
using Serilog;
using Zeng.CoreLibrary.Toolkit.Contacts;

namespace DockBar.Core.Services;

internal sealed class AppSettingWrapper : IAppSettingWrapper
{
    private ILogger Logger { get; }

    public AppSetting Data { get; }

    public AppSettingWrapper(ILogger logger, IDataProvider<AppSetting> appSettingProvider)
    {
        Logger = logger;
        Data = appSettingProvider.Datas.FirstOrDefault() ?? new();

        Data.PropertyChanged += OnDataPropertyChanged;
    }

    private void OnDataPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Logger.Debug("AppSetting 属性改变 {Property}", e.PropertyName);
    }


    public void Save(string filePath)
    {
        using var _ = LogHelper.Trace();
        try
        {
            using var fs = File.OpenWrite(filePath);
            MessagePackSerializer.Serialize(fs, Data);

            Logger.Information("保存全局设置到 {FilePath} 成功", filePath);
        }
        catch (Exception e)
        {
            Logger.Error(e, "保存全局设置到 {FilePath} 失败", filePath);
        }
    }
}