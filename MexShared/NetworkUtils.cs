using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace MexShared;

public class NetworkUtils
{
    public const int DefaultPort = 6957;
    public const int HeartbeatInterval = 10000;
    public const int HeartbeatMax = HeartbeatInterval * 4;
    public const int AnyPort = IPEndPoint.MinPort;
    public static readonly IPEndPoint Any = new(IPAddress.Any, AnyPort);
    public static readonly IPEndPoint IPv6Any = new(IPAddress.IPv6Any, AnyPort);

    public static bool TryParseEndPoint(
        string uriString,
        [NotNullWhen(true)] out string? scheme,
        [NotNullWhen(true)] out IPEndPoint? ep)
    {
        scheme = null;
        ep = null;
        if (!Uri.TryCreate(uriString, UriKind.Absolute, out Uri? uri))
            return false;
        scheme = uri.Scheme;
        int port = uri.Port < 0 ? DefaultPort : uri.Port;
        if (IPAddress.TryParse(uri.IdnHost, out IPAddress? ip0))
            ep = new(ip0, port);
        else if (Dns.GetHostAddresses(uri.IdnHost) is [IPAddress ip1, ..])
            ep = new(ip1, port);
        return ep is not null;
    }
}
