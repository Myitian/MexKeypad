using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace MexForwarder;

public sealed class DirectSource : BaseSource
{
    private readonly IPEndPoint _from;

    public DirectSource(IPEndPoint ep, BlockingCollection<(byte[], int)> queue) : base(ep, queue)
    {
        _from = ep.AddressFamily is AddressFamily.InterNetwork ?
            Program.Any : Program.IPv6Any;
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
                    _queue.Add((buffer, received));
            }
            catch (Exception ex) when (ex is not ObjectDisposedException)
            {
                Console.WriteLine($"[{DateTime.UtcNow:O}] {ex.GetType().Name} {ex.Message}");
            }
        }
    }
}