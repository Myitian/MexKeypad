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
                    KeyInfo.SendInput(MemoryMarshal.Cast<byte, KeyInfo>(buffer.AsSpan(0, received)));
            }
            catch (Exception ex) when (ex is not ObjectDisposedException)
            {
                Console.WriteLine($"[{DateTime.UtcNow:O}] {ex.GetType().Name} {ex.Message}");
            }
        }
    }
}