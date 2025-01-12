using System.Diagnostics;
using System.Drawing.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using DockBar.Core.Helpers;
using DockBar.Core.Internals;
using MessagePack;
using MessagePack.Formatters;
using Vanara.PInvoke;

namespace DockBar.Core.DockItems;

public enum LinkType
{
    Undefined,
    Exe,
    Lnk,
    Web,
    File,
    Folder,
}

[MessagePackObject(keyAsPropertyName: true)]
public partial class DockLinkItem : DockItemBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LinkType))]
    public partial string? LinkPath { get; set; }

    [IgnoreMember]
    public LinkType LinkType =>
        Path.GetExtension(LinkPath) switch
        {
            ".exe" => LinkType.Exe,
            ".lnk" or ".url" => LinkType.Lnk,
            "" => LinkType.Folder,
            null => LinkType.Undefined,
            _ => LinkType.File,
        };

    protected override void StartCore()
    {
        if (LinkPath is null)
            return;
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

    partial void OnLinkPathChanged(string? value)
    {
        if (value is null)
        {
            IconData = null;
            return;
        }
        if (LinkType == LinkType.Undefined)
        {
            IconData = null;
            return;
        }
        if (LinkType is LinkType.Web) { }
        else
        {
            if (LinkType is LinkType.File or LinkType.Lnk or LinkType.Exe or LinkType.Folder)
            {
                if (string.IsNullOrEmpty(ShowName))
                {
                    ShowName = Path.GetFileNameWithoutExtension(LinkPath);
                }
            }

            // 处理 DockItemIcon
            using var bmp = IconHelper.ExtractFileIcon(value);
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
