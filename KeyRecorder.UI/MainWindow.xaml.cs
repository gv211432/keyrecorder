using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using KeyRecorder.Core.IPC;
using KeyRecorder.Core.Data;
using KeyRecorder.Core.Models;

namespace KeyRecorder.UI;

public partial class MainWindow : Window
{
    private IpcClient? _ipcClient;
    private DatabaseManager? _databaseManager;
    private readonly DispatcherTimer _refreshTimer;
    private readonly ObservableCollection<TimelineEntry> _timelineEntries;
    private bool _isRecording;

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

            await ConnectToServiceAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error initializing: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                StatusBarText.Text = "KeyRecorder Service is not installed";
                var result = MessageBox.Show(
                    "KeyRecorder Service is not installed.\n\nPlease run the KeyRecorder installer to install the service.",
                    "Service Not Installed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }

            // Check if service is running
            if (!ServiceManager.IsServiceRunning())
            {
                StatusText.Text = "Status: Service Stopped";
                StatusBarText.Text = "Attempting to start KeyRecorder Service...";

                var result = MessageBox.Show(
                    "KeyRecorder Service is not running.\n\nWould you like to start it now?\n\n(You may be prompted for administrator privileges)",
                    "Service Not Running",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    var (success, message) = ServiceManager.TryStartService();
                    if (!success)
                    {
                        StatusText.Text = "Status: Failed to Start Service";
                        StatusBarText.Text = message;
                        MessageBox.Show(message, "Service Start Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }

                    // Wait a moment for service to fully initialize
                    await Task.Delay(1000);
                }
                else
                {
                    return false;
                }
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
                StatusText.Text = "Status: Connection Failed";
                StatusBarText.Text = "Unable to connect to service";

                var retryResult = MessageBox.Show(
                    "Unable to connect to KeyRecorder service via IPC.\n\nThe service may be starting up. Would you like to retry?",
                    "Connection Error",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (retryResult == MessageBoxResult.Yes)
                {
                    await Task.Delay(2000);
                    return await ConnectToServiceAsync();
                }

                return false;
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = "Status: Error";
            StatusBarText.Text = $"Error: {ex.Message}";
            MessageBox.Show($"Error connecting to service: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _refreshTimer?.Stop();
        _ipcClient?.Dispose();
        _databaseManager?.Dispose();
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
            if (_ipcClient == null) return;

            if (_isRecording)
            {
                await _ipcClient.SendMessageAsync(new IpcMessage { Type = IpcMessageType.PauseRecording });
                _isRecording = false;
                PauseResumeButton.Content = "Resume";
                StatusBarText.Text = "Recording paused";
            }
            else
            {
                await _ipcClient.SendMessageAsync(new IpcMessage { Type = IpcMessageType.ResumeRecording });
                _isRecording = true;
                PauseResumeButton.Content = "Pause";
                StatusBarText.Text = "Recording resumed";
            }

            await LoadRecentKeystrokesAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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