using System.IO.Pipes;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace KeyRecorder.Core.IPC;

public class IpcClient : IDisposable
{
    private const string PipeName = "KeyRecorderPipe";
    private readonly ILogger<IpcClient>? _logger;
    private NamedPipeClientStream? _client;
    private StreamWriter? _writer;
    private StreamReader? _reader;
    private bool _disposed;

    public event EventHandler<IpcMessage>? MessageReceived;

    public IpcClient(ILogger<IpcClient>? logger = null)
    {
        _logger = logger;
    }

    public async Task<bool> ConnectAsync(int timeoutMs = 5000)
    {
        try
        {
            _client = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            await _client.ConnectAsync(timeoutMs);

            _writer = new StreamWriter(_client) { AutoFlush = true };
            _reader = new StreamReader(_client);

            _logger?.LogInformation("Connected to IPC server");
            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to connect to IPC server");
            return false;
        }
    }

    public async Task SendMessageAsync(IpcMessage message)
    {
        if (_writer == null || _client?.IsConnected != true)
        {
            throw new InvalidOperationException("Not connected to server");
        }

        var json = JsonSerializer.Serialize(message);
        await _writer.WriteLineAsync(json);
        _logger?.LogDebug("Sent message: {Type}", message.Type);
    }

    public void Disconnect()
    {
        _writer?.Dispose();
        _reader?.Dispose();
        _client?.Dispose();
        _logger?.LogInformation("Disconnected from IPC server");
    }

    public void Dispose()
    {
        if (_disposed) return;
        Disconnect();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
