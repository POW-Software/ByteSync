namespace ByteSync.Common.Helpers;

public static class UrlUtils
{
    public static string AppendSegment(string url, string segment)
    {
        if (string.IsNullOrEmpty(url))
        {
            return segment;
        }

        if (string.IsNullOrEmpty(segment))
        {
            return url;
        }

        if (url.EndsWith("/"))
        {
            return url + segment;
        }

        return url + "/" + segment;
    }
}