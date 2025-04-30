using System.Diagnostics;
using System.Drawing.Imaging;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using DockBar.DockItem.Helpers;
using DockBar.Core.Helpers;
using MessagePack;

namespace DockBar.DockItem.Structs;

// HACK DockLinkItem 在网络方面获取名称不会被覆盖的问题

public enum LinkType
{
    Undefined,

    Exe,
    Lnk,
    Web,
    File,
    Folder,
}

[MessagePackObject(AllowPrivate = true)]
public partial class DockLinkItem : DockItemBase
{
    /// <summary>
    /// 是否自动检测链接类型
    /// </summary>
    [ObservableProperty]
    [Key(nameof(IsAutoDetectLinkType))]
    public partial bool IsAutoDetectLinkType { get; set; } = true;

    // LinkType 必须在 LinkPath 的位置前面
    // 因为在反序列化时，LinkType 会被先被反序列化
    // 如果 LinkPath 先被反序列化那么会导致 LinkType 被设置时会覆盖 LinkPath
    
    /// <summary>
    /// 链接类型
    /// </summary>
    [ObservableProperty]
    [Key(nameof(LinkType))]
    public partial LinkType LinkType { get; set; }
    /// <summary>
    /// 链接路径
    /// </summary>
    [ObservableProperty]
    [Key(nameof(LinkPath))]
    public partial string? LinkPath { get; set; }


    public DockLinkItem()
    {
        // 默认为可执行状态
        CanExecuteCore = true;
    }

    // public override async void RefreshData()
    // {
    //     base.RefreshData();
    //     if (IsAutoDetectLinkType)
    //        await DetectLinkTypeFromLinkPath(LinkPath);
    //     if (UseGeneratedShowName)
    //         GetShowNameFromPath(LinkPath);
    //     if (UseGeneratedIcon)
    //         GetIconDataFromPath(LinkPath);
    // }

    protected override void ExecuteCore()
    {
        using var _ = LogHelper.Trace();
        Logger.Verbose("DockLinItem 执行 {Path}",LinkPath);
        if (LinkPath is null)
            return;
        switch (LinkType)
        {
            case LinkType.Exe:
                Start(LinkPath, Path.GetDirectoryName(LinkPath));
                break;
            case LinkType.Lnk:
            case LinkType.Web:
            case LinkType.File:
            case LinkType.Folder:
                Start(LinkPath);
                break;
            default:
                break;
        }
    }

    partial void OnLinkPathChanged(string? value)
    {
        if (IsAutoDetectLinkType)
            DetectLinkTypeFromLinkPath(LinkPath);
    }
    
    partial void OnLinkTypeChanged(LinkType value)
    {
        if (UseGeneratedShowName)
            GetShowNameFromPath(LinkPath);
        if (UseGeneratedIcon)
            GetIconDataFromPath(LinkPath);
    }

    private async Task DetectLinkTypeFromLinkPath(string? path)
    {
        async Task TestWeb(string weburi)
        {
            using var client = new HttpClient();
            foreach (var uri in weburi.ToWebUris())
            {
                if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                    try
                    {
                        var message = (await client.GetAsync(uri)).EnsureSuccessStatusCode();
                        LinkType = LinkType.Web;
                    }
                    catch (Exception)
                    {
                    }
            }
        }

        if (string.IsNullOrEmpty(path))
        {
            LinkType = LinkType.Undefined;
        }
        else
        {
            if (Path.Exists(path))
            {
                LinkType = LinkType.File;
                if (Directory.Exists(path))
                {
                    LinkType = LinkType.Folder;
                }
                else if (path.EndsWith(".exe"))
                {
                    LinkType = LinkType.Exe;
                }
                else if (path.EndsWith(".lnk"))
                {
                    LinkType = LinkType.Lnk;
                }
            }
            else
            {
                await TestWeb(path);
            }
        }
    }

