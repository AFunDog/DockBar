using System.Diagnostics;
using DockBar.DockItem.Items;
using DockBar.DockItem.Structs;

namespace DockBar.DockItem.Internals;

internal sealed class DockItemCoreExecutor
{
    public async Task<bool> ExecuteLink(DockItemData dockItemData)
    {
        if (Enum.TryParse<LinkType>(dockItemData.Metadata.GetValueOrDefault("LinkType"), out var linkType) is false)
            return false;
        if (dockItemData.Metadata.GetValueOrDefault("LinkPath") is not { } linkPath)
            return false;
        
        switch (linkType)
        {
            case LinkType.Exe:
                return await Start(linkPath, Path.GetDirectoryName(linkPath));
                break;
            case LinkType.Lnk:
            case LinkType.Web:
            case LinkType.File:
            case LinkType.Folder:
                return await Start(linkPath);
                break;
            default:
                break;
        }

        return false;
        
        static async Task<bool> Start(string pathOrUrl, string? workingDir = null)
        {
            try
            {
                // 防止 Start 时间过长造成 UI 卡顿
                await Task.Run(() =>
                    {
                        var processStartInfo = new ProcessStartInfo { FileName = pathOrUrl, UseShellExecute = true };
                        if (workingDir is not null)
                            processStartInfo.WorkingDirectory = workingDir;
                        Process.Start(processStartInfo);
                    }
                );
                return true;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }

            return false;
        }
    }
}