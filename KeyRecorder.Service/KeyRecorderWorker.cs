using KeyRecorder.Core.Data;
using KeyRecorder.Core.Models;
using KeyRecorder.Core.IPC;
using System.Text.Json;

namespace KeyRecorder.Service;

public class KeyRecorderWorker : BackgroundService
{
    private readonly ILogger<KeyRecorderWorker> _logger;
    private readonly IConfiguration _configuration;
    private DatabaseManager? _databaseManager;
    private AppConfiguration? _appConfig;
    private IpcServer? _ipcServer;
    private Timer? _syncTimer;
    private Timer? _integrityTimer;
    private Timer? _snapshotTimer;
    private DateTime _lastSyncTime;
    private DateTime _lastSnapshotTime;
    private bool _isRecordingPaused;

    public KeyRecorderWorker(ILogger<KeyRecorderWorker> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("KeyRecorder Service starting at: {time}", DateTimeOffset.Now);

            _appConfig = LoadConfiguration();
            _isRecordingPaused = _appConfig.IsRecordingPaused;

            _databaseManager = new DatabaseManager(_appConfig.DatabasePath,
                LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<DatabaseManager>());
            await _databaseManager.InitializeAsync();

            // Note: Keyboard capture is now handled by the UI app running in user session
            // The service handles data storage, sync, and background maintenance jobs

            _ipcServer = new IpcServer(
                LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<IpcServer>());
            _ipcServer.MessageReceived += OnIpcMessageReceived;
            _ipcServer.Start();

            StartBackgroundJobs(_appConfig);

            _logger.LogInformation("KeyRecorder Service started successfully");

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in KeyRecorder Service");
            throw;
        }
    }

    private AppConfiguration LoadConfiguration()
    {
        var config = new AppConfiguration();

        var databasePath = _configuration["DatabasePath"];
        if (!string.IsNullOrEmpty(databasePath))
        {
            config.DatabasePath = databasePath;
        }

        if (int.TryParse(_configuration["SyncIntervalMinutes"], out var syncInterval))
        {
            config.SyncIntervalMinutes = syncInterval;
        }

        if (int.TryParse(_configuration["IntegrityCheckIntervalMinutes"], out var integrityInterval))
        {
            config.IntegrityCheckIntervalMinutes = integrityInterval;
        }

        if (int.TryParse(_configuration["RetentionDays"], out var retentionDays))
        {
            config.RetentionPolicy.RetentionDays = retentionDays;
        }

        _logger.LogInformation("Configuration loaded. Database path: {Path}", config.DatabasePath);
        return config;
    }

    private void StartBackgroundJobs(AppConfiguration config)
    {
        _syncTimer = new Timer(
            async _ => await PerformSyncAsync(),
            null,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(config.SyncIntervalMinutes));

        _integrityTimer = new Timer(
            async _ => await PerformIntegrityCheckAsync(),
            null,
            TimeSpan.FromMinutes(10),
            TimeSpan.FromMinutes(config.IntegrityCheckIntervalMinutes));

        _snapshotTimer = new Timer(
            async _ => await PerformSnapshotAsync(),
            null,
            TimeSpan.FromMinutes(15),
            TimeSpan.FromMinutes(config.SnapshotIntervalMinutes));

        _logger.LogInformation("Background jobs scheduled");
    }

    private async Task PerformSyncAsync()
    {
        try
        {
            _logger.LogInformation("Starting scheduled sync");
            if (_databaseManager != null)
            {
                await _databaseManager.SyncHotToMainAsync();
                _lastSyncTime = DateTime.UtcNow;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during scheduled sync");
        }
    }

    private async Task PerformIntegrityCheckAsync()
    {
        try
        {
            _logger.LogInformation("Starting scheduled integrity check");
            if (_databaseManager != null)
            {
                await _databaseManager.PerformIntegrityCheckAsync();

                if (_appConfig != null)
                {
                    await _databaseManager.ApplyRetentionPolicyAsync(_appConfig.RetentionPolicy);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during scheduled integrity check");
        }
    }

    private async Task PerformSnapshotAsync()
    {
        try
        {
            _logger.LogInformation("Starting scheduled snapshot");
            if (_databaseManager != null && _appConfig != null)
            {
                await _databaseManager.CreateSnapshotAsync();
                await _databaseManager.PruneSnapshotsAsync(_appConfig.MaxSnapshotsToKeep);
                _lastSnapshotTime = DateTime.UtcNow;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during scheduled snapshot");
        }
    }

    private async void OnIpcMessageReceived(object? sender, IpcMessage message)
    {
        try
        {
            switch (message.Type)
            {
                case IpcMessageType.KeystrokeNotification:
                    // Receive keystroke from UI app and store it
                    if (!string.IsNullOrEmpty(message.Payload) && _databaseManager != null)
                    {
                        var keystroke = JsonSerializer.Deserialize<KeystrokeEvent>(message.Payload);
                        if (keystroke != null)
                        {
                            await _databaseManager.RecordKeystrokeAsync(keystroke);
                        }
                    }
                    break;

                case IpcMessageType.PauseRecording:
                    _isRecordingPaused = true;
                    _logger.LogInformation("Recording paused (notified by UI)");
                    break;

                case IpcMessageType.ResumeRecording:
                    _isRecordingPaused = false;
                    _logger.LogInformation("Recording resumed (notified by UI)");
                    break;

                case IpcMessageType.GetStatus:
                    if (_databaseManager != null && _ipcServer != null)
                    {
                        var count = await _databaseManager.GetKeystrokeCountAsync();
                        var status = new StatusResponse
                        {
                            IsRecording = !_isRecordingPaused,
                            TotalKeystrokes = count,
                            LastSyncTime = _lastSyncTime != DateTime.MinValue ? _lastSyncTime : null,
                            LastSnapshotTime = _lastSnapshotTime != DateTime.MinValue ? _lastSnapshotTime : null
                        };
                        _logger.LogInformation("Status requested: {Status}", JsonSerializer.Serialize(status));

                        var response = new IpcMessage
                        {
                            Type = IpcMessageType.GetStatusResponse,
                            Payload = JsonSerializer.Serialize(status)
                        };
                        await _ipcServer.SendResponseAsync(response);
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling IPC message");
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("KeyRecorder Service stopping");

        _syncTimer?.Dispose();
        _integrityTimer?.Dispose();
        _snapshotTimer?.Dispose();

        _ipcServer?.Stop();
        _ipcServer?.Dispose();

        if (_databaseManager != null)
        {
            await _databaseManager.SyncHotToMainAsync();
        }

        _databaseManager?.Dispose();

        await base.StopAsync(stoppingToken);
        _logger.LogInformation("KeyRecorder Service stopped");
    }
}
