using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DockBar.DockItem.Helpers;
using DockBar.DockItem.Structs;
using MessagePack.Formatters;

namespace DockBar.DockItem
{
    public interface IDockItem
    {
        string Key { get; }
        string? IconPath { get; }

        void Start();

        internal static sealed IDockItem CreateDockItem(string key, string linkPath)
        {
            var iconPath = "";

            var linkType = Path.GetExtension(linkPath) switch
            {
                ".exe" => LinkType.Exe,
                ".lnk" or ".url" => LinkType.Lnk,
                "" => LinkType.Folder,
                _ => LinkType.File,
            };

            // 处理 DockItemIcon
            if (linkType is LinkType.Web) { }
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
                        iconPath = picPath;
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
            }
            return new DockLinkItem
            {
                Key = key,
                LinkPath = linkPath,
                LinkType = linkType,
                IconPath = iconPath,
            };
        }
    }
}
