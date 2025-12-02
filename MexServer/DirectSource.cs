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
                byte[] buffer = ArrayPool<byte>.Shared.Rent(0x10000);
                int received = _udp.ReceiveFrom(buffer, ref ep);
                if (received == 0)
                {
                    ReceiveHeartbeat();
                    ArrayPool<byte>.Shared.Return(buffer);
                }
                else
                    KeyInfo.SendInput(MemoryMarshal.Cast<byte, KeyInfo>(buffer.AsSpan(0, received)));
            }
            catch (Exception ex) when (ex is not ObjectDisposedException)
            {
                Console.WriteLine($"[{DateTime.UtcNow:O}] {ex.GetType().Name} {ex.Message}");
            }
        }
    }
}