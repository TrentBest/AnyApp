using System.IO;
using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace TheSingularityWorkshop.AnyApp;

/// <summary>
/// Local presentation bridge between WebApp and AnyApp.
///
/// HTTP state/command requests remain compatible with the Workshop browser
/// manifestation, while privileged requests require a per-launch token and
/// an allowed browser Origin. WebSocket sessions use the same boundary.
/// </summary>
public sealed class AnyAppWebBridge : IDisposable
{
    public const int ProtocolVersion = 1;

    private readonly object _gate = new();
    private readonly ConcurrentDictionary<Guid, WebSocket> _clients = new();
    private HttpListener? _listener;
    private CancellationTokenSource? _shutdown;
    private string? _launchToken;
    private string _state = "starting";
    private bool _hubVisible;
    private bool _splitMoniker;
    private string _browserHalf = "left";
    private int _generation;

    public event Action<string>? CommandReceived;

    public Uri Endpoint { get; private set; } = new("http://127.0.0.1:48156/");

    public bool SplitMoniker
    {
        get { lock (_gate) return _splitMoniker; }
    }

    public string BrowserHalf
    {
        get { lock (_gate) return _browserHalf; }
    }

    public string DesktopHalf
    {
        get
        {
            lock (_gate)
                return _browserHalf.Equals("left", StringComparison.OrdinalIgnoreCase)
                    ? "right"
                    : "left";
        }
    }

    public void SetLaunchToken(string? launchToken)
    {
        if (string.IsNullOrWhiteSpace(launchToken))
        {
            _launchToken = null;
            return;
        }

        _launchToken = launchToken;
    }

    public void Configure(ExperienceManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        foreach (var request in manifest.Bundles)
        {
            if (string.IsNullOrWhiteSpace(request.ConfigurationBase64))
                continue;

            try
            {
                var bytes = Convert.FromBase64String(request.ConfigurationBase64);
                var configuration = JsonSerializer.Deserialize<BridgeConfiguration>(bytes);

                if (configuration?.WebBridgeEndpoint is not null &&
                    Uri.TryCreate(configuration.WebBridgeEndpoint, UriKind.Absolute, out var endpoint) &&
                    endpoint.Scheme == Uri.UriSchemeHttp &&
                    IPAddress.TryParse(endpoint.Host, out var address) &&
                    IPAddress.IsLoopback(address))
                {
                    Endpoint = endpoint;
                    return;
                }
            }
            catch (FormatException)
            {
            }
            catch (JsonException)
            {
            }
        }
    }

    public void Start()
    {
        if (_listener is not null || string.IsNullOrWhiteSpace(_launchToken))
            return;

        var listener = new HttpListener();
        listener.Prefixes.Add(
            $"http://{Endpoint.Host}:{Endpoint.Port}/");

        try
        {
            listener.Start();
        }
        catch
        {
            listener.Close();
            return;
        }

        _listener = listener;
        _shutdown = new CancellationTokenSource();
        _ = ListenAsync(listener, _shutdown.Token);
    }

    public void SetState(string state, bool hubVisible)
    {
        lock (_gate)
        {
            _state = state;
            _hubVisible = hubVisible;
            _generation++;
        }
    }

    public void SetMonikerSplit(bool enabled)
    {
        lock (_gate)
        {
            _splitMoniker = enabled;
            _generation++;
        }
    }

    public void FlipMoniker()
    {
        lock (_gate)
        {
            _browserHalf = _browserHalf.Equals("left", StringComparison.OrdinalIgnoreCase)
                ? "right"
                : "left";
            _generation++;
        }
    }

