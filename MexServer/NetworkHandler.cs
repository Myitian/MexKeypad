using System.Net;
using System.Net.Sockets;

namespace MexServer;

public abstract class NetworkHandler : IDisposable
{
    protected readonly string _name;
    protected readonly Socket _udp;

    public NetworkHandler(IPEndPoint ep)
    {
        _name = GetType().Name;
        _udp = new(ep.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
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
        _udp.Dispose();
        GC.SuppressFinalize(this);
    }
}