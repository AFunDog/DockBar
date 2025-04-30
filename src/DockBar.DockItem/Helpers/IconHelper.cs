using System.Drawing;
using Windows.Win32.UI.Controls;
using Windows.Win32.UI.Shell;
using static Windows.Win32.PInvoke;

namespace DockBar.DockItem.Helpers;

internal static class IconHelper
{
    /// <summary>
    /// 从文件路径中提取图标
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public static Bitmap? ExtractFileIcon(string path)
    {
        //SHGetFileInfo
        //await Task.Yield();
        try
        {
            unsafe
            {
                SHFILEINFOW fileInfo = default;
                SHGetFileInfo(path, 0, &fileInfo, (uint)sizeof(SHFILEINFOW), SHGFI_FLAGS.SHGFI_OPENICON | SHGFI_FLAGS.SHGFI_SYSICONINDEX);
                SHGetImageList((int)SHIL_JUMBO, IImageList.IID_Guid, out var ppvObj);
                var list = (IImageList*)ppvObj;
                list->GetIcon(
                    fileInfo.iIcon,
                    (uint)(IMAGE_LIST_DRAW_STYLE.ILD_TRANSPARENT | IMAGE_LIST_DRAW_STYLE.ILD_IMAGE),
                    out var hIcon
                );
                // 这种方案会导致图标背景不透明
                //var bitmap = Bitmap.FromHicon(hIcon.DangerousGetHandle());

                var bitmap = Icon.FromHandle(hIcon.DangerousGetHandle()).ToBitmap();
                hIcon.Dispose();
                return TrimSmallIcon(bitmap);
            }
        }
        catch (Exception) { }

        return null;
    }

    public static async ValueTask<Bitmap?> ExtractWebIcon(string path)
    {
        try
        {
            using HttpClient client = new();

            foreach (var uri in path.ToWebUris())
            {
                if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                    try
                    {
                        // uri 会自动在最后加上 '/'
                        using var response = await client.GetAsync($"{uri}favicon.ico");
                        using var stream = response.Content.ReadAsStream();
                        var bitmap = new Bitmap(stream);
                        return bitmap;
                    }
                    catch (Exception) { }
            }

            //Uri.TryCreate(path, UriKind.Absolute, out Uri? uri);
            //if (uri is { } && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            //{
            //    try
            //    {
            //        // uri 会自动在最后加上 '/'
            //        using var response = await client.GetAsync($"{uri}favicon.ico");
            //        using var stream = response.Content.ReadAsStream();
            //        var bitmap = new Bitmap(stream);
            //        return bitmap;
            //    }
            //    catch (Exception) { }
            //}
            //Uri.TryCreate($"https://{path}", UriKind.Absolute, out uri);
            //if (uri is { } && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            //{
            //    try
            //    {
            //        // uri 会自动在最后加上 '/'
            //        using var response = await client.GetAsync($"{uri}favicon.ico");
            //        using var stream = response.Content.ReadAsStream();
            //        var bitmap = new Bitmap(stream);
            //        return bitmap;
            //    }
            //    catch (Exception) { }
            //}
            //Uri.TryCreate($"http://{path}", UriKind.Absolute, out uri);
            //if (uri is { } && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            //{
            //    try
            //    {
            //        // uri 会自动在最后加上 '/'
            //        using var response = await client.GetAsync($"{uri}favicon.ico");
            //        using var stream = response.Content.ReadAsStream();
            //        var bitmap = new Bitmap(stream);
            //        return bitmap;
            //    }
            //    catch (Exception) { }
            //}
        }
        catch (Exception) { }
        return null;
    }

    /// <summary>
    /// 如果图标只在左上角48*48像素区域内有内容，则裁剪掉多余的透明部分
    /// </summary>
    /// <param name="bitmap"></param>
    /// <returns></returns>
    private static Bitmap TrimSmallIcon(Bitmap bitmap)
    {
        if (bitmap.PixelFormat != System.Drawing.Imaging.PixelFormat.Format32bppArgb)
        {
            return bitmap;
        }
        var data = bitmap.LockBits(
            new(0, 0, bitmap.Width, bitmap.Height),
            System.Drawing.Imaging.ImageLockMode.ReadWrite,
            bitmap.PixelFormat
        );
        unsafe
        {
            static bool IsTransparent(byte* ptr, int x, int y, int width)
            {
                // 如果透明度为0
                return ptr[(y * width + x) * 4 + 3] == 0;
            }
            var ptr = (byte*)data.Scan0;
            int smallWidth = (int)(48 * (bitmap.HorizontalResolution / 96));
            int smallHeight = (int)(48 * (bitmap.VerticalResolution / 96));

            bool transparent = true;
            for (int i = 0; i < data.Height; i++)
            {
                for (int j = 0; j < data.Width; j++)
                {
                    if (i <= smallHeight && j <= smallWidth)
                        continue;
                    if (!IsTransparent(ptr, j, i, data.Width))
                    {
                        transparent = false;
                    }
                }
            }
            bitmap.UnlockBits(data);
            if (!transparent)
            {
                return bitmap;
            }
            else
            {
                // 裁剪掉多余的透明部分
                var smallBitmap = bitmap.Clone(new(0, 0, smallWidth, smallHeight), bitmap.PixelFormat);
                bitmap.Dispose();
                return smallBitmap;
            }
        }
    }
}
