using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Zeng.CoreLibrary.Toolkit.Avalonia.Structs;
using Zeng.CoreLibrary.Toolkit.Contacts;
using Serilog;
using Zeng.CoreLibrary.Toolkit.Structs;

namespace DockBar.AvaloniaApp.Services;

internal sealed class LocalizationDataProvider : IDataProvider<LocalizationData>
{
    const string LocalizationDir = "Localization";

    private List<LocalizationData> LocalizationDatas { get; } = [];
    public IEnumerable<LocalizationData> Datas => LocalizationDatas;

    public event Action<IDataProvider<LocalizationData>>? DataChanged;

    private ILogger Logger { get; }

    public LocalizationDataProvider(ILogger logger)
    {
        Logger = logger;
    }

    public void LoadData()
    {
        //LocalizationDatas.Clear();

        var tempData = new List<LocalizationData>();
        if (Directory.Exists(LocalizationDir) is false)
            return;
        foreach (var cultureDir in Directory.EnumerateDirectories(LocalizationDir))
        {
            try
            {
                var culture = CultureInfo.GetCultureInfo(Path.GetFileName(cultureDir));
                foreach (var locFile in Directory.EnumerateFiles(cultureDir, "*.loc"))
                {
                    try
                    {
                        // using var reader = new StreamReader(locFile, Encoding.UTF8);
                        // while (reader.ReadLine() is { } line)
                        // {
                        //     if (Regex.Match(line, @"(\S+)\s*:\s*(\S+)") is { Success: true } match)
                        //     {
                        //         tempData.Add(new LocalizationData(culture, match.Groups[1].Value, match.Groups[2].Value));
                        //     }
                        // }
                        foreach (var (key, tran) in GetLocFileTrans(File.ReadAllText(locFile)))
                        {
                            tempData.Add(new LocalizationData(culture, key, tran));
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Warning(ex, "加载本地化文件 {file} 失败跳过", locFile);
                    }
                }
            }
            catch (CultureNotFoundException ex)
            {
                Logger.Warning(ex, "{Dir} 不是有效的文化标志跳过加载", cultureDir);
            }
            catch (Exception e)
            {
                Logger.Error(e, "读取 {Dir} 本地化数据失败", cultureDir);
            }
        }
        if (tempData.Count != 0)
        {
            LocalizationDatas.Clear();
            LocalizationDatas.AddRange(tempData);
        }
        DataChanged?.Invoke(this);
    }
    
    

    public Task LoadDataAsync() => Task.Run(LoadData);
    
    private static IEnumerable<(string key, string tran)> GetLocFileTrans(string locText)
    {
        if (string.IsNullOrWhiteSpace(locText))
            yield break;
        // 翻译可以多行，所以要用 ; 结尾，文本中要使用 ; 要用 \;
        if (
            Regex.Matches(
                locText,
                @"(\S+)\s*:\s*((?:[^\\;]|\\.)*?)\s*;",
                RegexOptions.Compiled | RegexOptions.Singleline
            ) is
            { } matches
        )
        {
            for (int i = 0; i < matches.Count; i++)
            {
                var key = matches[i].Groups[1].Value;
                var value = Regex.Replace(
                    matches[i].Groups[2].Value,
                    "\\;",
                    ";",
                    RegexOptions.Compiled
                );
                yield return (key, value);
            }
        }
    }
}
