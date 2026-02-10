using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.Gaming.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Pokerogue_
{
    public sealed partial class MainPage : Page
    {
        private const string StartUrl = "https://pokerogue.net";
        private const int ReloadDelayMs = 4510;

        // One-time delayed reload flag
        private bool _initialReloadDone;

        // Hidden exit combo
        private readonly DispatcherTimer _comboTimer = new DispatcherTimer();
        private DateTimeOffset _comboStart;
        private bool _comboActive;

        public MainPage()
        {
            InitializeComponent();
            Loaded += MainPage_Loaded;

            // Ignore Xbox B / Back button completely
            SystemNavigationManager.GetForCurrentView().BackRequested += OnBackRequested;

            // Poll for hidden exit combo (LB + RB + DPadDown)
            _comboTimer.Interval = TimeSpan.FromMilliseconds(100);
            _comboTimer.Tick += ComboTimer_Tick;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Fullscreen + full CoreWindow bounds (removes safe-area borders)
            var view = ApplicationView.GetForCurrentView();
            view.SetDesiredBoundsMode(ApplicationViewBoundsMode.UseCoreWindow);
            view.TryEnterFullScreenMode();

            // WebView2 init (UWP version – no env parameter)
            await Browser.EnsureCoreWebView2Async().AsTask();

            // Kiosk / performance settings
            var s = Browser.CoreWebView2.Settings;
            s.AreDevToolsEnabled = false;
            s.AreDefaultContextMenusEnabled = false;
            s.IsStatusBarEnabled = false;
            s.IsZoomControlEnabled = false;
            s.IsBuiltInErrorPageEnabled = false;
            s.AreBrowserAcceleratorKeysEnabled = false;
            s.IsGeneralAutofillEnabled = false;
            s.IsPasswordAutosaveEnabled = false;

            // Block popups / new windows
            Browser.CoreWebView2.NewWindowRequested += (o, args) =>
            {
                args.Handled = true;
                if (!string.IsNullOrWhiteSpace(args.Uri))
                {
                    Browser.CoreWebView2.Navigate(args.Uri);
                }
            };

            // One-time reload after first successful navigation
            TypedEventHandler<
                Microsoft.Web.WebView2.Core.CoreWebView2,
                Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs> navHandler = null!;

            navHandler = async (s2, e2) =>
            {
                if (_initialReloadDone) return;
                _initialReloadDone = true;

                try { Browser.CoreWebView2.NavigationCompleted -= navHandler; } catch { }

                await Task.Delay(ReloadDelayMs);

                try
                {
                    Browser.CoreWebView2.Reload();
                }
                catch { }
            };

            Browser.CoreWebView2.NavigationCompleted += navHandler;

            // Navigate
            Browser.CoreWebView2.Navigate(StartUrl);

            // Optional: keep focus on the webview for controller nav (doesn't affect cursor)
            Browser.Focus(FocusState.Programmatic);

            // Start watching for exit combo
            _comboTimer.Start();
        }

        // Ignore Xbox B / Back completely
        private void OnBackRequested(object? sender, BackRequestedEventArgs e)
        {
            e.Handled = true;
        }

        // Hidden exit combo: hold LB + RB + DPadDown for ~1 second
        private void ComboTimer_Tick(object? sender, object e)
        {
            if (Gamepad.Gamepads.Count == 0) return;

            var gp = Gamepad.Gamepads[0];
            var reading = gp.GetCurrentReading();

            bool comboPressed =
                reading.Buttons.HasFlag(GamepadButtons.LeftShoulder) &&
                reading.Buttons.HasFlag(GamepadButtons.RightShoulder) &&
                reading.Buttons.HasFlag(GamepadButtons.DPadDown);

            if (comboPressed)
            {
                if (!_comboActive)
                {
                    _comboActive = true;
                    _comboStart = DateTimeOffset.Now;
                }
                else if ((DateTimeOffset.Now - _comboStart).TotalMilliseconds >= 1000)
                {
                    Application.Current.Exit();
                }
            }
            else
            {
                _comboActive = false;
            }
        }
    }
}
