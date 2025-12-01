using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace MexForwarder;

public sealed class ForwardSource : BaseSource
{
    private readonly ReadOnlyMemory<byte> _token;
    private readonly IPEndPoint _from;
    private EndPoint? _remote = null;
    private long _time = 0;

    public ForwardSource(
        IPEndPoint ep,
        BlockingCollection<(byte[], int)> queue,
        ReadOnlyMemory<byte> token) : base(ep, queue)
    {
        _token = token;
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
                if (ep.Equals(_remote))
                    _time = DateTime.UtcNow.Ticks;
                if (received == 0)
                {
                    ReceiveHeartbeat();
                    ArrayPool<byte>.Shared.Return(buffer);
                }
                else if (buffer[0] is byte.MaxValue)
                {
                    if (buffer.AsSpan(1, received - 1).SequenceEqual(_token.Span))
                    {
                        _remote = ep;
                        _time = DateTime.UtcNow.Ticks;
                        Console.WriteLine($"[{DateTime.UtcNow:O}] Established connection with {_remote}.");
                    }
                    ArrayPool<byte>.Shared.Return(buffer);
                }
                else if (ep.Equals(_remote) && DateTime.UtcNow.Ticks - _time < TimeSpan.TicksPerMinute)
                    _queue.Add((buffer, received));
            }
            catch (Exception ex) when (ex is not ObjectDisposedException)
            {
                Console.WriteLine($"[{DateTime.UtcNow:O}] {ex.GetType().Name} {ex.Message}");
            }
        }
    }
}