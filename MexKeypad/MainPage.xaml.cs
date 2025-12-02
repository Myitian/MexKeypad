using MexShared;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace MexKeypad;

public partial class MainPage : ContentPage
{
    private static readonly string[] _embedLayouts = [
        "embed:f13-24.xaml",
        "embed:misc.xaml",
        "embed:numpad.xaml",
        "embed:spaces.xaml"];
    private bool _changingKeypadLayout = false;
    private readonly HttpClient _httpClient = new();
    public string Page { get; private set; }
    public IKeyHandler KeyHandler { get; private set; } = new KeyServer();

    public ConfigModel ConfigModel { get; }

    public MainPage()
    {
        BindingContext = ConfigModel = new(new(SetPage));
        InitializeComponent();
        SetKeypadLayout(ConfigModel.KeypadLayout);
        ConfigModel.PropertyChanged += OnConfigChanged;
        ConfigModel.AllPropertyChanged();
        SetPage("tab-keypad");
    }

    public static IEnumerable<IVisualTreeElement> GetChildren(IVisualTreeElement container, bool recursive = false)
    {
        foreach (IVisualTreeElement child in container.GetVisualChildren())
        {
            yield return child;
            if (recursive)
            {
                foreach (IVisualTreeElement grandchild in GetChildren(child, true))
                    yield return grandchild;
            }
        }
    }

    [MemberNotNull(nameof(Page))]
    public void SetPage(string page)
    {
        if (page == Page)
            return;
        foreach (VisualElement element in GetChildren(MainArea)
            .OfType<VisualElement>()
            .Where(it => it.StyleClass.Contains("tab-page")))
            element.IsVisible = element.StyleId == page;
        Page = page;
    }
    private void OnConfigChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!ReferenceEquals(sender, ConfigModel))
            return;
#if WINDOWS
        if (e.PropertyName is nameof(ConfigModel.TopMost) or null)
            Platforms.Windows.Win32Handler.SetWindow(Window, true, ConfigModel.TopMost);
