using MexShared;
using System.Net;
using System.Text;

namespace MexServer;

class Program
{
    static int Main(string[] args)
    {
        if (args is not [string srcAddress, ..])
        {
            Console.WriteLine($"""
                Usage: MexServer <source> [token]
                A simple server for MexKeypad protocol. No need for starting the main MAUI app.
                Also supports MexForwarder's forward/reverse protocol.
                Token is used by forward mode and reverse mode.

                Use Ctrl+C to break.

                Address format: <direct|forward|reverse>://<hostname>[:<port={NetworkUtils.DefaultPort}>]
                direct: direct udp transmission
                forward: handshake from source side
                reverse: handshake from destination side
                """);
            return 1;
        }
        ReadOnlyMemory<byte> token = args.Length > 1 ? Encoding.UTF8.GetBytes(args[1]) : default;
        if (!NetworkUtils.TryParseEndPoint(srcAddress, out string? srcScheme, out IPEndPoint? srcEP))
        {
            Console.WriteLine("Invalid source address");
            return 1;
        }
        NetworkHandler src;
        switch (srcScheme)
        {
            case "direct":
                src = new DirectSource(srcEP);
                break;
            case "forward":
                src = new ForwardSource(srcEP, token);
                break;
            case "reverse":
                src = new ReverseSource(srcEP, token);
                break;
            default:
                Console.WriteLine("Invalid source address");
                return 1;
        }
        src.Init();
        new Task(src.StartSend, TaskCreationOptions.LongRunning).Start();
        src.StartReceive();
        return 0;
    }
}