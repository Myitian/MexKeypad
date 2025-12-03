using MexShared;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace MexServer;

public abstract class NetworkHandler : IDisposable
{
    protected readonly byte[] _buffer = GC.AllocateUninitializedArray<byte>(0x10000);
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
    protected void ReceiveData(int received)
    {
        KeyInfo.SendInput(MemoryMarshal.Cast<byte, KeyInfo>(_buffer.AsSpan(0, received)));
    }
    public void Dispose()
    {
        _udp.Dispose();
        GC.SuppressFinalize(this);
    }
}