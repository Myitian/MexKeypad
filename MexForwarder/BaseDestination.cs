using MexShared;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net;

namespace MexForwarder;

public abstract class BaseDestination(IPEndPoint ep, BlockingCollection<(byte[], int)> queue)
    : NetworkHandler(ep, queue)
{
    public virtual void SendPacket(ReadOnlySpan<byte> data)
    {
        _udp.Send(data);
    }
    public override void StartSend()
    {
        while (!_queue.IsCompleted)
        {
            if (_queue.TryTake(out (byte[] data, int received) o, NetworkUtils.HeartbeatInterval))
            {
                SendPacket(o.data.AsSpan(..o.received));
                ArrayPool<byte>.Shared.Return(o.data);
            }
            else
                SendHeartbeat();
        }
    }
}