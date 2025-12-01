using MexShared;
using System.Collections.Concurrent;
using System.Net;
using System.Text;

namespace MexForwarder;

class Program
{
    public const int HeartbeatInterval = 10000;
    public const int HeartbeatMax = HeartbeatInterval * 4;
    public const int AnyPort = IPEndPoint.MinPort;
    public static readonly IPEndPoint Any = new(IPAddress.Any, AnyPort);
    public static readonly IPEndPoint IPv6Any = new(IPAddress.IPv6Any, AnyPort);
    static int Main(string[] args)
    {
        if (args is not [string srcAddress, string dstAddress, ..])
        {
            Console.WriteLine($"""
                Usage: MexForwarder <source> <destination> [token]
                A simple port forwarder for MexKeypad protocol.
                Not for general purpose. No secure transmission. May have bugs.
                Token is used by forward mode and reverse mode.

                Address format: <direct|forward|reverse|debug>://<hostname>[:<port={NetworkUtils.DefaultPort}>]
                direct: direct udp transmission
                forward: handshake from source side
                reverse: handshake from destination side
                debug: read/print raw hex data (hostname & port will be discarded, but still need to be in the correct format)
                
                Example #1
                [KeyClient IP:1] --(forward)--> [Forwarder IP:2] --(forward)--> [KeyServer IP:3]
                KeyClient: MexForwarder direct://localhost forward://2
                Forwarder: MexForwarder forward://localhost forward://3
                KeyServer: MexForwarder forward://localhost direct://localhost
                
                Example #2
                [KeyClient IP:1] --(forward)--> [Forwarder IP:2] --(reverse)--> [KeyServer IP:3]
                KeyClient: MexForwarder direct://localhost forward://2:1111
                Forwarder: MexForwarder forward://localhost:1111 reverse://localhost:2222
                KeyServer: MexForwarder reverse://2:2222 direct://localhost
                """);
            return 1;
        }
        ReadOnlyMemory<byte> token = args.Length > 2 ? Encoding.UTF8.GetBytes(args[2]) : default;
        if (!NetworkUtils.TryParseEndPoint(srcAddress, out string? srcScheme, out IPEndPoint? srcEP))
        {
            Console.WriteLine("Invalid source address");
            return 1;
        }
        if (!NetworkUtils.TryParseEndPoint(dstAddress, out string? dstScheme, out IPEndPoint? dstEP))
        {
            Console.WriteLine("Invalid destination address");
            return 1;
        }
        BlockingCollection<(byte[], int)> queue = [];
        BaseSource src;
        switch (srcScheme)
        {
            case "direct":
                src = new DirectSource(srcEP, queue);
                break;
            case "forward":
                src = new ForwardSource(srcEP, queue, token);
                break;
            case "reverse":
                src = new ReverseSource(srcEP, queue, token);
                break;
            case "debug":
                src = new DebugSource(srcEP, queue);
                break;
            default:
                Console.WriteLine("Invalid source address");
                return 1;
        }
        src.Init();
        BaseDestination dst;
        switch (dstScheme)
        {
            case "direct":
                dst = new DirectDestination(dstEP, queue);
                break;
            case "forward":
                dst = new ForwardDestination(dstEP, queue, token);
                break;
            case "reverse":
                dst = new ReverseDestination(dstEP, queue, token);
                break;
            case "debug":
                dst = new DebugDestination(dstEP, queue);
                break;
            default:
                Console.WriteLine("Invalid destination address");
                return 1;
        }
        dst.Init();
        new Task(src.StartSend, TaskCreationOptions.LongRunning).Start();
        new Task(dst.StartSend, TaskCreationOptions.LongRunning).Start();
        new Task(dst.StartReceive, TaskCreationOptions.LongRunning).Start();
        src.StartReceive();
        return 0;
    }
}