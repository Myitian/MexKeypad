using MexShared;

namespace MexKeypad;

public interface IKeyHandler : IDisposable
{
    ValueTask HandleKeys(bool keyUp, params ReadOnlySpan<KeyInfo> keys);
    ValueTask<Exception[]?> Start(string uriString);
    void Stop();
}