using System.Collections.Concurrent;
using System.Net;

namespace MexForwarder;

public sealed class DirectDestination : BaseDestination
{
    public DirectDestination(
        IPEndPoint ep,
        BlockingCollection<(byte[], int)> queue) : base(ep, queue)
    {
        _udp.Connect(ep);
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