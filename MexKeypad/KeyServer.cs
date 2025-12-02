using MexShared;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MexKeypad;

public partial class KeyServer : IKeyHandler
{
    private CancellationTokenSource? _cts;
    private TcpListener? _tcpListener;
    private UdpClient? _udpListener;
    public bool IsListening => _tcpListener is not null || _udpListener is not null;

    private static void AcceptTcpClient(
        TcpListener listener,
        CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient client = listener.AcceptTcpClient();
            _ = AcceptTcpClientAsync(client, cancellationToken);
        }
    }
    private static async Task AcceptTcpClientAsync(
        TcpClient client,
        CancellationToken cancellationToken = default)
    {
        using NetworkStream stream = client.GetStream();
        using IMemoryOwner<byte> mem = MemoryPool<byte>.Shared.Rent(1024);
        int keyInfoSize = Unsafe.SizeOf<KeyInfo>();
        int offset = 0;
        Memory<byte> memory = mem.Memory;
        while (!cancellationToken.IsCancellationRequested)
        {
            int required = keyInfoSize - offset;
            int read = await stream.ReadAtLeastAsync(memory[offset..], required, false, cancellationToken);
            if (read < required)
                break;
            Span<byte> span = memory.Span[..(offset + read)];
            HandleKeysStatic(MemoryMarshal.Cast<byte, KeyInfo>(span));
            offset = span.Length % keyInfoSize;
            if (offset > 0)
                span[^offset..].CopyTo(span);
        }
    }
    private static async Task AcceptUdpClientAsync(
        UdpClient listener,
        CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            UdpReceiveResult result = await listener.ReceiveAsync(cancellationToken);
            HandleKeysStatic(MemoryMarshal.Cast<byte, KeyInfo>(result.Buffer));
        }
    }
    public static void HandleKeysStatic(params ReadOnlySpan<KeyInfo> keys)
    {
        if (keys.IsEmpty)
            return;
#if WINDOWS
        Platforms.Windows.Win32Handler.HandleKeys(keys);
#endif
    }
    public ValueTask HandleKeys(bool keyUp, params ReadOnlySpan<KeyInfo> keys)
    {
        if (!keyUp)
            HandleKeysStatic(keys);
        else
        {
            Span<KeyInfo> keyBuffer = stackalloc KeyInfo[keys.Length];
            int j = 0;
            for (int i = keys.Length - 1; i >= 0; i--)
            {
                if (keys[i].Flag.HasFlag(KeyboardEventFlag.Unicode))
                    continue;
                keyBuffer[j] = keys[i];
                keyBuffer[j].Flag |= KeyboardEventFlag.KeyUp;
                j++;
            }
            HandleKeysStatic(keyBuffer[..j]);
        }
        return ValueTask.CompletedTask;
    }
    public async ValueTask<Exception[]?> Start(string uriString)
    {
        try
        {
            Stop();
            if (!Uri.TryCreate(uriString, UriKind.Absolute, out Uri? uri))
                return [new ArgumentException("无效的URI", nameof(uriString))];
            if (await Dns.GetHostAddressesAsync(uri.IdnHost) is not IPAddress[] { Length: > 0 } ips)
                return [new ArgumentException($"无法解析主机名：{uri.IdnHost}", nameof(uriString))];
            int port = uri.Port < 0 ? NetworkUtils.DefaultPort : uri.Port;
            List<Exception>? exceptions = null;
            switch (uri.Scheme)
            {
                case "tcp":
                    foreach (IPAddress ip in ips)
                    {
                        try
                        {
                            _cts = new();
                            IPEndPoint localEP = new(ip, port);
                            _tcpListener = new(localEP);
                            _tcpListener.Start();
                            _ = Task.Factory.StartNew(() => AcceptTcpClient(_tcpListener, _cts.Token), TaskCreationOptions.LongRunning);
                            return null;
                        }
                        catch (Exception ex)
                        {
                            Stop();
                            (exceptions ??= []).Add(new($"{ip.ToStringEx()}:{port} {ex.GetType().Name} {ex.Message}", ex));
                        }
                    }
                    return [new ArgumentException("未找到可用IP！", nameof(uriString)),
                        .. exceptions ?? Enumerable.Empty<Exception>()];
                case "udp":
                    foreach (IPAddress ip in ips)
                    {
                        try
                        {
                            _cts = new();
                            IPEndPoint localEP = new(ip, port);
                            _udpListener = new(localEP);
                            _ = AcceptUdpClientAsync(_udpListener, _cts.Token);
                            return null;
                        }
                        catch (Exception ex)
                        {
                            Stop();
                            (exceptions ??= []).Add(new($"{ip.ToStringEx()}:{port} {ex.GetType().Name} {ex.Message}", ex));
                        }
                    }
                    return [new ArgumentException("未找到可用IP！", nameof(uriString)),
                        .. exceptions ?? Enumerable.Empty<Exception>()];
                default:
                    return [new ArgumentException($"无效的协议：{uri.Scheme}", nameof(uriString))];
            }
        }
        catch (Exception ex)
        {
            Stop();
            return [new($"{ex.GetType().Name} {ex.Message}", ex)];
        }
    }
    public void Stop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _tcpListener?.Dispose();
        _tcpListener = null;
        _udpListener?.Dispose();
        _udpListener = null;
    }
    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }
}