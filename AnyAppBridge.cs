using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace TheSingularityWorkshop.AnyApp;

public sealed class AnyAppBridge : IAsyncDisposable
{
    public const int ProtocolVersion = 1;
    public const int Port = 47631;

    private readonly HttpListener _listener = new();
    private readonly string _launchToken;
    private readonly ConcurrentDictionary<Guid, WebSocket> _clients = new();
    private CancellationTokenSource? _shutdown;

    public AnyAppBridge(string launchToken)
    {
        if (string.IsNullOrWhiteSpace(launchToken))
            throw new ArgumentException("A launch token is required.", nameof(launchToken));

        _launchToken = launchToken;
        _listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
    }

    public Uri Endpoint =>
        new($"ws://127.0.0.1:{Port}/bridge?token={Uri.EscapeDataString(_launchToken)}");

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_shutdown is not null)
            return Task.CompletedTask;

        _shutdown = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _listener.Start();
        _ = AcceptLoopAsync(_shutdown.Token);
        return Task.CompletedTask;
    }

    public async Task PublishExperienceStateAsync(
        ExperienceManifest manifest,
        string lifecycleState,
        CancellationToken cancellationToken = default)
    {
        var message = CreateEnvelope(
            "experience.state",
            new
            {
                experienceId = manifest.ExperienceId,
                version = manifest.Version,
                lifecycleState
            });

        await BroadcastAsync(message, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_shutdown is null)
            return;

        _shutdown.Cancel();

        try
        {
            _listener.Stop();
            _listener.Close();
        }
        catch
        {
            // Shutdown is best effort.
        }

        foreach (var client in _clients.Values)
        {
            try
            {
                await client.CloseAsync(
                    WebSocketCloseStatus.NormalClosure,
                    "AnyApp shutting down",
                    CancellationToken.None);
            }
            catch
            {
                // Client may already be disconnected.
            }

            client.Dispose();
        }

        _clients.Clear();
        _shutdown.Dispose();
        _shutdown = null;
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            HttpListenerContext context;

            try
            {
                context = await _listener.GetContextAsync();
            }
            catch when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch
            {
                continue;
            }

            _ = HandleAsync(context, cancellationToken);
        }
    }

    private async Task HandleAsync(
        HttpListenerContext context,
        CancellationToken cancellationToken)
    {
        var origin = context.Request.Headers["Origin"];

        if (!IsAllowedOrigin(origin) ||
            !FixedEquals(
                context.Request.QueryString["token"],
                _launchToken))
        {
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            context.Response.Close();
            return;
        }

        if (!context.Request.IsWebSocketRequest)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.Close();
            return;
        }

        HttpListenerWebSocketContext socketContext;

        try
        {
            socketContext = await context.AcceptWebSocketAsync(
                subProtocol: null,
                receiveBufferSize: 16 * 1024,
                keepAliveInterval: TimeSpan.FromSeconds(30));
        }
        catch
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.Close();
            return;
        }

        var id = Guid.NewGuid();
        var socket = socketContext.WebSocket;
        _clients[id] = socket;

        try
        {
            await SendAsync(
                socket,
                CreateEnvelope(
                    "welcome",
                    new
                    {
                        protocol = ProtocolVersion,
                        sessionId = id,
                        host = "AnyApp"
                    }),
                cancellationToken);

            await ReceiveLoopAsync(id, socket, cancellationToken);
        }
        catch
        {
            // Connection failure is represented by the disconnect event.
        }
        finally
        {
            _clients.TryRemove(id, out _);

            try
            {
                if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
                    await socket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Session ended",
                        CancellationToken.None);
            }
            catch
            {
                // Client may already be gone.
            }

            socket.Dispose();
        }
    }

    private async Task ReceiveLoopAsync(
        Guid sessionId,
        WebSocket socket,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[64 * 1024];

        while (socket.State == WebSocketState.Open &&
               !cancellationToken.IsCancellationRequested)
        {
            using var message = new MemoryStream();
            WebSocketReceiveResult result;

            do
            {
                result = await socket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                    return;

                if (result.MessageType != WebSocketMessageType.Text)
                    throw new InvalidOperationException(
                        "The AnyApp bridge accepts JSON text messages only.");

                if (message.Length + result.Count > buffer.Length)
                    throw new InvalidOperationException(
                        "The AnyApp bridge message exceeds the 64 KiB limit.");

                message.Write(buffer, 0, result.Count);
            }
            while (!result.EndOfMessage);

            using var document = JsonDocument.Parse(message.ToArray());
            var root = document.RootElement;

            if (!root.TryGetProperty("protocol", out var protocol) ||
                protocol.GetInt32() != ProtocolVersion)
            {
                await SendAsync(
                    socket,
                    CreateEnvelope(
                        "error",
                        new
                        {
                            code = "protocol-version",
                            message = $"Unsupported bridge protocol. Expected {ProtocolVersion}."
                        }),
                    cancellationToken);

                continue;
            }

            var type = root.TryGetProperty("type", out var typeElement)
                ? typeElement.GetString()
                : null;

            if (string.Equals(type, "hello", StringComparison.Ordinal))
            {
                await SendAsync(
                    socket,
                    CreateEnvelope(
                        "hello.ack",
                        new
                        {
                            sessionId,
                            protocol = ProtocolVersion
                        }),
                    cancellationToken);
            }
            else if (string.Equals(type, "heartbeat", StringComparison.Ordinal))
            {
                await SendAsync(
                    socket,
                    CreateEnvelope(
                        "heartbeat.ack",
                        new
                        {
                            sessionId,
                            utc = DateTimeOffset.UtcNow
                        }),
                    cancellationToken);
            }
            else if (string.Equals(type, "event", StringComparison.Ordinal))
            {
                // Events are deliberately acknowledged but not interpreted by the
                // transport layer. Experience-owned semantics belong above the bridge.
                await SendAsync(
                    socket,
                    CreateEnvelope(
                        "event.ack",
                        new
                        {
                            sessionId
                        }),
                    cancellationToken);
            }
        }
    }

    private async Task BroadcastAsync(
        string message,
        CancellationToken cancellationToken)
    {
        foreach (var pair in _clients.ToArray())
        {
            if (pair.Value.State != WebSocketState.Open)
                continue;

            try
            {
                await SendAsync(pair.Value, message, cancellationToken);
            }
            catch
            {
                _clients.TryRemove(pair.Key, out _);
            }
        }
    }

    private static async Task SendAsync(
        WebSocket socket,
        string message,
        CancellationToken cancellationToken)
    {
        var bytes = Encoding.UTF8.GetBytes(message);

        await socket.SendAsync(
            new ArraySegment<byte>(bytes),
            WebSocketMessageType.Text,
            endOfMessage: true,
            cancellationToken);
    }

    private static string CreateEnvelope(string type, object payload) =>
        JsonSerializer.Serialize(
            new
            {
                protocol = ProtocolVersion,
                type,
                sequence = 0,
                timestampUtc = DateTimeOffset.UtcNow,
                payload
            });

    private static bool FixedEquals(string? left, string right)
    {
        if (left is null)
            return false;

        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);

        return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    private static bool IsAllowedOrigin(string? origin)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
            return false;

        if (uri.IsLoopback)
            return true;

        var configured = Environment.GetEnvironmentVariable(
            "ANYAPP_ALLOWED_ORIGINS");

        if (string.IsNullOrWhiteSpace(configured))
            return string.Equals(
                uri.AbsoluteUri.TrimEnd('/'),
                "https://lemon-ground-09f542010.1.azurestaticapps.net",
                StringComparison.OrdinalIgnoreCase);

        return configured
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(value => string.Equals(
                value.TrimEnd('/'),
                uri.AbsoluteUri.TrimEnd('/'),
                StringComparison.OrdinalIgnoreCase));
    }
}
