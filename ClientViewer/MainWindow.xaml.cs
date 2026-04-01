using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Core;

namespace ClientViewer;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// Auto-connect silent remote desktop client
/// </summary>
public partial class MainWindow : Window
{
    private NetworkService? _networkService;
    private Bitmap? _currentFrame;
    private int _hostWidth = 1920;
    private int _hostHeight = 1080;
    private bool _isConnected;
    private bool _isRemoteControl = true;
    private string _hostAddress = "127.0.0.1"; // Default fallback
    private int _hostPort = 5000;

    public MainWindow()
    {
        InitializeComponent();
        LoadConfig(); // Load config.json
        InitializeComponents();
        SetupUI();
        
        Loaded += MainWindow_Loaded;
    }

    private void LoadConfig()
    {
        try
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
            if (File.Exists(configPath))
            {
                string json = File.ReadAllText(configPath);
                var config = JsonSerializer.Deserialize<ConfigSettings>(json);
                if (config != null)
                {
                    _hostAddress = config.host ?? "127.0.0.1";
                    _hostPort = config.port > 0 ? config.port : 5000;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Config load error: {ex.Message}");
        }
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await Task.Delay(1000);
        await ConnectAsync(_hostAddress, _hostPort);
    }

    private void InitializeComponents()
    {
        _networkService = new NetworkService();
        _networkService.DataReceived += OnDataReceived;
        _networkService.StatusChanged += OnStatusChanged;
        _networkService.Disconnected += OnDisconnected;
    }

    private void SetupUI()
    {
        Title = "Remote Desktop Viewer";
        Width = 800;
        Height = 600;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Minimal top panel - just shows connection info
        var topPanel = new StackPanel { Margin = new Thickness(10), Orientation = Orientation.Horizontal };
        
        var hostLabel = new TextBlock 
        { 
            Text = $"Connecting to {_hostAddress}:{_hostPort}...",
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = System.Windows.Media.Brushes.Blue
        };
        topPanel.Children.Add(hostLabel);
        
        Grid.SetRow(topPanel, 0);
        grid.Children.Add(topPanel);

        // Center - Remote desktop display
        var image = new System.Windows.Controls.Image 
        { 
            Name = "RemoteImage",
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        
        var scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = image
        };
        Grid.SetRow(scrollViewer, 1);
        grid.Children.Add(scrollViewer);

        // Bottom - Status
        var statusPanel = new Border { Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(240, 240, 240)), Padding = new Thickness(10) };
        var statusLabel = new TextBlock { Name = "StatusLabel", Text = "Status: Connecting..." };
        statusPanel.Child = statusLabel;
        Grid.SetRow(statusPanel, 2);
        grid.Children.Add(statusPanel);

        Content = grid;
        
        // Handle mouse events for remote control
        scrollViewer.MouseMove += (s, e) => 
        {
            if (_isConnected && _isRemoteControl)
            {
                var pos = e.GetPosition(image);
                SendMouseMove((int)pos.X, (int)pos.Y);
            }
        };
        
        scrollViewer.MouseLeftButtonDown += (s, e) =>
        {
            if (_isConnected && _isRemoteControl)
            {
                var pos = e.GetPosition(image);
                SendMouseClick((int)pos.X, (int)pos.Y, "left", true);
            }
        };
        
        scrollViewer.MouseLeftButtonUp += (s, e) =>
        {
            if (_isConnected && _isRemoteControl)
            {
                var pos = e.GetPosition(image);
                SendMouseClick((int)pos.X, (int)pos.Y, "left", false);
            }
        };
        
        scrollViewer.MouseRightButtonDown += (s, e) =>
        {
            if (_isConnected && _isRemoteControl)
            {
                var pos = e.GetPosition(image);
                SendMouseClick((int)pos.X, (int)pos.Y, "right", true);
            }
        };
        
        scrollViewer.MouseRightButtonUp += (s, e) =>
        {
            if (_isConnected && _isRemoteControl)
            {
                var pos = e.GetPosition(image);
                SendMouseClick((int)pos.X, (int)pos.Y, "right", false);
            }
        };
        
        scrollViewer.MouseWheel += (s, e) =>
        {
            if (_isConnected && _isRemoteControl)
            {
                int delta = e.Delta > 0 ? 120 : -120;
                SendMouseScroll(delta);
            }
        };
        
        // Keyboard events
        scrollViewer.Focusable = true;
        scrollViewer.KeyDown += (s, e) =>
        {
            if (_isConnected && _isRemoteControl)
            {
                SendKeyPress((int)e.Key, true);
            }
        };
        
        scrollViewer.KeyUp += (s, e) =>
        {
            if (_isConnected && _isRemoteControl)
            {
                SendKeyPress((int)e.Key, false);
            }
        };
    }

