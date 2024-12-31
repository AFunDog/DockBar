using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;

namespace DockBar.Core.Helpers;

internal static class IconHelper
{
    /// <summary>
    /// 获取文件、目录、快捷方式等的文件大图标
    /// </summary>
    /// <param name="path">指定路径</param>
    /// <returns></returns>
    public static Bitmap? ExtractFileIcon(string path)
    {
        try
        {
            Vanara.PInvoke.Shell32.SHFILEINFO info = default;
            nint iconHandle = Vanara.PInvoke.Shell32.SHGetFileInfo(
                path,
                0,
                ref info,
                Marshal.SizeOf(info),
                Vanara.PInvoke.Shell32.SHGFI.SHGFI_OPENICON | Vanara.PInvoke.Shell32.SHGFI.SHGFI_SYSICONINDEX
            );
            // IID_IImageList GUID
            Guid guid = new("46EB5926-582E-4017-9FDF-E8998DAA0950");
            var res = Vanara.PInvoke.Shell32.SHGetImageList(Vanara.PInvoke.Shell32.SHIL.SHIL_JUMBO, in guid, out var ls);
            var list = (Vanara.PInvoke.ComCtl32.IImageList)ls;
            using var ico = list.GetIcon(
                info.iIcon,
                Vanara.PInvoke.ComCtl32.IMAGELISTDRAWFLAGS.ILD_TRANSPARENT | Vanara.PInvoke.ComCtl32.IMAGELISTDRAWFLAGS.ILD_IMAGE
            );
            return Icon.FromHandle(ico.DangerousGetHandle()).ToBitmap();
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
        }
        return null;
    }
}
