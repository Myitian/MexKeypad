using MexShared;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace MexServer;

public sealed class ForwardSource : NetworkHandler
{
    private readonly ReadOnlyMemory<byte> _token;
    private readonly IPEndPoint _from;
    private EndPoint? _remote = null;
    private long _time = 0;

    public ForwardSource(
        IPEndPoint ep,
        ReadOnlyMemory<byte> token) : base(ep)
    {
        _token = token;
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
                if (ep.Equals(_remote))
                {
                    _time = DateTime.UtcNow.Ticks;
                    if (received == 0)
                        ReceiveHeartbeat();
                    else if (DateTime.UtcNow.Ticks - _time < NetworkUtils.HeartbeatMax * TimeSpan.TicksPerMillisecond)
                        ReceiveData(received);
                }
                else if (received > 0 && _buffer[0] is byte.MaxValue)
                {
                    if (_buffer.AsSpan(1, received - 1).SequenceEqual(_token.Span))
                    {
                        _remote = ep;
                        _time = DateTime.UtcNow.Ticks;
                        Console.WriteLine($"[{DateTime.UtcNow:O}] {_name} Established connection with {_remote}.");
                    }
                }
            }
            catch (Exception ex) when (ex is not ObjectDisposedException)
            {
                Console.WriteLine($"[{DateTime.UtcNow:O}] {ex.GetType().Name} {ex.Message}");
            }
        }
    }
}