#endif
        if (e.PropertyName is nameof(ConfigModel.KeypadLayout) or null && !_changingKeypadLayout)
            SetKeypadLayout(ConfigModel.KeypadLayout);
    }
    private void LoadKeypadLayoutFromXaml(string xaml)
    {
        try
        {
            TabKeypad.LoadFromXaml(xaml);
            foreach (StyleableElement element in GetChildren(MainArea, true)
                .OfType<StyleableElement>()
                .Where(it => it.StyleClass?.Contains("mexkey") is true))
            {
                string message;
                switch (element)
                {
                    case Button button when button.CommandParameter is string keys:
                        if (KeyInfo.TryParseArray(keys, out message) is KeyInfo[] array)
                        {
                            button.CommandParameter = array;
                            button.Pressed += MexKey_Pressed;
                            button.Released += MexKey_Released;
#if ANDROID
                            (button.Handler?.PlatformView as Android.Widget.TextView)
                                ?.SetMaxLines(int.MaxValue);
#endif
                            break;
                        }
                        throw new Exception(message);
                    case ImageButton button when button.CommandParameter is string keys:
                        if (KeyInfo.TryParseArray(keys, out message) is KeyInfo[] array2)
                        {
                            button.CommandParameter = array2;
                            button.Pressed += MexKey_Pressed;
                            button.Released += MexKey_Released;
                            break;
                        }
                        throw new Exception(message);
                }
            }
        }
        catch (Exception ex)
        {
            TabKeypad.Content = new Label()
            {
                Padding = new(10),
                Text = $"加载键盘布局失败！\n{ex.GetType().FullName}\n{ex.Message}"
            };
        }
    }
    public async void SetKeypadLayout(string? file)
    {
        _changingKeypadLayout = true;
        file = file?.Trim();
        if (string.IsNullOrEmpty(file))
        {
            await DisplayAlertAsync("错误", "请填入有效的资源路径", "确认");
            return;
        }
        string? xaml = null;
        string? error = null;
        try
        {
            do
            {
                if (file.StartsWith("embed:", StringComparison.OrdinalIgnoreCase))
                {
                    string resName = file.AsSpan(6).Trim([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]).ToString();
                    if (await FileSystem.AppPackageFileExistsAsync(resName))
                    {
                        using Stream stream = await FileSystem.OpenAppPackageFileAsync(resName);
                        using StreamReader reader = new(stream);
                        xaml = await reader.ReadToEndAsync();
                    }
                    else
                    {
                        error = "无效的嵌入资源";
                        break;
                    }
                }
                else if (file.StartsWith("http:", StringComparison.OrdinalIgnoreCase)
                    || file.StartsWith("https:", StringComparison.OrdinalIgnoreCase))
                {
                    if (!Uri.TryCreate(file, UriKind.Absolute, out Uri? uri))
                    {
                        error = "无效的HTTP(S)链接";
                        break;
                    }
                    file = uri.ToString();
                    xaml = await _httpClient.GetStringAsync(uri);
                }
                else
                {
                    if (file.StartsWith("file:", StringComparison.OrdinalIgnoreCase)
                        && Uri.TryCreate($"file:///{file.AsSpan(5).Trim([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar])}", UriKind.Absolute, out Uri? uri))
                        file = uri.LocalPath;
                    file = Path.GetFullPath(file);
                    if (File.Exists(file))
                        xaml = await File.ReadAllTextAsync(file);
                    else
                    {
                        error = "文件不存在";
                        break;
                    }
                }
            }
            while (false);
        }
        catch (Exception ex)
        {
            xaml = null;
            error = $"获取XAML文件失败。异常：{ex.GetType().Name}: {ex.Message}";
        }
        ConfigModel.KeypadLayout = file;

        if (error is not null)
        {
            LayoutInfoLabel.Text = error;
            TabKeypad.Content = new Label()
            {
                Padding = new(10),
                Text = $"加载键盘布局失败！\n{error}"
            };
        }
        else
        {
            LayoutInfoLabel.Text = "";
            if (xaml is not null)
                LoadKeypadLayoutFromXaml(xaml);
        }
        _changingKeypadLayout = false;
    }
    private async ValueTask HandleKeys(bool keyUp, object? sender)
    {
        KeyInfo[] keys;
        switch (sender)
        {
            case Button button when button.CommandParameter is KeyInfo[] array:
                keys = array;
                break;
            case ImageButton button when button.CommandParameter is KeyInfo[] array:
                keys = array;
                break;
            default:
                return;
        }
        try
        {
            await KeyHandler.HandleKeys(keyUp, keys);
        }
        catch { }
    }
    public async ValueTask RestartKeyHandler()
    {
        KeyHandler.Stop();
        Exception[]? exceptions = null;
        switch (ConfigModel.KeyHandlerMode)
        {
            case KeyHandlerMode.Local:
                if (KeyHandler is not KeyServer)
                {
                    KeyHandler.Dispose();
                    KeyHandler = new KeyServer();
                }
                KeyHandlerInfoLabel.Text = "当前状态：本地软键盘";
                return;
            case KeyHandlerMode.Client:
                if (KeyHandler is not KeyClient)
                {
                    KeyHandler.Dispose();
                    KeyHandler = new KeyClient();
                }
                exceptions = await KeyHandler.Start(ConfigModel.RemoteAddress);
                KeyHandlerInfoLabel.Text = $"当前状态：客户端模式（活动中）{ConfigModel.RemoteAddress}";
                break;
            case KeyHandlerMode.Server:
                if (KeyHandler is not KeyServer)
                {
                    KeyHandler.Dispose();
                    KeyHandler = new KeyServer();
                }
                exceptions = await KeyHandler.Start(ConfigModel.LocalAddress);
                KeyHandlerInfoLabel.Text = $"当前状态：服务端模式（活动中）{ConfigModel.LocalAddress}";
                break;
        }
        if (exceptions is not null)
        {
            StopKeyHandler();
            await DisplayAlertAsync("错误",
                string.Join(Environment.NewLine, exceptions.Select(it => it.Message)),
                "确认");
        }
    }
    public void StopKeyHandler()
    {
        KeyHandler.Stop();
        switch (ConfigModel.KeyHandlerMode)
        {
            case KeyHandlerMode.Client:
                KeyHandlerInfoLabel.Text = "当前状态：客户端模式（已停止）";
                break;
            case KeyHandlerMode.Server:
                KeyHandlerInfoLabel.Text = "当前状态：服务端模式（已停止）";
                break;
        }
    }

    private async void ContentPage_Loaded(object? sender, EventArgs e)
    {
#if WINDOWS
        Platforms.Windows.Win32Handler.SetWindow(Window, true, ConfigModel.TopMost);
#endif
        await RestartKeyHandler();
    }
    private async void MexKey_Pressed(object? sender, EventArgs e)
    {
        await HandleKeys(false, sender);
    }
    private async void MexKey_Released(object? sender, EventArgs e)
    {
        await HandleKeys(true, sender);
    }
    private void AdaptiveGrid_SizeChanged(object? sender, EventArgs e)
    {
        string visualStateName = AdaptiveGrid.Width > AdaptiveGrid.Height
            ? "LandscapeState"
            : "PortraitState";
        VisualStateManager.GoToState(AdaptiveGrid, visualStateName);
        VisualStateManager.GoToState(MainArea, visualStateName);
        VisualStateManager.GoToState(TabKeypad, visualStateName);
        if (TabKeypad.Content is View content)
            VisualStateManager.GoToState(content, visualStateName);
    }
    private void KeypadLayout_Entry_Unfocused(object? sender, FocusEventArgs e)
    {
        SetKeypadLayout((sender as InputView)?.Text);
    }
    private void KeypadLayout_Reset_Clicked(object? sender, EventArgs e)
    {
        SetKeypadLayout(ConfigModel.KeypadLayoutDefaultValue);
    }
    private async void KeypadLayout_Select_Clicked(object? sender, EventArgs e)
    {
        string selectedAction = await DisplayActionSheetAsync("请选择一个预设", "取消", null, _embedLayouts);
        if (selectedAction.AsSpan().StartsWith("embed:"))
            SetKeypadLayout(selectedAction);
    }
    private async void KeypadLayout_Browse_Clicked(object? sender, EventArgs e)
    {
        if (await FilePicker.PickAsync() is not FileResult file)
            return;
        SetKeypadLayout(file.FullPath);
    }

    private void KeyHandlerMode_Reset_Clicked(object? sender, EventArgs e)
    {
        ConfigModel.KeyHandlerMode = ConfigModel.KeyHandlerModeDefaultValue;
        ConfigModel.RemoteAddress = ConfigModel.RemoteAddressDefaultValue;
        ConfigModel.LocalAddress = ConfigModel.LocalAddressDefaultValue;
    }
    private async void KeyHandlerMode_Start_Clicked(object? sender, EventArgs e)
    {
        if (sender is VisualElement element)
        {
            element.IsEnabled = false;
            await RestartKeyHandler();
            element.IsEnabled = true;
        }
    }
    private void KeyHandlerMode_Stop_Clicked(object? sender, EventArgs e)
    {
        StopKeyHandler();
    }

    private async void ResetAll_Clicked(object? sender, EventArgs e)
    {
        if (!await DisplayAlertAsync("警告", "是否要重置所有配置？", "确认", "取消"))
            return;
        ConfigModel.ClearConfig();
    }
}
