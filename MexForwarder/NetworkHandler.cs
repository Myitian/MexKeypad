using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace MexForwarder;

public abstract class NetworkHandler : IDisposable
{
    protected readonly string _name;
    protected readonly Socket _udp;
    protected readonly BlockingCollection<(byte[], int)> _queue;

    public NetworkHandler(IPEndPoint ep, BlockingCollection<(byte[], int)> queue)
    {
        _name = GetType().Name;
        _udp = new(ep.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
        _queue = queue;
    }

    public virtual void Init()
    {
        Console.WriteLine($"[{DateTime.UtcNow:O}] {_name} {nameof(Init)}");
    }
    public abstract void StartSend();
    public abstract void StartReceive();
    public virtual void SendHeartbeat()
    {
        Console.WriteLine($"[{DateTime.UtcNow:O}] {_name} Send heartbeat");
    }
    public virtual void ReceiveHeartbeat()
    {
        Console.WriteLine($"[{DateTime.UtcNow:O}] {_name} Receive heartbeat");
    }
    public void Dispose()
    {
        _queue.CompleteAdding();
        _udp.Dispose();
        _queue.Dispose();
        GC.SuppressFinalize(this);
    }
}