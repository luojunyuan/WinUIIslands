using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using IslandWindow = CoreIsland.Window;

namespace CoreIsland.TitleBar;

internal sealed partial class CaptionButtonsControl : UserControl, ICaptionButtons
{
    private static readonly SolidColorBrush TransparentBrush = new(Colors.Transparent);

    private const string CaptionButtonResourcesXaml = """
<ResourceDictionary
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <ResourceDictionary.ThemeDictionaries>
        <ResourceDictionary x:Key="Light">
            <Color x:Key="CaptionButtonForegroundColor">Black</Color>
            <SolidColorBrush x:Key="CaptionButtonForeground" Color="{StaticResource CaptionButtonForegroundColor}" />
            <SolidColorBrush x:Key="CaptionButtonForegroundPointerOver" Color="{StaticResource CaptionButtonForegroundColor}" />
            <SolidColorBrush x:Key="CaptionButtonForegroundPressed" Opacity="0.7" Color="{StaticResource CaptionButtonForegroundColor}" />
            <SolidColorBrush x:Key="CaptionButtonForegroundNotActive" Opacity="0.38" Color="{StaticResource CaptionButtonForegroundColor}" />
            <SolidColorBrush x:Key="CaptionButtonBackgroundPointerOver" Opacity="0.06" Color="{StaticResource CaptionButtonForegroundColor}" />
            <SolidColorBrush x:Key="CaptionButtonBackgroundPressed" Opacity="0.04" Color="{StaticResource CaptionButtonForegroundColor}" />
        </ResourceDictionary>
        <ResourceDictionary x:Key="Dark">
            <Color x:Key="CaptionButtonForegroundColor">White</Color>
            <SolidColorBrush x:Key="CaptionButtonForeground" Color="{StaticResource CaptionButtonForegroundColor}" />
            <SolidColorBrush x:Key="CaptionButtonForegroundPointerOver" Color="{StaticResource CaptionButtonForegroundColor}" />
            <SolidColorBrush x:Key="CaptionButtonForegroundPressed" Opacity="0.7" Color="{StaticResource CaptionButtonForegroundColor}" />
            <SolidColorBrush x:Key="CaptionButtonForegroundNotActive" Opacity="0.35" Color="{StaticResource CaptionButtonForegroundColor}" />
            <SolidColorBrush x:Key="CaptionButtonBackgroundPointerOver" Opacity="0.06" Color="{StaticResource CaptionButtonForegroundColor}" />
            <SolidColorBrush x:Key="CaptionButtonBackgroundPressed" Opacity="0.04" Color="{StaticResource CaptionButtonForegroundColor}" />
        </ResourceDictionary>
    </ResourceDictionary.ThemeDictionaries>

    <Color x:Key="CaptionButtonBackground">Transparent</Color>
    <x:Double x:Key="CaptionButtonWidth">46</x:Double>
    <x:Double x:Key="CaptionButtonHeight">32</x:Double>
    <x:String x:Key="CaptionButtonGlyph">&#xE8BB;</x:String>

    <Style x:Key="CaptionButtonStyle" TargetType="Button">
        <Setter Property="BorderThickness" Value="0" />
        <Setter Property="Background" Value="{StaticResource CaptionButtonBackground}" />
        <Setter Property="IsTabStop" Value="False" />
        <Setter Property="Width" Value="{StaticResource CaptionButtonWidth}" />
        <Setter Property="MinWidth" Value="{StaticResource CaptionButtonWidth}" />
        <Setter Property="Height" Value="{StaticResource CaptionButtonHeight}" />
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="Button">
                    <Border
                        x:Name="ButtonBaseElement"
                        Padding="{TemplateBinding Padding}"
                        Background="{TemplateBinding Background}"
                        BorderBrush="{TemplateBinding BorderBrush}"
                        BorderThickness="{TemplateBinding BorderThickness}">
                        <Viewbox Width="10" Height="10">
                            <FontIcon
                                x:Name="ButtonIcon"
                                FontFamily="{ThemeResource SymbolThemeFontFamily}"
                                Foreground="{ThemeResource CaptionButtonForeground}"
                                Glyph="{ThemeResource CaptionButtonGlyph}" />
                        </Viewbox>
                        <VisualStateManager.VisualStateGroups>
                            <VisualStateGroup x:Name="CommonStates">
                                <VisualState x:Name="Normal">
                                    <VisualState.Setters>
                                        <Setter Target="ButtonBaseElement.Background" Value="{StaticResource CaptionButtonBackground}" />
                                        <Setter Target="ButtonIcon.Foreground" Value="{ThemeResource CaptionButtonForeground}" />
                                    </VisualState.Setters>
                                </VisualState>
                                <VisualState x:Name="PointerOver">
                                    <VisualState.Setters>
                                        <Setter Target="ButtonBaseElement.Background" Value="{ThemeResource CaptionButtonBackgroundPointerOver}" />
                                        <Setter Target="ButtonIcon.Foreground" Value="{ThemeResource CaptionButtonForegroundPointerOver}" />
                                    </VisualState.Setters>
                                </VisualState>
                                <VisualState x:Name="Pressed">
                                    <VisualState.Setters>
                                        <Setter Target="ButtonBaseElement.Background" Value="{ThemeResource CaptionButtonBackgroundPressed}" />
                                        <Setter Target="ButtonIcon.Foreground" Value="{ThemeResource CaptionButtonForegroundPressed}" />
                                    </VisualState.Setters>
                                </VisualState>
                                <VisualState x:Name="NotActive">
                                    <VisualState.Setters>
                                        <Setter Target="ButtonIcon.Foreground" Value="{ThemeResource CaptionButtonForegroundNotActive}" />
                                    </VisualState.Setters>
                                </VisualState>
                            </VisualStateGroup>
                            <VisualStateGroup x:Name="MinMaxStates">
                                <VisualState x:Name="WindowStateNormal" />
                                <VisualState x:Name="WindowStateMaximized">
                                    <VisualState.Setters>
                                        <Setter Target="ButtonIcon.Glyph" Value="&#xE923;" />
                                    </VisualState.Setters>
                                </VisualState>
                            </VisualStateGroup>
                        </VisualStateManager.VisualStateGroups>
                    </Border>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>

    <SolidColorBrush x:Key="CloseCaptionButtonBackgroundPointerOver" Color="#C42B1C" />
    <SolidColorBrush x:Key="CloseCaptionButtonBackgroundPressed" Opacity="0.9" Color="#C42B1C" />
    <SolidColorBrush x:Key="CloseCaptionButtonForegroundPointerOver" Color="White" />
    <SolidColorBrush x:Key="CloseCaptionButtonForegroundPressed" Opacity="0.7" Color="White" />

    <Style x:Key="CloseCaptionButtonStyle" TargetType="Button">
        <Setter Property="BorderThickness" Value="0" />
        <Setter Property="Background" Value="{StaticResource CaptionButtonBackground}" />
        <Setter Property="IsTabStop" Value="False" />
        <Setter Property="Width" Value="{StaticResource CaptionButtonWidth}" />
        <Setter Property="MinWidth" Value="{StaticResource CaptionButtonWidth}" />
        <Setter Property="Height" Value="{StaticResource CaptionButtonHeight}" />
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="Button">
                    <Border
                        x:Name="ButtonBaseElement"
                        Padding="{TemplateBinding Padding}"
                        Background="{TemplateBinding Background}"
                        BorderBrush="{TemplateBinding BorderBrush}"
                        BorderThickness="{TemplateBinding BorderThickness}">
                        <Viewbox Width="10" Height="10">
                            <FontIcon
                                x:Name="ButtonIcon"
                                FontFamily="{ThemeResource SymbolThemeFontFamily}"
                                Foreground="{ThemeResource CaptionButtonForeground}"
                                Glyph="{ThemeResource CaptionButtonGlyph}" />
                        </Viewbox>
                        <VisualStateManager.VisualStateGroups>
                            <VisualStateGroup x:Name="CommonStates">
                                <VisualState x:Name="Normal">
                                    <VisualState.Setters>
                                        <Setter Target="ButtonBaseElement.Background" Value="{StaticResource CaptionButtonBackground}" />
                                        <Setter Target="ButtonIcon.Foreground" Value="{ThemeResource CaptionButtonForeground}" />
                                    </VisualState.Setters>
                                </VisualState>
                                <VisualState x:Name="PointerOver">
                                    <VisualState.Setters>
                                        <Setter Target="ButtonBaseElement.Background" Value="{StaticResource CloseCaptionButtonBackgroundPointerOver}" />
                                        <Setter Target="ButtonIcon.Foreground" Value="{StaticResource CloseCaptionButtonForegroundPointerOver}" />
                                    </VisualState.Setters>
                                </VisualState>
                                <VisualState x:Name="Pressed">
                                    <VisualState.Setters>
                                        <Setter Target="ButtonBaseElement.Background" Value="{StaticResource CloseCaptionButtonBackgroundPressed}" />
                                        <Setter Target="ButtonIcon.Foreground" Value="{StaticResource CloseCaptionButtonForegroundPressed}" />
                                    </VisualState.Setters>
                                </VisualState>
                                <VisualState x:Name="NotActive">
                                    <VisualState.Setters>
                                        <Setter Target="ButtonIcon.Foreground" Value="{ThemeResource CaptionButtonForegroundNotActive}" />
                                    </VisualState.Setters>
                                </VisualState>
                            </VisualStateGroup>
                            <VisualStateGroup x:Name="MinMaxStates">
                                <VisualState x:Name="WindowStateNormal" />
                                <VisualState x:Name="WindowStateMaximized">
                                    <VisualState.Setters>
                                        <Setter Target="ButtonIcon.Glyph" Value="&#xE923;" />
                                    </VisualState.Setters>
                                </VisualState>
                            </VisualStateGroup>
                        </VisualStateManager.VisualStateGroups>
                    </Border>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>
</ResourceDictionary>
""";

