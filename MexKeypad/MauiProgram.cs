using System.Net;
using System.Net.Sockets;

namespace MexKeypad;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        MauiAppBuilder builder = MauiApp.CreateBuilder().UseMauiApp<App>();
#if DEBUG
        builder.Logging.AddDebug();
#endif
        return builder.Build();
    }

    public static string ToStringEx(this IPAddress ip)
    {
        if (ip.AddressFamily is AddressFamily.InterNetworkV6)
            return $"[{ip}]";
        return ip.ToString();
    }
}