    private async Task ListenAsync(
        HttpListener listener,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            HttpListenerContext context;

            try
            {
                context = await listener.GetContextAsync();
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
        try
        {
            if (!IsAllowedOrigin(context.Request.Headers["Origin"]) ||
                !IsValidToken(context.Request.QueryString["token"]))
            {
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                context.Response.Close();
                return;
            }

            if (context.Request.IsWebSocketRequest)
            {
                await HandleWebSocketAsync(context, cancellationToken);
                return;
            }

            if (context.Request.HttpMethod.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
            {
                WriteHeaders(context.Response);
                context.Response.StatusCode = (int)HttpStatusCode.NoContent;
                context.Response.Close();
                return;
            }

            if (context.Request.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase) &&
                context.Request.Url?.AbsolutePath.Equals("/state", StringComparison.OrdinalIgnoreCase) == true)
            {
                await WriteJsonAsync(context, HttpStatusCode.OK, Snapshot(), cancellationToken);
                return;
            }

            if (context.Request.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase) &&
                context.Request.Url?.AbsolutePath.Equals("/command", StringComparison.OrdinalIgnoreCase) == true)
            {
                using var reader = new StreamReader(
                    context.Request.InputStream,
                    context.Request.ContentEncoding ?? Encoding.UTF8);

                var body = await reader.ReadToEndAsync(cancellationToken);
                var command = JsonSerializer.Deserialize<BridgeCommand>(body);

                if (!TryExecuteCommand(command?.Command))
                {
                    await WriteJsonAsync(
                        context,
                        HttpStatusCode.BadRequest,
                        new { accepted = false, error = "Unknown command." },
                        cancellationToken);
                    return;
                }

                await WriteJsonAsync(
                    context,
                    HttpStatusCode.OK,
                    new { accepted = true, command = command!.Command },
                    cancellationToken);
                return;
            }

            await WriteJsonAsync(
                context,
                HttpStatusCode.NotFound,
                new { error = "Not found." },
                cancellationToken);
        }
        catch
        {
            try
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.Close();
            }
            catch
            {
                // The bridge is optional presentation infrastructure.
            }
        }
    }

    private async Task HandleWebSocketAsync(
        HttpListenerContext context,
        CancellationToken cancellationToken)
    {
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
                        sessionId = id,
                        host = "AnyApp"
                    }),
                cancellationToken);

            await ReceiveLoopAsync(id, socket, cancellationToken);
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
                        new { sessionId, protocol = ProtocolVersion }),
                    cancellationToken);
            }
            else if (string.Equals(type, "heartbeat", StringComparison.Ordinal))
            {
                await SendAsync(
                    socket,
                    CreateEnvelope(
                        "heartbeat.ack",
                        new { sessionId, utc = DateTimeOffset.UtcNow }),
                    cancellationToken);
            }
            else if (string.Equals(type, "command", StringComparison.Ordinal) &&
                     root.TryGetProperty("payload", out var payload) &&
                     payload.TryGetProperty("command", out var commandElement) &&
                     TryExecuteCommand(commandElement.GetString()))
            {
                await SendAsync(
                    socket,
                    CreateEnvelope(
                        "command.ack",
                        new { sessionId }),
                    cancellationToken);
            }
            else if (string.Equals(type, "event", StringComparison.Ordinal))
            {
                await SendAsync(
                    socket,
                    CreateEnvelope(
                        "event.ack",
                        new { sessionId }),
                    cancellationToken);
            }
        }
    }

    private bool TryExecuteCommand(string? command)
    {
        switch (command?.ToLowerInvariant())
        {
            case "return-hub":
                SetState("living", false);
                CommandReceived?.Invoke("return-hub");
                return true;

            case "split-moniker":
                SetMonikerSplit(true);
                CommandReceived?.Invoke("split-moniker");
                return true;

            case "whole-moniker":
                SetMonikerSplit(false);
                CommandReceived?.Invoke("whole-moniker");
                return true;

            case "flip-moniker":
                FlipMoniker();
                CommandReceived?.Invoke("flip-moniker");
                return true;

            default:
                return false;
        }
    }

    private async Task WriteJsonAsync(
        HttpListenerContext context,
        HttpStatusCode status,
        object payload,
        CancellationToken cancellationToken)
    {
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));

        WriteHeaders(context.Response);
        context.Response.StatusCode = (int)status;
        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.ContentLength64 = bytes.Length;
        await context.Response.OutputStream.WriteAsync(bytes, cancellationToken);
        context.Response.Close();
    }

    private static void WriteHeaders(HttpListenerResponse response)
    {
        response.Headers["Access-Control-Allow-Methods"] = "GET,POST,OPTIONS";
        response.Headers["Access-Control-Allow-Headers"] = "Content-Type";
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

    private bool IsValidToken(string? token)
    {
        var expected = _launchToken;
        return expected is not null &&
               token is not null &&
               FixedEquals(token, expected);
    }

    private static bool FixedEquals(string left, string right)
    {
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

        var configured = Environment.GetEnvironmentVariable("ANYAPP_ALLOWED_ORIGINS");

        if (string.IsNullOrWhiteSpace(configured))
        {
            return string.Equals(
                uri.AbsoluteUri.TrimEnd('/'),
                "https://lemon-ground-09f542010.1.azurestaticapps.net",
                StringComparison.OrdinalIgnoreCase);
        }

        return configured
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(value => string.Equals(
                value.TrimEnd('/'),
                uri.AbsoluteUri.TrimEnd('/'),
                StringComparison.OrdinalIgnoreCase));
    }

    private object Snapshot()
    {
        lock (_gate)
        {
            return new
            {
                protocol = ProtocolVersion,
                state = _state,
                hubVisible = _hubVisible,
                splitMoniker = _splitMoniker,
                browserHalf = _browserHalf,
                generation = _generation,
                endpoint = Endpoint.AbsoluteUri
            };
        }
    }

    public void Dispose()
    {
        _shutdown?.Cancel();

        try { _listener?.Stop(); }
        catch { }

        foreach (var client in _clients.Values)
        {
            try { client.Dispose(); }
            catch { }
        }

        _clients.Clear();
        _shutdown?.Dispose();
        _shutdown = null;
        _listener = null;
    }

    private sealed record BridgeConfiguration(string? WebBridgeEndpoint);
    private sealed record BridgeCommand(string? Command);
}