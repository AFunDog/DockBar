using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DockBar.DockItem.Helpers;
using MessagePack;
using MessagePack.Formatters;

namespace DockBar.DockItem.Structs;

internal enum LinkType
{
    Exe,
    Lnk,
    Web,
    File,
    Folder,
}

[MessagePackFormatter(typeof(DockLinkItemFormatter))]
internal sealed class DockLinkItem : IDockItem
{
    public required string Key { get; init; }

    public required string? IconPath { get; init; }

    public required string LinkPath { get; init; }

    public required LinkType LinkType { get; init; }

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
            var name = reader.ReadString();
            var linkPath = reader.ReadString();
            reader.Depth--;
            return (DockLinkItem)IDockItem.CreateDockItem(name!, linkPath!);
        }

        public void Serialize(ref MessagePackWriter writer, DockLinkItem value, MessagePackSerializerOptions options)
        {
            writer.Write(value.Key);
            writer.Write(value.LinkPath);
        }
    }
}
