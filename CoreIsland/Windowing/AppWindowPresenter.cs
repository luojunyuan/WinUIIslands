namespace CoreIsland.Windowing;

public abstract class AppWindowPresenter
{
    public abstract AppWindowPresenterKind Kind { get; }

    protected AppWindow AppWindow { get; }

    private protected AppWindowPresenter(AppWindow appWindow)
    {
        AppWindow = appWindow;
    }
}
