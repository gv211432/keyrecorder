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

    public async Task<IpcMessage?> SendMessageAndWaitForResponseAsync(IpcMessage message, int timeoutMs = 5000)
    {
        if (_writer == null || _reader == null || _client?.IsConnected != true)
        {
            throw new InvalidOperationException("Not connected to server");
        }

        var json = JsonSerializer.Serialize(message);
        await _writer.WriteLineAsync(json);
        _logger?.LogDebug("Sent message: {Type}", message.Type);

        using var cts = new CancellationTokenSource(timeoutMs);
        try
        {
            var responseJson = await _reader.ReadLineAsync(cts.Token);
            if (string.IsNullOrEmpty(responseJson))
                return null;

            var response = JsonSerializer.Deserialize<IpcMessage>(responseJson);
            _logger?.LogDebug("Received response: {Type}", response?.Type);
            return response;
        }
        catch (OperationCanceledException)
        {
            _logger?.LogWarning("Response timeout for message: {Type}", message.Type);
            return null;
        }
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
