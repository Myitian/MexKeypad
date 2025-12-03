using MexShared;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace MexServer;

public sealed class DirectSource : NetworkHandler
{
    private readonly IPEndPoint _from;

    public DirectSource(IPEndPoint ep) : base(ep)
    {
        _from = ep.AddressFamily is AddressFamily.InterNetwork ?
            NetworkUtils.Any : NetworkUtils.IPv6Any;
        _udp.Bind(ep);
    }
    public override void StartSend()
    {
    }
    public override void StartReceive()
    {
        while (true)
        {
            try
            {
                EndPoint ep = _from;
                int received = _udp.ReceiveFrom(_buffer, ref ep);
                if (received == 0)
                    ReceiveHeartbeat();
                else
                    ReceiveData(received);
            }
            catch (Exception ex) when (ex is not ObjectDisposedException)
            {
                Console.WriteLine($"[{DateTime.UtcNow:O}] {ex.GetType().Name} {ex.Message}");
            }
        }
    }
}