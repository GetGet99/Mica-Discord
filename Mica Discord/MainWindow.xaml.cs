using Microsoft.Web.WebView2.Core;
using PInvoke;
using System;
using System.Drawing;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using Windows.UI.ViewManagement;
using AccentColorTypes = CustomPInvoke.UxTheme.AccentColorTypes;
//using WindowMessage = PInvoke.User32.WindowMessage;

namespace MicaDiscord;

public partial class MainWindow : Window
{
    static UISettings UISettings { get; } = new();
    static bool IsExcessiveAccentColorEnabled => Settings.Default.ExcessiveAccentColor;
    const string Radius = "0.5rem";
    public static readonly bool NotSupportedBuild = Environment.OSVersion.Version.Build < 22523;
    bool DiscordEffectApplied = false;
    bool _Dark = true;
    bool Dark
    {
        set
        {
            _Dark = value;
            if (!IsExcessiveAccentColorEnabled) (Resources["Color"] as SolidColorBrush ?? throw new NullReferenceException()).Color = value ? Colors.White : Colors.Black;
        }
        get => _Dark;
    }
    
    void OnLoaded(object sender, RoutedEventArgs e)
    {
        void RefreshFrame()
        {
            HwndSource mainWindowSrc = HwndSource.FromHwnd(Handle);
            
            if (NotSupportedBuild && !Settings.Default.UseBackdropAnyway)
            {
                mainWindowSrc.CompositionTarget.BackgroundColor = Dark ? System.Windows.Media.Color.FromRgb(50, 50, 50) :
                       System.Windows.Media.Color.FromRgb(250, 250, 250);
            } else
            {
                mainWindowSrc.CompositionTarget.BackgroundColor = System.Windows.Media.Color.FromArgb(0, 0, 0, 0);
                DwmApi.DwmExtendFrameIntoClientArea(Handle, new()
                {
                    cxLeftWidth = -1,
                    cxRightWidth = -1,
                    cyTopHeight = -1,
                    cyBottomHeight = -1,
                });
            }
        }
        void RefreshDarkMode(bool dark) => CustomPInvoke.DwmApi.SetWindowAttribute(
            Handle,
            CustomPInvoke.DwmApi.DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE,
            dark ? 1 : 0 // Always dark as of now, or you can change to: ThemeManager.Current.ActualApplicationTheme == ApplicationTheme.Dark ? 1 : 0
        );
        static bool IsDarkBackground(Windows.UI.Color color)
        {
            return color.R + color.G + color.B < (255 * 3 - color.R - color.G - color.B);
        }
        RefreshDarkMode(dark: IsDarkBackground(UISettings.GetColorValue(UIColorType.Background)));
        SizeChanged += (_, _) => RefreshFrame();
        IsVisibleChanged += (_, _) => RefreshFrame();
        
        Width += 1;
        bool UserRequestOpenNewWindow = false;
        SetBackdrop((CustomPInvoke.BackdropType)Enum.Parse(typeof(CustomPInvoke.BackdropType), Settings.Default.BackdropType, ignoreCase: true));
        WebView.CoreWebView2InitializationCompleted += (_, _) =>
        {
            var CoreWebView2 = WebView.CoreWebView2;
            CoreWebView2.NewWindowRequested += (_, e) =>
            {
                if (UserRequestOpenNewWindow)
                {
                    UserRequestOpenNewWindow = false;
                    return;
                }
                System.Diagnostics.Process.Start("explorer", e.Uri);
                e.Handled = true;
            };
            
            CoreWebView2.ContextMenuRequested += (_, e) =>
            {
                if (e.ContextMenuTarget.HasLinkUri)
                {
                    var newItem = CoreWebView2.Environment.CreateContextMenuItem("Open In Default Browser", null, CoreWebView2ContextMenuItemKind.Command);
                    newItem.CustomItemSelected += (_, e2) =>
                    {
                        System.Diagnostics.Process.Start("explorer", e.ContextMenuTarget.LinkUri);
                    };
                    e.MenuItems.Insert(0, CoreWebView2.Environment.CreateContextMenuItem("", null, CoreWebView2ContextMenuItemKind.Separator));
                    e.MenuItems.Insert(0, newItem);

                    foreach (var menu in e.MenuItems)
                    {
                        if (menu.Label.ToLower().Replace("&","").Contains("new window")) menu.CustomItemSelected += (_, e) =>
                        {
                            UserRequestOpenNewWindow = true;
                        };
                    }
                }
            };
            var handle = WebView.Handle;
            _ = User32.SetWindowLong(handle, User32.WindowLongIndexFlags.GWL_EXSTYLE,
                (User32.SetWindowLongFlags)User32.GetWindowLong(handle, User32.WindowLongIndexFlags.GWL_EXSTYLE)
                | User32.SetWindowLongFlags.WS_CLIPCHILDREN
            );

            CoreWebView2.DocumentTitleChanged += (_, _) =>
            {
                var title = WebView.CoreWebView2.DocumentTitle;
                if (title == "Discord")
                {
                    title = "";
                    Title = "Mica Discord";
                } else Title = $"Mica Discord - {title}";
                WebsiteTitle.Text = title;
            };
            CoreWebView2.HistoryChanged += delegate
            {
                Back.IsEnabled = WebView.CanGoBack;
                Forward.IsEnabled = WebView.CanGoForward;
                RefreshFrame();
            };


            RefreshFrame();
        };
        WebView.NavigationCompleted += async delegate
        {

            if (!WebView.Source.OriginalString.Contains("discord.com")) return;
            DiscordEffectApplied = Settings.Default.ReplaceDiscordBackground;
            if (DiscordEffectApplied)
            {
                var Dark = (await WebView.CoreWebView2.ExecuteScriptAsync("document.getElementsByTagName('html')[0].classList.contains('theme-dark')")) == "true";
                var Light = (await WebView.CoreWebView2.ExecuteScriptAsync("document.getElementsByTagName('html')[0].classList.contains('theme-light')")) == "true";
                if (Dark is false && Light is false)
                    Dark = this.Dark; // Don't change
                var PrimaryColor = UISettings.GetColorValue(UIColorType.AccentLight3);//CustomPInvoke.GetAccentColor(Dark ? AccentColorTypes.ImmersiveSaturatedHighlight : AccentColorTypes.ImmersiveSaturatedSelectionBackground);
                var DisabledColor = UISettings.GetColorValue(UIColorType.AccentLight2);//CustomPInvoke.GetAccentColor(AccentColorTypes.ImmersiveSaturatedCommandRowDisabled);
                DisabledColor.A /= 2;
                var HoverColor = UISettings.GetColorValue(UIColorType.AccentLight2);
                var Accent = UISettings.GetColorValue(UIColorType.Accent);
                if (IsExcessiveAccentColorEnabled) (Resources["Color"] as SolidColorBrush ?? throw new NullReferenceException()).Color = System.Windows.Media.Color.FromArgb(PrimaryColor.A, PrimaryColor.R, PrimaryColor.G, PrimaryColor.B);

                /*
                static double GetHue(int red, int green, int blue)
                {

                    float min = Math.Min(Math.Min(red, green), blue);
                    float max = Math.Max(Math.Max(red, green), blue);

                    if (min == max)
                    {
                        return 0;
                    }

                    float hue;
                    if (max == red) hue = (green - blue) / (max - min);
                    else if (max == green) hue = 2f + (blue - red) / (max - min);
                    else hue = 4f + (red - green) / (max - min);

                    hue *= 60;
                    if (hue < 0) hue += 360;

                    return hue;
                }
                */
                RefreshDarkMode(dark: Dark);
                this.Dark = Dark;
                var LightColorCSS = Dark && Settings.Default.ModeAwareCSS;
                var invc = LightColorCSS ? 250 : 0;
                var regcgray = LightColorCSS ? 50 : 200;
                var floating = Dark ? 0 : 255;
                var ErrorAccentColor = CustomPInvoke.UxTheme.GetAccentColor(AccentColorTypes.ImmersiveSaturatedInlineErrorText);
                await WebView.CoreWebView2.ExecuteScriptAsync($@"
(function () {{
    let s = document.createElement('style');
    s.innerHTML = `
* {{ /* System Color */
    --sys-accent-prop: {Accent.R}, {Accent.G}, {Accent.B};
    --sys-accent-color: rgba({Accent.R}, {Accent.G}, {Accent.B}, {Accent.A});
    --sys-accent-light-3-color: rgba({PrimaryColor.R}, {PrimaryColor.G}, {PrimaryColor.B}, {PrimaryColor.A});
    --sys-accent-disabled-color: rgba({DisabledColor.R}, {DisabledColor.G}, {DisabledColor.B}, {DisabledColor.A});
    --sys-accent-disabled-color-half: rgba({DisabledColor.R}, {DisabledColor.G}, {DisabledColor.B}, {DisabledColor.A / 2});
    --sys-error-accent-color: rgba({ErrorAccentColor.R}, {ErrorAccentColor.G}, {ErrorAccentColor.B}, {ErrorAccentColor.A});
    --sys-hover-accent-color: rgba({HoverColor.R}, {HoverColor.G}, {HoverColor.B}, {HoverColor.A});
    --sys-light-color-bg: {(LightColorCSS ? 1 : 0)};
    --sys-layering-strength: {invc};
    --sys-floating-strength: {floating};
    --sys-non-tran-bg-gray-strength: {regcgray};
    --sys-border-radius: {Radius};
}}
* {{ /* Predefined Theme Color */
    --theme-layering-prop: var(--sys-layering-strength), var(--sys-layering-strength), var(--sys-layering-strength);
    --theme-layering-solid-color: rgb(--theme-layering-prop);
    --theme-bg-layering-double: rgba(var(--theme-layering-prop),0.1);
    --theme-bg-layering: rgba(var(--theme-layering-prop),0.05);
    --theme-bg-layering-half: rgba(var(--theme-layering-prop),0.025);
    --theme-floating-prop: var(--sys-floating-strength), var(--sys-floating-strength), var(--sys-floating-strength);
    --theme-bg-floating: rgba(var(--theme-floating-prop),0.75);
    --theme-scrollbar-color: rgba(var(--theme-layering-prop),0.25);
}}
* {{
    --background-primary: var(--theme-bg-layering);
    --background-secondary: transparent;
    --background-secondary-alt: rgba(var(--theme-layering-prop),0.075);
    --background-tertiary: transparent;
    --background-message-hover: rgba(var(--theme-layering-prop),0.07);
    --background-floating: var(--theme-bg-floating);

    --activity-card-background: var(--theme-bg-layering);
    --deprecated-store-bg: var(--background-primary);
    --channeltextarea-background: var(--theme-bg-layering);
    --input-background: var(--theme-bg-layering);
    --scrollbar-auto-track: transparent;
    --scrollbar-thin-track: #0000;
    --scrollbar-thin-thumb: --theme-scrollbar-color;
    --scrollbar-auto-thumb: --theme-scrollbar-color;
    --background-modifier-hover: var(--theme-bg-layering-half);
    --background-modifier-selected: var(--theme-bg-layering);
    --background-mentioned-hover: hsla(38,calc(var(--saturation-factor, 1)*95.7%),54.1%,calc(0.08 + var(--sys-light-color-bg) * 0.1));
    --text-link: var(--sys-accent-light-3-color);
}}
* {{
{(
    IsExcessiveAccentColorEnabled ? @"
    --interactive-active: var(--sys-accent-light-3-color);
    --interactive-normal: var(--sys-accent-light-3-color);
    --interactive-hover: var(--sys-hover-accent-color);
    --interactive-disabled: var(--sys-accent-disabled-color-half);
    --text-muted: var(--sys-accent-disabled-color-half);
    --channels-default: var(--sys-accent-disabled-color);
" : ""
)}
    
}}
* {{
    --brand-experiment-100: var(--sys-accent-color);
    --brand-experiment-130: var(--sys-accent-color);
    --brand-experiment-160: var(--sys-accent-color);
    --brand-experiment-200: var(--sys-accent-color);
    --brand-experiment-230: var(--sys-accent-color);
    --brand-experiment-260: var(--sys-accent-color);
    --brand-experiment-300: var(--sys-accent-color);
    --brand-experiment-330: var(--sys-accent-color);
    --brand-experiment-360: var(--sys-accent-color);
    --brand-experiment-400: var(--sys-accent-color);
    --brand-experiment-430: var(--sys-accent-color);
    --brand-experiment-460: var(--sys-accent-color);
    --brand-experiment: var(--sys-accent-color);
    --brand-experiment-500: var(--sys-accent-color);
    --brand-experiment-530: var(--sys-accent-color);
    --brand-experiment-560: var(--sys-accent-color);
    --brand-experiment-600: var(--sys-accent-color);
    --brand-experiment-630: var(--sys-accent-color);
    --brand-experiment-660: var(--sys-accent-color);
    --brand-experiment-700: var(--sys-accent-color);
    --brand-experiment-730: var(--sys-accent-color);
    --brand-experiment-760: var(--sys-accent-color);
    --brand-experiment-800: var(--sys-accent-color);
    --brand-experiment-830: var(--sys-accent-color);
    --brand-experiment-860: var(--sys-accent-color);
    --brand-experiment-900: var(--sys-accent-color);
    --brand-experiment-05a: rgba(var(--sys-accent-prop),0.05);
    --brand-experiment-10a: rgba(var(--sys-accent-prop),0.1);
    --brand-experiment-15a: rgba(var(--sys-accent-prop),0.15);
    --brand-experiment-20a: rgba(var(--sys-accent-prop),0.2);
    --brand-experiment-25a: rgba(var(--sys-accent-prop),0.25);
    --brand-experiment-30a: rgba(var(--sys-accent-prop),0.3);
    --brand-experiment-35a: rgba(var(--sys-accent-prop),0.35);
    --brand-experiment-40a: rgba(var(--sys-accent-prop),0.4);
    --brand-experiment-45a: rgba(var(--sys-accent-prop),0.45);
    --brand-experiment-50a: rgba(var(--sys-accent-prop),0.5);
    --brand-experiment-55a: rgba(var(--sys-accent-prop),0.55);
    --brand-experiment-60a: rgba(var(--sys-accent-prop),0.6);
    --brand-experiment-65a: rgba(var(--sys-accent-prop),0.65);
    --brand-experiment-70a: rgba(var(--sys-accent-prop),0.7);
    --brand-experiment-75a: rgba(var(--sys-accent-prop),0.75);
    --brand-experiment-80a: rgba(var(--sys-accent-prop),0.8);
    --brand-experiment-85a: rgba(var(--sys-accent-prop),0.85);
    --brand-experiment-90a: rgba(var(--sys-accent-prop),0.9);
    --brand-experiment-95a: rgba(var(--sys-accent-prop),0.95);
}}
.appMount-2yBXZl /* Main application */
{{
    border-radius: var(--sys-border-radius);
}}
.content-3spvdd {{
    --background-primary: rgb(var(--sys-non-tran-bg-gray-strength), var(--sys-non-tran-bg-gray-strength), var(--sys-non-tran-bg-gray-strength));
}}
code, article {{
    --background-secondary: var(--theme-bg-layering);
}}
.lookFilled-yCfaCM.colorPrimary-2AuQVo, .lookFilled-yCfaCM.colorGrey-2iAG-B {{
    background-color: --theme-bg-layering !important;
}}
.content-2a4AW9 {{
    --background-secondary: var(--theme-bg-layering-half);
    border-color: black;
}}

.form-3gdLxP {{
    margin-top: 0px !important;
    padding-top: 16px !important;
}}
.form-3gdLxP:before {{
    width: 0px !important;
    height: 0px !important;
}}
.scroller-kQBbkU::-webkit-scrollbar-track {{
    margin-bottom: 0px !important;
}}
.scrollerSpacer-3AqkT9 {{
    display: none;
}}
.content-2a4AW9, .members-3WRCEx, .membersWrap-3NUR2t {{
    min-height: 100%;
    padding: 0px !important;
}}
.content-FDHp32 a {{
    color: var(--text-link) !important;
}}
.chat-2ZfjoI, .container-2cd8Mz, .container-36u7Lw, .applicationStore-2nk7Lo {{
    border-radius: var(--sys-border-radius) 0px 0px 0px;
}}
.chatContent-3KubbW {{
    background-color: var(--theme-bg-layering-half) !important;
    border-color: black;
}}
.container-2cd8Mz {{
    background-color: --theme-bg-layering !important;
}}

.callContainer-HtHELf {{
    background-color: transparent;
}}

.panels-3wFtMD {{
    border-radius: var(--sys-border-radius) 0px 0px 0px;
}}
.message-2CShn3 {{
    border-radius: var(--sys-border-radius);
}}
.sidebar-1tnWFu {{
border-radius: var(--sys-border-radius) var(--sys-border-radius) 0px 0px;
}}
.popout-TdhJ6Z {{
    --background-tertiary: var(--theme-bg-floating);
}}
.popout-1KHNAq {{
    --background-secondary: var(--theme-bg-floating);
}}
.lookFilled-1GseHa.select-1Ia3hD {{
    --background-secondary: var(--theme-bg-layering);
    --background-tertiary: rgba(var(--theme-layering-prop),0.75);
}}
.unread-36eUEm, .item-2LIpTv {{
    border-radius: 4px;
}}

.item-2LIpTv {{
    margin: 0px 0px 0px 2px;
    width: 4px;
    background: var(--sys-accent-light-3-color);
}}
.gradientContainer-phMG8d /* In-Call Gradient effect on top and bottom */
{{
    height: 40px;
    background-image: linear-gradient(--theme-bg-layering,transparent);
}}
.unread-36eUEm /* Text Channel Unread */
{{
    display: none;
    background-color: var(--sys-accent-light-3-color);
}}
.wrapper-NhbLHG::before /* Text Channel (before) */
{{
    content: """";
    position: absolute;
    height: 0px;
    width: 4px;
    border-radius: 4px;
    top: 50%;
    left: 0;
    margin-top: 0px;
    background: var(--sys-accent-light-3-color);
    transition: height 0.1s linear, margin-top 0.1s linear;
}}
.wrapper-NhbLHG:hover::before /* Text Channel (Hover, before) */
{{
    height: 14px;
    margin-top: -7px;
}}
.modeUnread-3Cxepe::before /* Text Channel Unread (before) */
{{
    height: 8px;
    margin-top: -4px;
}}
.modeSelected-3DmyhH::before /* Text Channel Selected */
{{
    height: 18px !important;
    margin-top: -9px !important;
}}
.modeUnread-3Cxepe .channelName-3KPsGw {{
    font-weight: 600;
}}
.hljs-comment {{
    color: seagreen !important;
}}
[id*=""popout""] /* Thread Menu and Pinned Message */
{{
    background-color: var(--background-floating);
    border-radius: var(--sys-border-radius);
}}
.outer-2JOHae.active-1W_Gl9, .outer-2JOHae.interactive-2zD88a:hover /* The card in Home -> Friend page background */
{{
    background-color: var(--theme-bg-layering-double) !important;
}}
.inset-SbsSFp /* The card in Home -> Friend page -> Inner Card */
{{
    background-color: var(--theme-bg-layering) !important;
}}
`.trim();
    document.head.appendChild(s);
}})()
".Trim());
                await WebView.CoreWebView2.ExecuteScriptAsync($@"
(function () {{
    
}})()
".Trim());
                RefreshFrame();
            }
        };
        
        WebView.WebMessageReceived += (_, e) =>
        {
            string s = e.TryGetWebMessageAsString();
            switch (s)
            {
                default:
                    break;
            }
        };
        Closing += (_, e) =>
        {
            if (!ForceClose && Settings.Default.UseSystemTray)
            {
                e.Cancel = true;
                Hide();
            }
        };
    }
    private void OpenSettings(object sender, RoutedEventArgs e)
    {
        SettingsDialog.ResetRequiresReload();
        DialogPlace.Visibility = Visibility.Visible;
        WebView.Visibility = Visibility.Hidden;
    }

    private void RefreshPage(object sender, RoutedEventArgs e)
    {
        try
        {
            WebView.Reload();
        } catch { }
    }
    public static SolidColorBrush GetColorFromHex(string hexaColor)
    {
        return new SolidColorBrush(
            System.Windows.Media.Color.FromArgb(
            Convert.ToByte(hexaColor[1..2], 16),
            Convert.ToByte(hexaColor[3..2], 16),
            Convert.ToByte(hexaColor[5..2], 16),
            Convert.ToByte(hexaColor[7..2], 16)
        ));
    }
    public static System.Windows.Media.Color GetColorFromUInt(uint value)
        => System.Windows.Media.Color.FromArgb(
            (byte)(value >> 24),
            (byte)(value >> 16),
            (byte)(value >> 8),
            (byte)(value)
        );
}
public partial class MainWindow : Window
{
    public bool ForceClose { get; set; } = false;
    public MainWindow()
    {
        AllowsTransparency = false;
        WindowInteropHelper = new WindowInteropHelper(this);
        InitializeComponent();
        Loaded += OnLoaded;
        SettingsDialog.OnClose += () =>
        {
            DialogPlace.Visibility = Visibility.Hidden;
            WebView.Visibility = Visibility.Visible;
            if (SettingsDialog.RequiresReload)
                WebView.Reload();
        };
        SettingsDialog.OnSettingsChanged += () =>
        {
            SetBackdrop((CustomPInvoke.BackdropType)Enum.Parse(typeof(CustomPInvoke.BackdropType), Settings.Default.BackdropType, ignoreCase: true));
        };
        //Icon = ImageSourceFromBitmap(ProgramResources.Logo);
        Back.Click += (_, _) =>
        {
            if (WebView.CanGoBack) WebView.GoBack();
        };
        Forward.Click += (_, _) =>
        {
            if (WebView.CanGoForward) WebView.GoForward();
        };
        SizeChanged += (_, _) =>
        {
            var a = WindowState == WindowState.Maximized ? 7.5 : 0;
            TitleBar.Margin = new Thickness(a, a, 0, 0);
            WebView.Margin = new Thickness(7.5, 0, 7.5, 7.5);
        };
        SetValue(WindowChrome.WindowChromeProperty, WindowChrome);
        WindowChrome.SetIsHitTestVisibleInChrome(Back, true);
        WindowChrome.SetIsHitTestVisibleInChrome(Forward, true);
        WindowChrome.SetIsHitTestVisibleInChrome(Reload, true);
        WindowChrome.SetIsHitTestVisibleInChrome(Setting, true);
        using var g = Graphics.FromImage(new Bitmap(1, 1));
        var dpix = g.DpiX;
    }
    WindowChrome WindowChrome { get; } = new WindowChrome
    {
        UseAeroCaptionButtons = true,
        ResizeBorderThickness = new Thickness(7.5)
    };
    public static ImageSource ImageSourceFromBitmap(Bitmap bmp)
    {
        var handle = bmp.GetHbitmap();
        try
        {
            return Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        }
        finally { Gdi32.DeleteObject(handle); }
    }

    void SetBackdrop(CustomPInvoke.BackdropType BackdropType) => SetBackdrop((int)BackdropType);
    void SetBackdrop(int BackdropType)
    {
        CustomPInvoke.DwmApi.SetWindowAttribute(
            Handle,
            CustomPInvoke.DwmApi.DWMWINDOWATTRIBUTE.DWMWA_SYSTEMBACKDROP_TYPE,
            BackdropType);
    }

    public WindowInteropHelper WindowInteropHelper { get; }
    public IntPtr Handle => WindowInteropHelper.Handle;
    
}