    private readonly Button _minimizeButton;
    private readonly Button _maximizeButton;
    private readonly Button _closeButton;
    private readonly ResourceDictionary _captionButtonResources;

    private CaptionButton? _pressedButton;
    private bool _allInNormal = true;
    private bool _isWindowMaximized;
    private bool _isWindowActive = true;

    public CaptionButtonsControl()
    {
        HorizontalAlignment = HorizontalAlignment.Right;
        VerticalAlignment = VerticalAlignment.Top;
        Background = TransparentBrush;
        Visibility = Visibility.Collapsed;

        _captionButtonResources = (ResourceDictionary)XamlReader.Load(CaptionButtonResourcesXaml);
        Resources.MergedDictionaries.Add(_captionButtonResources);

        _minimizeButton = CreateButton("Minimize", "CaptionButtonStyle", "\uE921");
        _maximizeButton = CreateButton("Maximize", "CaptionButtonStyle", "\uE922");
        _closeButton = CreateButton("Close", "CloseCaptionButtonStyle", "\uE8BB");

        _minimizeButton.Click += MinimizeButton_Click;
        _maximizeButton.Click += MaximizeButton_Click;
        _closeButton.Click += CloseButton_Click;

        Content = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Children =
            {
                _minimizeButton,
                _maximizeButton,
                _closeButton,
            },
        };

