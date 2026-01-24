using System.Collections.ObjectModel;
using System.Drawing;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using KeyRecorder.Core.IPC;
using KeyRecorder.Core.Data;
using KeyRecorder.Core.Models;
using KeyRecorder.Core.Capture;
using Forms = System.Windows.Forms;

namespace KeyRecorder.UI;

public partial class MainWindow : Window
{
    private IpcClient? _ipcClient;
    private DatabaseManager? _databaseManager;
    private KeyboardHook? _keyboardHook;
    private readonly DispatcherTimer _refreshTimer;
    private readonly ObservableCollection<TimelineEntry> _timelineEntries;
    private bool _isRecording;
    private Forms.NotifyIcon? _notifyIcon;
    private bool _isExiting;

    public MainWindow()
    {
        InitializeComponent();

        _timelineEntries = new ObservableCollection<TimelineEntry>();
        KeystrokeTimeline.ItemsSource = _timelineEntries;

        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        _refreshTimer.Tick += RefreshTimer_Tick;

        Loaded += MainWindow_Loaded;
        Closing += MainWindow_Closing;
        StateChanged += MainWindow_StateChanged;

        InitializeNotifyIcon();
    }

    private void InitializeNotifyIcon()
    {
        _notifyIcon = new Forms.NotifyIcon
        {
            Text = "KeyRecorder - Keyboard Activity Monitor",
            Visible = true
        };

        // Load icon from resources
        try
        {
            var iconUri = new Uri("pack://application:,,,/Assets/logo.png");
            var streamInfo = System.Windows.Application.GetResourceStream(iconUri);
            if (streamInfo != null)
            {
                using var bitmap = new Bitmap(streamInfo.Stream);
                _notifyIcon.Icon = System.Drawing.Icon.FromHandle(bitmap.GetHicon());
            }
        }
        catch
        {
            // Fallback to default icon
            _notifyIcon.Icon = SystemIcons.Application;
        }

        // Create context menu
        var contextMenu = new Forms.ContextMenuStrip();

        var showItem = new Forms.ToolStripMenuItem("Show Window");
        showItem.Click += (s, e) => ShowWindow();
        contextMenu.Items.Add(showItem);

        var pauseItem = new Forms.ToolStripMenuItem("Pause Recording");
        pauseItem.Click += (s, e) => ToggleRecording();
        contextMenu.Items.Add(pauseItem);

        contextMenu.Items.Add(new Forms.ToolStripSeparator());

        var exitItem = new Forms.ToolStripMenuItem("Exit");
        exitItem.Click += (s, e) => ExitApplication();
        contextMenu.Items.Add(exitItem);

        _notifyIcon.ContextMenuStrip = contextMenu;
        _notifyIcon.DoubleClick += (s, e) => ShowWindow();

        // Update context menu when opening
        contextMenu.Opening += (s, e) =>
        {
            pauseItem.Text = _isRecording ? "Pause Recording" : "Resume Recording";
        };
    }

    private void ShowWindow()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void ToggleRecording()
    {
        Dispatcher.Invoke(() => PauseResumeButton_Click(this, new RoutedEventArgs()));
    }

    private void ExitApplication()
    {
        _isExiting = true;
        Close();
    }

