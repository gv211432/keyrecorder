using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace KeyRecorder.Core.IPC;

public class IpcServer : IDisposable
{
    private const string PipeName = "KeyRecorderPipe";
    private readonly ILogger<IpcServer>? _logger;
    private CancellationTokenSource? _cts;
    private Task? _serverTask;
    private bool _disposed;

    public event EventHandler<IpcMessage>? MessageReceived;

    public IpcServer(ILogger<IpcServer>? logger = null)
    {
        _logger = logger;
    }

    public void Start()
    {
        if (_serverTask != null)
        {
            _logger?.LogWarning("IPC Server already started");
            return;
        }

        _cts = new CancellationTokenSource();
        _serverTask = Task.Run(() => RunServerAsync(_cts.Token));
        _logger?.LogInformation("IPC Server started");
    }

    private async Task RunServerAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var pipeSecurity = new PipeSecurity();

                // Allow Everyone to access the pipe
                var everyoneSid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
                pipeSecurity.AddAccessRule(new PipeAccessRule(everyoneSid, PipeAccessRights.ReadWrite, AccessControlType.Allow));

                // Allow LocalSystem (service account)
                var localSystemSid = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
                pipeSecurity.AddAccessRule(new PipeAccessRule(localSystemSid, PipeAccessRights.FullControl, AccessControlType.Allow));

                using var server = NamedPipeServerStreamAcl.Create(
                    PipeName,
                    PipeDirection.InOut,
                    NamedPipeServerStream.MaxAllowedServerInstances,
                    PipeTransmissionMode.Message,
                    PipeOptions.Asynchronous,
                    4096,
                    4096,
                    pipeSecurity);

                _logger?.LogInformation("Waiting for client connection...");
                await server.WaitForConnectionAsync(cancellationToken);
                _logger?.LogInformation("Client connected");

                await HandleClientAsync(server, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in IPC server");
                await Task.Delay(1000, cancellationToken);
            }
        }
    }

    private async Task HandleClientAsync(NamedPipeServerStream server, CancellationToken cancellationToken)
    {
        try
        {
            using var reader = new StreamReader(server);
            using var writer = new StreamWriter(server) { AutoFlush = true };

            while (server.IsConnected && !cancellationToken.IsCancellationRequested)
            {
                var messageJson = await reader.ReadLineAsync(cancellationToken);
                if (string.IsNullOrEmpty(messageJson))
                    break;

                var message = JsonSerializer.Deserialize<IpcMessage>(messageJson);
                if (message != null)
                {
                    _logger?.LogDebug("Received message: {Type}", message.Type);
                    MessageReceived?.Invoke(this, message);
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error handling client");
        }
    }

    public void Stop()
    {
        _cts?.Cancel();
        _serverTask?.Wait(TimeSpan.FromSeconds(5));
        _logger?.LogInformation("IPC Server stopped");
    }

    public void Dispose()
    {
        if (_disposed) return;
        Stop();
        _cts?.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
