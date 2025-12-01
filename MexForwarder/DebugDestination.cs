using System.Collections.Concurrent;
using System.Net;

namespace MexForwarder;

public sealed class DebugDestination(IPEndPoint ep, BlockingCollection<(byte[], int)> queue)
    : BaseDestination(ep, queue)
{
    public override void SendHeartbeat()
    {
    }
    public override void StartReceive()
    {
    }
    public override void SendPacket(ReadOnlySpan<byte> data)
    {
        Console.WriteLine(Convert.ToHexString(data));
    }
}