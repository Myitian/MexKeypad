using MexShared;
using System.Buffers;
using System.Net;
using System.Runtime.InteropServices;

namespace MexServer;

public sealed class ReverseSource : NetworkHandler
{
    private readonly ReadOnlyMemory<byte> _token;

    public ReverseSource(
        IPEndPoint ep,
        ReadOnlyMemory<byte> token) : base(ep)
    {
        _token = token;
        _udp.Connect(ep);
    }
    public override void Init()
    {
        _udp.Send(_token.Span);
        base.Init();
    }
    public override void SendHeartbeat()
    {
        _udp.Send(ReadOnlySpan<byte>.Empty);
        base.SendHeartbeat();
    }
    public override void StartSend()
    {
        while (true)
        {
            SendHeartbeat();
            Thread.Sleep(NetworkUtils.HeartbeatInterval);
        }
    }
    public override void StartReceive()
    {
        while (true)
        {
            try
            {
                int received = _udp.Receive(_buffer);
                if (received == 0)
                    ReceiveHeartbeat();
                else
                    ReceiveData(received);
            }
            catch (Exception ex) when (ex is not ObjectDisposedException)
            {
                Console.WriteLine($"[{DateTime.UtcNow:O}] {ex.GetType().Name} {ex.Message}");
                Init();
            }
        }
    }
}