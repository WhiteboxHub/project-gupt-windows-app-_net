using System.Drawing;
using System.Windows;
using System.Windows.Threading;
using Core;

namespace HostAgent;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// Auto-start silent remote desktop host
/// </summary>
public partial class MainWindow : Window
{
    private ScreenCapture? _screenCapture;
    private ImageEncoder? _imageEncoder;
    private NetworkService? _networkService;
    private InputInjector? _inputInjector;
    private DispatcherTimer? _captureTimer;
    private Bitmap? _lastFrame;
    private string _sessionId = "auto-share";
    private bool _isSharing;
    private int _serverPort = 5000;

    public MainWindow()
    {
        InitializeComponent();
        InitializeComponents();
        SetupMinimalUI();
        
        // Auto-start sharing on load
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Auto-start sharing immediately
        await StartSharingAsync();
        
        // Optionally hide window after starting
        // Uncomment the next line to run silently in background
        // Hide();
    }

    private void InitializeComponents()
    {
        _screenCapture = new ScreenCapture();
        _imageEncoder = new ImageEncoder();
        _networkService = new NetworkService();
        _inputInjector = new InputInjector();

        _networkService.DataReceived += OnDataReceived;
        _networkService.StatusChanged += OnStatusChanged;
        _networkService.Disconnected += OnDisconnected;

        _captureTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(100)
        };
        _captureTimer.Tick += CaptureTimer_Tick;
    }

    private void SetupMinimalUI()
    {
        Title = "Remote Desktop Host";
        Width = 300;
        Height = 120;
        WindowStyle = WindowStyle.ToolWindow;
        
        var panel = new System.Windows.Controls.StackPanel { Margin = new Thickness(10) };

        // Minimal status display
        var statusLabel = new System.Windows.Controls.TextBlock
        {
            Name = "StatusLabel",
            Text = "Starting...",
            Foreground = System.Windows.Media.Brushes.Green,
            Margin = new Thickness(0, 10, 0, 10)
        };
        panel.Children.Add(statusLabel);

        Content = panel;
    }

    private async Task StartSharingAsync()
    {
        _sessionId = "auto-share";
        _serverPort = 5000;

        // Start server - auto-accept all connections
        var started = await _networkService!.StartServerAsync(_serverPort);
        if (!started)
        {
            UpdateStatus("Failed to start server");
            return;
        }

        _isSharing = true;
        _captureTimer?.Start();
        
        UpdateStatus($"Sharing on port {_serverPort}");
    }

    private void StopSharing()
    {
        _isSharing = false;
        _captureTimer?.Stop();
        _networkService?.Disconnect();
        
        UpdateStatus("Sharing stopped");
    }

    private async void CaptureTimer_Tick(object? sender, EventArgs e)
    {
        if (!_isSharing || _networkService == null || !_networkService.IsConnected) return;

        try
        {
            var bitmap = _screenCapture!.CaptureScreen();
            var jpegData = _imageEncoder!.EncodeToJpegStream(bitmap, 60);
            
            var message = new ScreenFrameMessage
            {
                Width = bitmap.Width,
                Height = bitmap.Height,
                ImageDataBase64 = Convert.ToBase64String(jpegData),
                Format = "jpeg",
                Quality = 60
            };

            await _networkService.SendObjectAsync(message);
            
            _lastFrame?.Dispose();
            _lastFrame = bitmap;
        }
        catch
        {
            // Silently handle capture errors to avoid UI flooding
        }
    }

    private async void OnDataReceived(object? sender, byte[] data)
    {
        try
        {
            var message = ProtocolHandler.Deserialize(data);
            if (message == null) return;

            switch (message)
            {
                case MouseMoveMessage move:
                    _inputInjector?.SetCursorPosition(move.X, move.Y);
                    break;

                case MouseClickMessage click:
                    _inputInjector?.SetCursorPosition(click.X, click.Y);
                    if (click.Button == "left")
                    {
                        if (click.IsDown) _inputInjector?.LeftClick();
                    }
                    else if (click.Button == "right")
                    {
                        if (click.IsDown) _inputInjector?.RightClick();
                    }
                    break;

                case MouseScrollMessage scroll:
                    _inputInjector?.ScrollWheel(scroll.Delta);
                    break;

                case KeyPressMessage key:
                    _inputInjector?.PressKey((short)key.VirtualKey);
                    break;

                case TextInputMessage text:
                    _inputInjector?.TypeText(text.Text);
                    break;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error processing message: {ex.Message}");
        }
    }

    private void OnStatusChanged(object? sender, string status)
    {
        Dispatcher.Invoke(() => UpdateStatus(status));
    }

    private void OnDisconnected(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            _isSharing = false;
            _captureTimer?.Stop();
            UpdateStatus("Client disconnected");
            UpdateButtons(true, false);
        });
    }

    private void UpdateStatus(string status)
    {
        if (Content is System.Windows.Controls.StackPanel panel)
        {
            foreach (var child in panel.Children)
            {
                if (child is System.Windows.Controls.TextBlock tb && tb.Name == "StatusLabel")
                {
                    tb.Text = $"Status: {status}";
                    break;
                }
            }
        }
    }

    private void UpdateButtons(bool enableStart, bool enableStop)
    {
        if (Content is System.Windows.Controls.StackPanel panel)
        {
            foreach (var child in panel.Children)
            {
                if (child is System.Windows.Controls.Button btn)
                {
                    if (btn.Name == "StartButton")
                        btn.IsEnabled = enableStart;
                    else if (btn.Name == "StopButton")
                        btn.IsEnabled = enableStop;
                }
            }
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _captureTimer?.Stop();
        _networkService?.Dispose();
        _lastFrame?.Dispose();
        base.OnClosed(e);
    }
}
