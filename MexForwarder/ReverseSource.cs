using System.Buffers;
using System.Collections.Concurrent;
using System.Net;

namespace MexForwarder;

public sealed class ReverseSource : BaseSource
{
    private readonly ReadOnlyMemory<byte> _token;

    public ReverseSource(
        IPEndPoint ep,
        BlockingCollection<(byte[], int)> queue,
        ReadOnlyMemory<byte> token) : base(ep, queue)
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
            Thread.Sleep(Program.HeartbeatInterval);
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
                    _queue.Add((buffer, received));
            }
            catch (Exception ex) when (ex is not ObjectDisposedException)
            {
                Console.WriteLine($"[{DateTime.UtcNow:O}] {ex.GetType().Name} {ex.Message}");
                Init();
            }
        }
    }
}