        ApplyNormalState();
    }

    public IslandWindow? Window { get; set; }

    public FrameworkElement Element => this;

    public FrameworkElement MinimizeButtonElement => _minimizeButton;

    public FrameworkElement MaximizeButtonElement => _maximizeButton;

    public FrameworkElement CloseButtonElement => _closeButton;

    public void HoverButton(CaptionButton button)
    {
        if (_pressedButton is CaptionButton pressedButton)
        {
            bool hoveringOnPressedButton = pressedButton == button;
            _allInNormal = !hoveringOnPressedButton;
            GoToButtonState(_minimizeButton, hoveringOnPressedButton && button == CaptionButton.Minimize ? "Pressed" : "Normal");
            GoToButtonState(_maximizeButton, hoveringOnPressedButton && button == CaptionButton.Maximize ? "Pressed" : "Normal");
            GoToButtonState(_closeButton, hoveringOnPressedButton && button == CaptionButton.Close ? "Pressed" : "Normal");
            return;
        }

        _allInNormal = false;
        var activeState = _isWindowActive ? "Normal" : "NotActive";
        GoToButtonState(_minimizeButton, button == CaptionButton.Minimize ? "PointerOver" : activeState);
        GoToButtonState(_maximizeButton, button == CaptionButton.Maximize ? "PointerOver" : activeState);
        GoToButtonState(_closeButton, button == CaptionButton.Close ? "PointerOver" : activeState);
    }

    public void PressButton(CaptionButton button)
    {
        _allInNormal = false;
        _pressedButton = button;

        GoToButtonState(_minimizeButton, button == CaptionButton.Minimize ? "Pressed" : "Normal");
        GoToButtonState(_maximizeButton, button == CaptionButton.Maximize ? "Pressed" : "Normal");
        GoToButtonState(_closeButton, button == CaptionButton.Close ? "Pressed" : "Normal");
    }

    public void ReleaseButton(CaptionButton button)
    {
        bool clicked = _pressedButton == button;

        if (clicked)
            Window?.PostCaptionButtonCommand(button);

        _pressedButton = null;
        _allInNormal = clicked;

        GoToButtonState(_minimizeButton, !clicked && button == CaptionButton.Minimize ? "PointerOver" : "Normal");
        GoToButtonState(_maximizeButton, !clicked && button == CaptionButton.Maximize ? "PointerOver" : "Normal");
        GoToButtonState(_closeButton, !clicked && button == CaptionButton.Close ? "PointerOver" : "Normal");
    }

    public void ReleaseButtons()
    {
        if (_pressedButton is null)
            return;

        _pressedButton = null;
        LeaveButtons();
    }

    public void LeaveButtons()
    {
        if (_allInNormal)
            return;

        _allInNormal = true;
        var activeState = _isWindowActive ? "Normal" : "NotActive";
        GoToButtonState(_minimizeButton, activeState);
        GoToButtonState(_maximizeButton, activeState);
        GoToButtonState(_closeButton, activeState);
    }

    public void IsWindowMaximized(bool value) => IsWindowMaximized(value, false);

    private void IsWindowMaximized(bool value, bool fromDispatcher)
    {
        if (_isWindowMaximized == value)
            return;

        if (!fromDispatcher && _maximizeButton.Dispatcher.HasThreadAccess is false)
        {
            _ = Dispatcher.RunAsync(CoreDispatcherPriority.Low, () => IsWindowMaximized(value, true));
            return;
        }

        if (VisualStateManager.GoToState(_maximizeButton, value ? "WindowStateMaximized" : "WindowStateNormal", false))
        {
            _isWindowMaximized = value;
            AutomationProperties.SetName(_maximizeButton, value ? "Restore" : "Maximize");
        }
        else if (!fromDispatcher)
        {
            _ = Dispatcher.RunAsync(CoreDispatcherPriority.Low, () => IsWindowMaximized(value, true));
        }
    }

    public void IsWindowActive(bool value)
    {
        _isWindowActive = value;

        var activeState = value ? "Normal" : "NotActive";
        GoToButtonState(_minimizeButton, activeState);
        GoToButtonState(_maximizeButton, activeState);
        GoToButtonState(_closeButton, activeState);
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e) => Window?.PostCaptionButtonCommand(CaptionButton.Minimize);

    private void MaximizeButton_Click(object sender, RoutedEventArgs e) => Window?.PostCaptionButtonCommand(CaptionButton.Maximize);

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Window?.PostCaptionButtonCommand(CaptionButton.Close);

    private Button CreateButton(string automationName, string styleKey, string glyph)
    {
        var button = new Button
        {
            Style = (Style)_captionButtonResources[styleKey],
            IsTabStop = false,
        };

        button.Resources["CaptionButtonGlyph"] = glyph;
        AutomationProperties.SetName(button, automationName);
        return button;
    }

    private void ApplyNormalState()
    {
        var activeState = _isWindowActive ? "Normal" : "NotActive";
        GoToButtonState(_minimizeButton, activeState);
        GoToButtonState(_maximizeButton, activeState);
        GoToButtonState(_closeButton, activeState);
    }

    private static void GoToButtonState(Button button, string state) => VisualStateManager.GoToState(button, state, false);
}