    private void GetShowNameFromPath(string? path)
    {
        async void GetWebTitle()
        {
            const string Pattern = @"<title>(.+)</title>";
            using var client = new HttpClient();
            foreach (var uri in path.ToWebUris())
            {
                if (uri is { } && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                    try
                    {
                        var content = await client.GetStringAsync(uri);
                        var match = Regex.Match(content, Pattern, RegexOptions.Compiled);
                        if (match.Success)
                        {
                            ShowName = match.Groups[1].Value;
                            return;
                        }
                    }
                    catch (Exception)
                    {
                    }
            }
            //Uri.TryCreate(weburi, UriKind.Absolute, out var weburi);
            //if (weburi is { } && (weburi.Scheme == Uri.UriSchemeHttp || weburi.Scheme == Uri.UriSchemeHttps))
            //    try
            //    {
            //        var content = await client.GetStringAsync(weburi);
            //        var match = Regex.Match(content, Pattern, RegexOptions.Compiled);
            //        if (match.Success)
            //        {
            //            ShowName = match.Groups[1].Value;
            //            return;
            //        }
            //    }
            //    catch (Exception) { }
            //Uri.TryCreate($"https://{weburi}", UriKind.Absolute, out weburi);
            //if (weburi is { } && (weburi.Scheme == Uri.UriSchemeHttp || weburi.Scheme == Uri.UriSchemeHttps))
            //    try
            //    {
            //        var content = await client.GetStringAsync(weburi);
            //        var match = Regex.Match(content, Pattern, RegexOptions.Compiled);
            //        if (match.Success)
            //        {
            //            ShowName = match.Groups[1].Value;
            //            return;
            //        }
            //    }
            //    catch (Exception) { }
            //Uri.TryCreate($"http://{weburi}", UriKind.Absolute, out weburi);
            //if (weburi is { } && (weburi.Scheme == Uri.UriSchemeHttp || weburi.Scheme == Uri.UriSchemeHttps))
            //    try
            //    {
            //        var content = await client.GetStringAsync(weburi);
            //        var match = Regex.Match(content, Pattern, RegexOptions.Compiled);
            //        if (match.Success)
            //        {
            //            ShowName = match.Groups[1].Value;
            //            return;
            //        }
            //    }
            //    catch (Exception) { }
        }

        if (path == null)
        {
            ShowName = null;
            return;
        }

        switch (LinkType)
        {
            case LinkType.Undefined:
                ShowName = null;
                break;
            case LinkType.Web:
                GetWebTitle();
                break;
            case LinkType.Folder:
                ShowName = Path.GetFileName(Path.GetDirectoryName(path));
                break;
            default:
                ShowName = Path.GetFileName(path);
                break;
        }
    }

    private void GetIconDataFromPath(string? path)
    {
        async void GetIconDataFromWeb()
        {
            using var bmp = await IconHelper.ExtractWebIcon(path);
            if (bmp is not null)
            {
                using var ms = new MemoryStream();
                bmp.Save(ms, ImageFormat.Png);
                IconData = ms.ToArray();
            }
        }

        if (path is null)
        {
            IconData = null;
            return;
        }

        if (LinkType == LinkType.Undefined)
        {
            IconData = null;
            return;
        }

        if (LinkType is LinkType.Web)
        {
            GetIconDataFromWeb();
        }
        else
        {
            // 处理 DockItemIcon
            using var bmp = IconHelper.ExtractFileIcon(path);
            if (bmp is not null)
            {
                using var ms = new MemoryStream();
                bmp.Save(ms, ImageFormat.Png);
                IconData = ms.ToArray();
            }
        }
    }

    private static void Start(string pathOrUrl, string? workingDir = null)
    {
        try
        {
            var processStartInfo = new ProcessStartInfo() { FileName = pathOrUrl, UseShellExecute = true };
            if (workingDir is not null)
                processStartInfo.WorkingDirectory = workingDir;
            Process.Start(processStartInfo);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
        }
    }

    //internal class DockLinkItemFormatter : IMessagePackFormatter<DockLinkItem?>
    //{
    //    public DockLinkItem? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    //    {
    //        if (reader.TryReadNil())
    //            return null;

    //        options.Security.DepthStep(ref reader);
    //        var key = reader.ReadString();
    //        var showName = reader.ReadString();
    //        var linkPath = reader.ReadString();
    //        reader.Depth--;
    //        if (key is null || showName is null || linkPath is null)
    //            return null;
    //        return new DockLinkItem(key, linkPath) { ShowName = showName };
    //    }

    //    public void Serialize(ref MessagePackWriter writer, DockLinkItem? value, MessagePackSerializerOptions options)
    //    {
    //        if (value is null)
    //        {
    //            writer.WriteNil();
    //            return;
    //        }
    //        writer.Write(value.Key);
    //        writer.Write(value.ShowName);
    //        writer.Write(value.LinkPath);
    //    }
    //}
}