    private async Task ConnectAsync(string host, int port)
    {
        _hostAddress = host;
        _hostPort = port;

        var connected = await _networkService!.ConnectAsync(_hostAddress, _hostPort);
        if (connected)
        {
            _isConnected = true;
            UpdateStatus("Connected - Remote Control Enabled");
        }
        else
        {
            // Try reconnecting after 2 seconds
            UpdateStatus("Connection failed - Retrying...");
            await Task.Delay(2000);
            await ConnectAsync(host, port);
        }
    }

    private void Disconnect()
    {
        _networkService?.Disconnect();
        _isConnected = false;
        UpdateStatus("Disconnected");
        UpdateButtons(true, false, false);
        
        // Clear the image
        if (Content is Grid grid && grid.Children.Count > 1 && grid.Children[1] is ScrollViewer sv)
        {
            if (sv.Content is System.Windows.Controls.Image img)
            {
                img.Source = null;
            }
        }
    }

    private void OnDataReceived(object? sender, byte[] data)
    {
        try
        {
            var message = ProtocolHandler.Deserialize(data);
            if (message is ScreenFrameMessage frame)
            {
                Dispatcher.Invoke(() => DisplayFrame(frame));
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error processing frame: {ex.Message}");
        }
    }

    private void DisplayFrame(ScreenFrameMessage frame)
    {
        try
        {
            var imageBytes = Convert.FromBase64String(frame.ImageDataBase64);
            using var ms = new MemoryStream(imageBytes);
            
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.StreamSource = ms;
            bitmapImage.EndInit();
            bitmapImage.Freeze();
            
            _hostWidth = frame.Width;
            _hostHeight = frame.Height;

            if (Content is Grid grid && grid.Children.Count > 1 && grid.Children[1] is ScrollViewer sv)
            {
                if (sv.Content is System.Windows.Controls.Image img)
                {
                    img.Source = bitmapImage;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error displaying frame: {ex.Message}");
        }
    }

    private void SendMouseMove(int x, int y)
    {
        if (_networkService == null || !_isConnected) return;
        
        // Scale coordinates
        int scaledX = (int)((double)x / _hostWidth * 65535);
        int scaledY = (int)((double)y / _hostHeight * 65535);
        
        var message = new MouseMoveMessage { X = scaledX, Y = scaledY };
        _ = _networkService.SendObjectAsync(message);
    }

    private void SendMouseClick(int x, int y, string button, bool isDown)
    {
        if (_networkService == null || !_isConnected) return;
        
        var message = new MouseClickMessage 
        { 
            X = x, 
            Y = y, 
            Button = button, 
            IsDown = isDown 
        };
        _ = _networkService.SendObjectAsync(message);
    }

    private void SendMouseScroll(int delta)
    {
        if (_networkService == null || !_isConnected) return;
        
        var message = new MouseScrollMessage { Delta = delta };
        _ = _networkService.SendObjectAsync(message);
    }

    private void SendKeyPress(int virtualKey, bool isDown)
    {
        if (_networkService == null || !_isConnected) return;
        
        var message = new KeyPressMessage { VirtualKey = virtualKey, IsDown = isDown };
        _ = _networkService.SendObjectAsync(message);
    }

    private void OnStatusChanged(object? sender, string status)
    {
        Dispatcher.Invoke(() => UpdateStatus(status));
    }

    private void OnDisconnected(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            _isConnected = false;
            UpdateStatus("Connection lost");
            UpdateButtons(true, false, false);
        });
    }

    private void UpdateStatus(string status)
    {
        if (Content is Grid grid && grid.Children.Count > 2 && grid.Children[2] is Border border)
        {
            if (border.Child is TextBlock tb)
            {
                tb.Text = $"Status: {status}";
            }
        }
    }

    private void UpdateButtons(bool enableConnect, bool enableDisconnect, bool enableControl)
    {
        if (Content is Grid grid && grid.Children.Count > 0 && grid.Children[0] is StackPanel sp)
        {
            foreach (var child in sp.Children)
            {
                if (child is StackPanel buttonPanel)
                {
                    foreach (var btn in buttonPanel.Children)
                    {
                        if (btn is Button b)
                        {
                            if (b.Name == "ConnectButton")
                                b.IsEnabled = enableConnect;
                            else if (b.Name == "DisconnectButton")
                                b.IsEnabled = enableDisconnect;
                        }
                        else if (btn is CheckBox cb)
                        {
                            if (cb.Name == "ControlCheckBox")
                                cb.IsEnabled = enableControl;
                        }
                    }
                }
            }
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _networkService?.Dispose();
        _currentFrame?.Dispose();
        base.OnClosed(e);
    }
}

/// <summary>
/// Configuration settings loaded from config.json
/// </summary>
public class ConfigSettings
{
    public string? host { get; set; }
    public int port { get; set; }
}
