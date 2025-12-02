namespace MexKeypad;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        MainPage mainPage = new();
        TitleBar titleBar = new() { Title = mainPage.Title };
        titleBar.SetAppThemeColor(VisualElement.BackgroundColorProperty, Colors.White, Colors.Black);
        return new(mainPage)
        {
            Title = mainPage.Title,
            TitleBar = titleBar
        };
    }
}