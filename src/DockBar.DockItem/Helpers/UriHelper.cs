namespace DockBar.DockItem.Helpers;

public static class UriHelper
{
    public static IEnumerable<Uri> ToWebUris(this string? webUriString)
    {
        if (string.IsNullOrWhiteSpace(webUriString))
            yield break;
        if (Uri.TryCreate(webUriString, UriKind.Absolute, out var res))
            yield return res;
        if (Uri.TryCreate($"https://{webUriString}", UriKind.Absolute, out var resHttps))
            yield return resHttps;
        if (Uri.TryCreate($"http://{webUriString}", UriKind.Absolute, out var resHttp))
            yield return resHttp;
    }
}