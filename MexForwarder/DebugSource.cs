using System.Buffers;
using System.Collections.Concurrent;
using System.Net;

namespace MexForwarder;

public sealed class DebugSource(IPEndPoint ep, BlockingCollection<(byte[], int)> queue)
    : BaseSource(ep, queue)
{
    public override void StartSend()
    {
    }
    public override void StartReceive()
    {
        while (true)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(0x10000);
            try
            {
                Convert.FromHexString(Console.ReadLine().AsSpan().Trim(), buffer, out _, out int received);
                ReceiveData(buffer, received);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.UtcNow:O}] {ex.GetType().Name} {ex.Message}");
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}