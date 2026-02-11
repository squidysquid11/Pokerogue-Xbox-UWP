using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Gaming.Input;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;

namespace Pokerogue_
{
    public sealed partial class MainPage : Page
    {
        private const string StartUrl = "https://pokerogue.net";

        // Wait before attempting recovery after error
        private const int RestartDelayMs = 2500;

        // about:blank wait + settle
        private const int BlankTimeoutMs = 2000;
        private const int SettleAfterBlankMs = 400;

        // If we navigate to StartUrl after an error and still don't reach DOMContentLoaded,
        // treat it as stuck/black-screen and try again.
        private const int RecoveryDomTimeoutMs = 12000;

        private bool _restartInProgress;
        private int _recoveryGeneration;

        private DateTimeOffset _lastDomContentLoaded = DateTimeOffset.MinValue;

        // Hidden exit combo
        private readonly DispatcherTimer _comboTimer = new DispatcherTimer();
        private DateTimeOffset _comboStart;
        private bool _comboActive;

        public MainPage()
        {
            InitializeComponent();
            Loaded += MainPage_Loaded;

            SystemNavigationManager.GetForCurrentView().BackRequested += OnBackRequested;

            _comboTimer.Interval = TimeSpan.FromMilliseconds(100);
            _comboTimer.Tick += ComboTimer_Tick;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Fullscreen + full CoreWindow bounds
            var view = ApplicationView.GetForCurrentView();
            view.SetDesiredBoundsMode(ApplicationViewBoundsMode.UseCoreWindow);
            view.TryEnterFullScreenMode();

            await Browser.EnsureCoreWebView2Async().AsTask();

            // Kiosk settings
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

            // Track a "healthy" load
            Browser.CoreWebView2.DOMContentLoaded += (o, e) =>
            {
                _lastDomContentLoaded = DateTimeOffset.Now;
            };

            // Error detection
            Browser.CoreWebView2.NavigationCompleted += async (o, e2) =>
            {
                if (!e2.IsSuccess)
                {
                    await RestartLikeBrowserAsync();
                }
            };

            Browser.CoreWebView2.ProcessFailed += async (o, e) =>
            {
                await RestartLikeBrowserAsync();
            };

            // Initial navigation
            Browser.CoreWebView2.Navigate(StartUrl);
            Browser.Focus(FocusState.Programmatic);

            _comboTimer.Start();
        }

        /// <summary>
        /// Stop → about:blank (wait) → settle → StartUrl
        /// plus a watchdog to retry if we get stuck on black screen / never reach DOMContentLoaded.
        /// </summary>
        private async Task RestartLikeBrowserAsync()
        {
            if (_restartInProgress) return;
            _restartInProgress = true;

            int myGen = ++_recoveryGeneration;

            try
            {
                await Task.Delay(RestartDelayMs);

                var wv = Browser?.CoreWebView2;
                if (wv == null) return;

                // 1) Stop
                try { wv.Stop(); } catch { }

                // 2) Navigate to about:blank and wait for it to land
                var tcsBlank = new TaskCompletionSource<bool>();

                TypedEventHandler<CoreWebView2, CoreWebView2NavigationCompletedEventArgs> blankHandler = null!;
                blankHandler = (s, e) =>
                {
                    try
                    {
                        if (string.Equals(s.Source, "about:blank", StringComparison.OrdinalIgnoreCase))
                            tcsBlank.TrySetResult(true);
                    }
                    catch { }
                };

                wv.NavigationCompleted += blankHandler;
                try
                {
                    try { wv.Navigate("about:blank"); } catch { }
                    await Task.WhenAny(tcsBlank.Task, Task.Delay(BlankTimeoutMs));
                }
                finally
                {
                    try { wv.NavigationCompleted -= blankHandler; } catch { }
                }

                await Task.Delay(SettleAfterBlankMs);

                // 3) Navigate back to Pokerogue
                try { wv.Navigate(StartUrl); } catch { }

                // 4) Watchdog: if DOMContentLoaded doesn't update soon, run recovery again
                _ = RecoveryWatchdogAsync(myGen, _lastDomContentLoaded);
            }
            finally
            {
                _restartInProgress = false;
            }
        }

        private async Task RecoveryWatchdogAsync(int gen, DateTimeOffset domStampAtStart)
        {
            await Task.Delay(RecoveryDomTimeoutMs);

            // If another recovery started, ignore this watchdog
            if (gen != _recoveryGeneration) return;

            // If we have DOMContentLoaded since we began, we're fine
            if (_lastDomContentLoaded > domStampAtStart) return;

            // Still stuck -> try the same recovery again
            await RestartLikeBrowserAsync();
        }

        private void OnBackRequested(object? sender, BackRequestedEventArgs e)
        {
            e.Handled = true;
        }

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
