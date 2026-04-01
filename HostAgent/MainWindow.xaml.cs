using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Core;

namespace HostAgent;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private ScreenCapture? _screenCapture;
    private ImageEncoder? _imageEncoder;
    private NetworkService? _networkService;
    private InputInjector? _inputInjector;
    private DispatcherTimer? _captureTimer;
    private Bitmap? _lastFrame;
    private string _sessionId = string.Empty;
    private bool _isSharing;
    private int _serverPort = 5000;

    public MainWindow()
    {
        InitializeComponent();
        InitializeComponents();
        SetupUI();
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
            Interval = TimeSpan.FromMilliseconds(100) // ~10 FPS
        };
        _captureTimer.Tick += CaptureTimer_Tick;
    }

    private void SetupUI()
    {
        Title = "Host Agent - Remote Desktop";
        Width = 400;
        Height = 300;

        var panel = new System.Windows.Controls.StackPanel { Margin = new Thickness(10) };

        // Session ID input
        panel.Children.Add(new System.Windows.Controls.TextBlock 
        { 
            Text = "Session ID:", 
            Margin = new Thickness(0, 5, 0, 5) 
        });
        var sessionIdBox = new System.Windows.Controls.TextBox 
        { 
            Name = "SessionIdBox",
            Margin = new Thickness(0, 0, 0, 10) 
        };
        panel.Children.Add(sessionIdBox);

        // Port input
        panel.Children.Add(new System.Windows.Controls.TextBlock 
        { 
            Text = "Server Port:", 
            Margin = new Thickness(0, 5, 0, 5) 
        });
        var portBox = new System.Windows.Controls.TextBox 
        { 
            Name = "PortBox",
            Text = "5000",
            Margin = new Thickness(0, 0, 0, 10) 
        };
        panel.Children.Add(portBox);

        // Status label
        var statusLabel = new System.Windows.Controls.TextBlock
        {
            Name = "StatusLabel",
            Text = "Status: Not connected",
            Foreground = System.Windows.Media.Brushes.Blue,
            Margin = new Thickness(0, 10, 0, 10)
        };
        panel.Children.Add(statusLabel);

        // Start/Stop button
        var startButton = new System.Windows.Controls.Button
        {
            Content = "Start Sharing",
            Name = "StartButton",
            Padding = new Thickness(10, 5, 10, 5),
            Margin = new Thickness(0, 5, 0, 5)
        };
        startButton.Click += (s, e) => StartSharing(sessionIdBox.Text, portBox.Text);
        panel.Children.Add(startButton);

        // Stop button
        var stopButton = new System.Windows.Controls.Button
        {
            Content = "Stop Sharing",
            Name = "StopButton",
            Padding = new Thickness(10, 5, 10, 5),
            Margin = new Thickness(0, 5, 0, 5),
            IsEnabled = false
        };
        stopButton.Click += (s, e) => StopSharing();
        panel.Children.Add(stopButton);

        // Register button references
        startButton.Tag = stopButton;
        stopButton.Tag = startButton;

        Content = panel;
    }

    private async void StartSharing(string sessionId, string portText)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            MessageBox.Show("Please enter a session ID", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(portText, out int port) || port < 1024 || port > 65535)
        {
            MessageBox.Show("Please enter a valid port (1024-65535)", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _sessionId = sessionId;
        _serverPort = port;

        // Start server to listen for client connections
        var started = await _networkService!.StartServerAsync(_serverPort);
        if (!started)
        {
            MessageBox.Show("Failed to start server. Port may be in use.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        _isSharing = true;
        _captureTimer?.Start();
        
        UpdateStatus("Sharing session: " + _sessionId);
        UpdateButtons(false, true);
    }

    private void StopSharing()
    {
        _isSharing = false;
        _captureTimer?.Stop();
        _networkService?.Disconnect();
        
        UpdateStatus("Sharing stopped");
        UpdateButtons(true, false);
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
