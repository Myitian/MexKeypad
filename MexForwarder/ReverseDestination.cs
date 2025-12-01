using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace MexForwarder;

public sealed class ReverseDestination : BaseDestination
{
    private readonly byte[] _buffer = new byte[0x10000];
    private readonly ReadOnlyMemory<byte> _token;
    private readonly IPEndPoint _from;
    private EndPoint? _remote = null;
    private long _time = 0;

    public ReverseDestination(
        IPEndPoint ep,
        BlockingCollection<(byte[], int)> queue,
        ReadOnlyMemory<byte> token) : base(ep, queue)
    {
        _token = token;
        _from = ep.AddressFamily is AddressFamily.InterNetwork ?
            Program.Any : Program.IPv6Any;
        _udp.Bind(ep);
    }
    public override void SendPacket(ReadOnlySpan<byte> data)
    {
        if (DateTime.UtcNow.Ticks - _time < TimeSpan.TicksPerMinute && _remote is not null)
            _udp.SendTo(data, _remote);
    }
    public override void SendHeartbeat()
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
                if (ep.Equals(_remote))
                {
                    _time = DateTime.UtcNow.Ticks;
                    if (received == 0)
                        ReceiveHeartbeat();
                }
                else if (_token.Span.SequenceEqual(_buffer.AsSpan(0, received)))
                {
                    _remote = ep;
                    _time = DateTime.UtcNow.Ticks;
                    Console.WriteLine($"[{DateTime.UtcNow:O}] Established connection with {_remote}.");
                }
            }
            catch (Exception ex) when (ex is not ObjectDisposedException)
            {
                Console.WriteLine($"[{DateTime.UtcNow:O}] {ex.GetType().Name} {ex.Message}");
            }
        }
    }
}