    private void MainWindow_StateChanged(object? sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            Hide();
            UpdateTrayIconText();
        }
    }

    private void UpdateTrayIconText()
    {
        if (_notifyIcon != null)
        {
            var status = _isRecording ? "Recording" : "Paused";
            _notifyIcon.Text = $"KeyRecorder - {status}";
        }
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var databasePath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "KeyRecorder");

            _databaseManager = new DatabaseManager(databasePath);
            await _databaseManager.InitializeAsync();

            // Initialize keyboard hook in the UI (user session)
            _keyboardHook = new KeyboardHook();
            _keyboardHook.KeystrokeCaptured += OnKeystrokeCaptured;

            await ConnectToServiceAsync();

            // Start keyboard capture
            _keyboardHook.Start();
            _isRecording = true;
            PauseResumeButton.Content = "Pause";
            UpdateTrayIconText();

            // Check if we should start minimized (e.g., from Windows startup)
            if (App.StartMinimized)
            {
                WindowState = WindowState.Minimized;
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error initializing: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void OnKeystrokeCaptured(object? sender, KeystrokeEvent e)
    {
        try
        {
            // Record locally
            if (_databaseManager != null)
            {
                await _databaseManager.RecordKeystrokeAsync(e);
            }

            // Also notify service (optional - for redundancy)
            if (_ipcClient != null)
            {
                try
                {
                    var message = new IpcMessage
                    {
                        Type = IpcMessageType.KeystrokeNotification,
                        Payload = JsonSerializer.Serialize(e)
                    };
                    await _ipcClient.SendMessageAsync(message);
                }
                catch
                {
                    // Ignore IPC errors - local storage is primary
                }
            }
        }
        catch
        {
            // Ignore recording errors to not disrupt capture
        }
    }

    private async Task<bool> ConnectToServiceAsync()
    {
        try
        {
            // Check if service is installed
            if (!ServiceManager.IsServiceInstalled())
            {
                StatusText.Text = "Status: Service Not Installed";
                StatusBarText.Text = "Recording locally (service not installed)";
                _refreshTimer.Start();
                await LoadRecentKeystrokesAsync();
                return false;
            }

            // Check if service is running
            if (!ServiceManager.IsServiceRunning())
            {
                StatusText.Text = "Status: Service Stopped";
                StatusBarText.Text = "Recording locally (starting service...)";

                var (success, message) = ServiceManager.TryStartService();
                if (!success)
                {
                    StatusText.Text = "Status: Service Stopped";
                    StatusBarText.Text = "Recording locally (service failed to start)";
                    _refreshTimer.Start();
                    await LoadRecentKeystrokesAsync();
                    return false;
                }

                await Task.Delay(1000);
            }

            // Try to connect via IPC
            _ipcClient = new IpcClient();
            var connected = await _ipcClient.ConnectAsync();

            if (connected)
            {
                StatusText.Text = "Status: Connected";
                StatusBarText.Text = "Connected to KeyRecorder Service";

                _refreshTimer.Start();
                await LoadRecentKeystrokesAsync();
                return true;
            }
            else
            {
                StatusText.Text = "Status: Recording Locally";
                StatusBarText.Text = "Recording locally (service connection failed)";
                _refreshTimer.Start();
                await LoadRecentKeystrokesAsync();
                return false;
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = "Status: Recording Locally";
            StatusBarText.Text = $"Recording locally: {ex.Message}";
            _refreshTimer.Start();
            await LoadRecentKeystrokesAsync();
            return false;
        }
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!_isExiting)
        {
            // Minimize to tray instead of closing
            e.Cancel = true;
            WindowState = WindowState.Minimized;
            return;
        }

        // Actually closing - cleanup
        _refreshTimer?.Stop();
        _keyboardHook?.Stop();
        _keyboardHook?.Dispose();
        _ipcClient?.Dispose();
        _databaseManager?.Dispose();
        _notifyIcon?.Dispose();
    }

    private async void RefreshTimer_Tick(object? sender, EventArgs e)
    {
        await LoadRecentKeystrokesAsync();
    }

    private async Task LoadRecentKeystrokesAsync()
    {
        try
        {
            if (_databaseManager == null) return;

            var keystrokes = await _databaseManager.GetRecentKeystrokesAsync(500);

            var grouped = GroupKeystrokesByMinute(keystrokes);

            _timelineEntries.Clear();
            foreach (var entry in grouped)
            {
                _timelineEntries.Add(entry);
            }

            var count = await _databaseManager.GetKeystrokeCountAsync();
            TotalKeystrokesText.Text = count.ToString("N0");

            RecordingStatusText.Text = _isRecording ? "Active" : "Paused";
            RecordingStatusText.Foreground = _isRecording ?
                System.Windows.Media.Brushes.Green : System.Windows.Media.Brushes.Red;

            StatusBarText.Text = $"Last updated: {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            StatusBarText.Text = $"Error loading keystrokes: {ex.Message}";
        }
    }

    private List<TimelineEntry> GroupKeystrokesByMinute(List<KeystrokeEvent> keystrokes)
    {
        var entries = new List<TimelineEntry>();

        var grouped = keystrokes
            .Where(k => k.IsKeyDown)
            .GroupBy(k => new DateTime(k.Timestamp.Year, k.Timestamp.Month, k.Timestamp.Day,
                                       k.Timestamp.Hour, k.Timestamp.Minute, 0))
            .OrderByDescending(g => g.Key);

        foreach (var group in grouped)
        {
            var keystrokeText = string.Join(" ", group.Select(k => GetDisplayKey(k)));

            entries.Add(new TimelineEntry
            {
                TimeLabel = group.Key.ToLocalTime().ToString("HH:mm"),
                KeystrokesText = keystrokeText,
                KeystrokeCount = group.Count()
            });
        }

        return entries;
    }

    private string GetDisplayKey(KeystrokeEvent k)
    {
        var modifiers = "";
        if (k.IsCtrlPressed) modifiers += "Ctrl+";
        if (k.IsAltPressed) modifiers += "Alt+";
        if (k.IsShiftPressed) modifiers += "Shift+";
        if (k.IsWinPressed) modifiers += "Win+";

        return modifiers + k.KeyName;
    }

    private async void PauseResumeButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_isRecording)
            {
                // Pause recording
                if (_keyboardHook != null)
                {
                    _keyboardHook.IsPaused = true;
                }
                _isRecording = false;
                PauseResumeButton.Content = "Resume";
                StatusBarText.Text = "Recording paused";

                // Notify service
                if (_ipcClient != null)
                {
                    try
                    {
                        await _ipcClient.SendMessageAsync(new IpcMessage { Type = IpcMessageType.PauseRecording });
                    }
                    catch { }
                }
            }
            else
            {
                // Resume recording
                if (_keyboardHook != null)
                {
                    _keyboardHook.IsPaused = false;
                }
                _isRecording = true;
                PauseResumeButton.Content = "Pause";
                StatusBarText.Text = "Recording resumed";

                // Notify service
                if (_ipcClient != null)
                {
                    try
                    {
                        await _ipcClient.SendMessageAsync(new IpcMessage { Type = IpcMessageType.ResumeRecording });
                    }
                    catch { }
                }
            }

            UpdateTrayIconText();
            await LoadRecentKeystrokesAsync();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await LoadRecentKeystrokesAsync();
    }

    private void AboutButton_Click(object sender, RoutedEventArgs e)
    {
        var aboutWindow = new AboutWindow
        {
            Owner = this
        };
        aboutWindow.ShowDialog();
    }
}

public class TimelineEntry
{
    public string TimeLabel { get; set; } = string.Empty;
    public string KeystrokesText { get; set; } = string.Empty;
    public int KeystrokeCount { get; set; }
}
