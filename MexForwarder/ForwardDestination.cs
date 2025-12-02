using MexShared;
using System.Collections.Concurrent;
using System.Net;

namespace MexForwarder;

public sealed class ForwardDestination : BaseDestination
{
    private readonly ReadOnlyMemory<byte> _token;

    public ForwardDestination(
        IPEndPoint ep,
        BlockingCollection<(byte[], int)> queue,
        ReadOnlyMemory<byte> token) : base(ep, queue)
    {
        _token = token;
        _udp.Connect(ep);
        _udp.ReceiveTimeout = NetworkUtils.HeartbeatMax;
    }

    public override void Init()
    {
        Span<byte> buffer = stackalloc byte[_token.Length + 1];
        buffer[0] = byte.MaxValue;
        _token.Span.CopyTo(buffer[1..]);
        _udp.Send(buffer);
        base.Init();
    }
    public override void SendHeartbeat()
    {
        _udp.Send(ReadOnlySpan<byte>.Empty);
        base.SendHeartbeat();
    }
    public override void StartReceive()
    {
    }
}