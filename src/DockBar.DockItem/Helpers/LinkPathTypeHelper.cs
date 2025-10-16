using DockBar.DockItem.Items;

namespace DockBar.DockItem.Helpers;

public static class LinkPathTypeHelper
{
    public static async Task<LinkType> DetectLinkTypeFromLinkPath(string? path)
    {
        async Task<LinkType> TestWeb(string webUri)
        {
            using var client = new HttpClient();
            foreach (var uri in webUri.ToWebUris())
                if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                    try
                    {
                        var message = (await client.GetAsync(uri)).EnsureSuccessStatusCode();
                        return LinkType.Web;
                    }
                    catch (Exception)
                    {
                    }
            return LinkType.Undefined;
        }

        if (string.IsNullOrEmpty(path))
        {
            return LinkType.Undefined;
        }
        else
        {
            if (Path.Exists(path))
            {
                if (Directory.Exists(path))
                    return LinkType.Folder;
                else if (path.EndsWith(".exe"))
                    return LinkType.Exe;
                else if (path.EndsWith(".lnk"))
                    return LinkType.Lnk;
                return LinkType.File;
            }
            else
            {
                return await TestWeb(path);
            }
        }
    }
}
