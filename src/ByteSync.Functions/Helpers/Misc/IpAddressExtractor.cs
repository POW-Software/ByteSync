using System.Net;
using Microsoft.Azure.Functions.Worker.Http;

namespace ByteSync.Functions.Helpers.Misc;

public static class IpAddressExtractor
{
    public static string ExtractIpAddress(this HttpRequestData req)
    {
        var headerDictionary = req.Headers.ToDictionary(x => x.Key.ToLower(), x => x.Value, StringComparer.Ordinal);
        if (headerDictionary.TryGetValue("x-forwarded-for", out var headerValues))
        {
            var ipSegment = headerValues.FirstOrDefault()?.Split(',')[0]?.Trim();
            if (!string.IsNullOrEmpty(ipSegment))
            {
                // Cases where the IPv6 address is in square brackets, e.g. “[2001:db8::1]:1234”
                if (ipSegment.StartsWith("["))
                {
                    var endBracketIndex = ipSegment.IndexOf("]", StringComparison.Ordinal);
                    if (endBracketIndex > 1)
                    {
                        var ipPart = ipSegment.Substring(1, endBracketIndex - 1);
                        if (IPAddress.TryParse(ipPart, out var ipAddress))
                        {
                            return ipAddress.ToString();
                        }
                    }
                }
                else
                {
                    // Direct parsing attempt (IPv4 without port or IPv6 without port)
                    if (IPAddress.TryParse(ipSegment, out var ipAddress))
                    {
                        return ipAddress.ToString();
                    }
                    
                    // If it is an IPv4 with a port, there will normally be a single ':'
                    if (ipSegment.Count(c => c == ':') == 1)
                    {
                        var possibleIpv4 = ipSegment.Split(':')[0];
                        if (IPAddress.TryParse(possibleIpv4, out ipAddress))
                        {
                            return ipAddress.ToString();
                        }
                    }
                }
            }
        }
        
        return "";
    }
}