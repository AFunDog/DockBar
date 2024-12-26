using System.Diagnostics;
using System.Drawing.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using DockBar.Core.Helpers;
using MessagePack;
using MessagePack.Formatters;

namespace DockBar.Core.Structs;

public enum LinkType
{
    Exe,
    Lnk,
    Web,
    File,
    Folder,
}

[MessagePackFormatter(typeof(DockLinkItemFormatter))]
public sealed partial class DockLinkItem : ObservableObject, IDockItem
{
    public string Key { get; }

    [ObservableProperty]
    private string _showName;

    //public string ShowName { get; set; }
    public string? IconPath { get; }

    public string LinkPath { get; }

    public LinkType LinkType { get; }

    public DockLinkItem(string key, string linkPath)
    {
        LinkType = Path.GetExtension(linkPath) switch
        {
            ".exe" => LinkType.Exe,
            ".lnk" or ".url" => LinkType.Lnk,
            "" => LinkType.Folder,
            _ => LinkType.File,
        };

        // 处理 DockItemIcon
        if (LinkType is LinkType.Web) { }
        else
        {
            try
            {
                using var bmp = IconHelper.ExtractFileIcon(linkPath);
                if (bmp is not null)
                {
                    var picPath = $"Assets/Temp/{Path.GetFileNameWithoutExtension(linkPath)}.png";
                    if (!Directory.Exists(Path.GetDirectoryName(picPath)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(picPath)!);
                    }
                    bmp.Save(picPath, ImageFormat.Png);
                    IconPath = picPath;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        Key = key;
        ShowName = key;
        LinkPath = linkPath;
    }

    public void Start()
    {
        switch (LinkType)
        {
            case LinkType.Exe:
                Start(LinkPath, Path.GetDirectoryName(LinkPath));
                break;
            case LinkType.Lnk:
                Start(LinkPath);
                break;
            case LinkType.Web:
                Start(LinkPath);
                break;
            case LinkType.File:
                Start(LinkPath);
                break;
            case LinkType.Folder:
                Start(LinkPath);
                break;
            default:
                break;
        }
    }

    private static void Start(string pathOrUrl, string? workingDir = null)
    {
        try
        {
            var processStartInfo = new ProcessStartInfo() { FileName = pathOrUrl, UseShellExecute = true, };
            if (workingDir is not null)
                processStartInfo.WorkingDirectory = workingDir;
            Process.Start(processStartInfo);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
        }
    }

    class DockLinkItemFormatter : IMessagePackFormatter<DockLinkItem>
    {
        public DockLinkItem Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            options.Security.DepthStep(ref reader);
            var key = reader.ReadString();
            var showName = reader.ReadString();
            var linkPath = reader.ReadString();
            reader.Depth--;
            if (key is null || showName is null || linkPath is null)
                return null;
            return new DockLinkItem(key, linkPath) { ShowName = showName };
        }

        public void Serialize(ref MessagePackWriter writer, DockLinkItem value, MessagePackSerializerOptions options)
        {
            writer.Write(value.Key);
            writer.Write(value.ShowName);
            writer.Write(value.LinkPath);
        }
    }
}
