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
                byte[] buffer = ArrayPool<byte>.Shared.Rent(0x10000);
                int received = _udp.Receive(buffer);
                if (received == 0)
                {
                    ReceiveHeartbeat();
                    ArrayPool<byte>.Shared.Return(buffer);
                }
                else
                    KeyInfo.SendInput(MemoryMarshal.Cast<byte, KeyInfo>(buffer.AsSpan(0, received)));
            }
            catch (Exception ex) when (ex is not ObjectDisposedException)
            {
                Console.WriteLine($"[{DateTime.UtcNow:O}] {ex.GetType().Name} {ex.Message}");
                Init();
            }
        }
    }
}