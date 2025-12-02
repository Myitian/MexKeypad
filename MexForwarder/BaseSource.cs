using System.Collections.Concurrent;
using System.Net;

namespace MexForwarder;

public abstract class BaseSource(IPEndPoint ep, BlockingCollection<(byte[], int)> queue)
    : NetworkHandler(ep, queue)
{
    protected void ReceiveData(byte[] buffer, int received)
    {
        _queue.Add((buffer, received));
    }
}