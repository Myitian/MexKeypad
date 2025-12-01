using MexShared;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MexKeypad;

public partial class KeyClient : IKeyHandler
{
    private readonly SemaphoreSlim _lock = new(1);
    private TcpClient? _tcpClient;
    private UdpClient? _udpClient;

    private async ValueTask HandleKeysInternal(Memory<byte> buffer)
    {
        if (_tcpClient?.GetStream() is Stream tcp)
        {
            await _lock.WaitAsync();
            try
            {
                await tcp.WriteAsync(buffer);
            }
            finally
            {
                _lock.Release();
            }
        }
        else if (_udpClient is UdpClient udp)
        {
            await udp.SendAsync(buffer);
        }
    }
    public ValueTask HandleKeys(bool keyUp, params ReadOnlySpan<KeyInfo> keys)
    {
        if (ReferenceEquals(_tcpClient, _udpClient))
            return ValueTask.CompletedTask;
        int size = keys.Length * Unsafe.SizeOf<KeyInfo>();
        using IMemoryOwner<byte> mem = MemoryPool<byte>.Shared.Rent(size);
        Memory<byte> buffer = mem.Memory;
        Span<KeyInfo> keyBuffer = MemoryMarshal.Cast<byte, KeyInfo>(buffer.Span);
        if (keyUp)
        {
            for (int i = 0; i < keys.Length; i++)
            {
                keyBuffer[i] = keys[i];
                keyBuffer[i].Flag |= KeyboardEventFlag.KeyUp;
            }
        }
        else
        {
            for (int i = 0; i < keys.Length; i++)
            {
                keyBuffer[i] = keys[i];
                keyBuffer[i].Flag &= ~KeyboardEventFlag.KeyUp;
            }
        }
        return HandleKeysInternal(buffer[..size]);
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
            switch (uri.Scheme)
            {
                case "tcp":
                    _tcpClient = new()
                    {
                        NoDelay = true
                    };
                    await _tcpClient.ConnectAsync(ips, port);
                    return null;
                case "udp":
                    _udpClient = new(ips[0].AddressFamily);
                    _udpClient.Connect(ips[0], port);
                    return null;
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
        _tcpClient?.Dispose();
        _tcpClient = null;
        _udpClient?.Dispose();
        _udpClient = null;
    }
    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }
}