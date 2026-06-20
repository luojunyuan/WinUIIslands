using Windows.UI.Xaml;

namespace CoreIsland.TitleBar;

public enum CaptionButton
{
    Minimize = 8,
    Maximize = 9,
    Close = 20,
}

public interface ICaptionButtons
{
    FrameworkElement Element { get; }

    FrameworkElement MinimizeButtonElement { get; }

    FrameworkElement MaximizeButtonElement { get; }

    FrameworkElement CloseButtonElement { get; }

    void HoverButton(CaptionButton button);

    void PressButton(CaptionButton button);

    void ReleaseButton(CaptionButton button);

    void ReleaseButtons();

    void LeaveButtons();

    void IsWindowMaximized(bool value);

    void IsWindowActive(bool value);
}
