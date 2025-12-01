using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MexKeypad;

public partial class ConfigModel(Command<string> switchPage, IPreferences? preferences = null) : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    private readonly IPreferences _preferences = preferences ?? Preferences.Default;
    private static readonly bool _isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#pragma warning disable CA1822
    public bool IsWindows => _isWindows;
#pragma warning restore CA1822
    public Command<string> SwitchPage { get; } = switchPage;

    #region KeypadLayout
    public static readonly string KeypadLayoutDefaultValue = "embed:numpad.xaml";
    public string KeypadLayout
    {
        get => GetConfig(KeypadLayoutDefaultValue);
        set
        {
            SetConfig(value);
            OnPropertyChanged();
        }
    }
    #endregion KeypadLayout

    #region KeyHandlerMode
    public static readonly KeyHandlerMode KeyHandlerModeDefaultValue = _isWindows ?
        KeyHandlerMode.Local : KeyHandlerMode.Client;
    public KeyHandlerMode KeyHandlerMode
    {
        get => (KeyHandlerMode)GetConfig((int)KeyHandlerModeDefaultValue);
        set
        {
            SetConfig((int)value);
            OnPropertyChanged();
        }
    }
    #endregion KeyHandlerMode

    #region RemoteAddress
    public static readonly string RemoteAddressDefaultValue = "";
    public string RemoteAddress
    {
        get => GetConfig(RemoteAddressDefaultValue);
        set
        {
            SetConfig(value);
            OnPropertyChanged();
        }
    }
    #endregion RemoteAddress

    #region LocalAddress
    public static readonly string LocalAddressDefaultValue = "tcp://localhost:6957";
    public string LocalAddress
    {
        get => GetConfig(LocalAddressDefaultValue);
        set
        {
            SetConfig(value);
            OnPropertyChanged();
        }
    }
    #endregion LocalAddress

    #region TopMost
    public static readonly bool TopMostDefaultValue = false;
    public bool TopMost
    {
        get => GetConfig(TopMostDefaultValue);
        set
        {
            SetConfig(value);
            OnPropertyChanged();
        }
    }
    #endregion TopMost

    public void AllPropertyChanged()
    {
        OnPropertyChanged(null);
    }
    public void ClearConfig()
    {
        _preferences.Clear();
        AllPropertyChanged();
    }
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new(propertyName));
    }
    private void SetConfig<T>(T? value, [CallerMemberName] string? propertyName = null)
    {
        _preferences.Set(propertyName ?? "", value);
    }
    [return: NotNullIfNotNull(nameof(defaultValue))]
    private T? GetConfig<T>(T? defaultValue, [CallerMemberName] string? propertyName = null)
    {
        return _preferences.Get(propertyName ?? "", defaultValue);
    }
}