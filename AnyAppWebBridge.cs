using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace TheSingularityWorkshop.AnyApp;

/// <summary>
/// Small localhost control plane between a running AnyApp instance and the Workshop web page.
/// The endpoint is configured by the Experience manifest rather than hard-coded into the Experience.
/// </summary>
public sealed class AnyAppWebBridge : IDisposable
{
    private readonly object _gate = new();
    private TcpListener? _listener;
    private CancellationTokenSource? _shutdown;
    private string _state = "starting";
    private bool _hubVisible;
    private bool _splitMoniker;
    private string _browserHalf = "left";
    private int _generation;

    public event Action<string>? CommandReceived;

    public Uri Endpoint { get; private set; } = new("http://127.0.0.1:48156/");

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
                    IPAddress.TryParse(endpoint.Host, out _))
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
        if (_listener is not null)
            return;

        var listener = new TcpListener(IPAddress.Loopback, Endpoint.Port);

        try
        {
            listener.Start();
        }
        catch
        {
            listener.Stop();
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

    private async Task ListenAsync(TcpListener listener, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient client;

            try
            {
                client = await listener.AcceptTcpClientAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }

            _ = Task.Run(() => HandleAsync(client, cancellationToken), cancellationToken);
        }
    }

    private async Task HandleAsync(TcpClient client, CancellationToken cancellationToken)
    {
        using (client)
        await using var stream = client.GetStream();

        try
        {
            var request = await ReadRequestAsync(stream, cancellationToken);
            if (request is null)
                return;

            if (request.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
            {
                await WriteResponseAsync(stream, 204, string.Empty, cancellationToken);
                return;
            }

            if (request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase) &&
                request.Path.Equals("/state", StringComparison.OrdinalIgnoreCase))
            {
                await WriteResponseAsync(
                    stream,
                    200,
                    JsonSerializer.Serialize(Snapshot()),
                    cancellationToken);
                return;
            }

            if (request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase) &&
                request.Path.Equals("/command", StringComparison.OrdinalIgnoreCase))
            {
                var command = JsonSerializer.Deserialize<BridgeCommand>(request.Body);

                switch (command?.Command?.ToLowerInvariant())
                {
                    case "return-hub":
                        SetState("living", false);
                        CommandReceived?.Invoke("return-hub");
                        await WriteResponseAsync(
                            stream,
                            200,
                            JsonSerializer.Serialize(new { accepted = true, command = "return-hub" }),
                            cancellationToken);
                        return;

                    case "split-moniker":
                        SetMonikerSplit(true);
                        CommandReceived?.Invoke("split-moniker");
                        await WriteResponseAsync(
                            stream,
                            200,
                            JsonSerializer.Serialize(new { accepted = true, command = "split-moniker" }),
                            cancellationToken);
                        return;

                    case "whole-moniker":
                        SetMonikerSplit(false);
                        CommandReceived?.Invoke("whole-moniker");
                        await WriteResponseAsync(
                            stream,
                            200,
                            JsonSerializer.Serialize(new { accepted = true, command = "whole-moniker" }),
                            cancellationToken);
                        return;

                    case "flip-moniker":
                        FlipMoniker();
                        CommandReceived?.Invoke("flip-moniker");
                        await WriteResponseAsync(
                            stream,
                            200,
                            JsonSerializer.Serialize(new { accepted = true, command = "flip-moniker" }),
                            cancellationToken);
                        return;
                }

                await WriteResponseAsync(
                    stream,
                    400,
                    JsonSerializer.Serialize(new { accepted = false, error = "Unknown command." }),
                    cancellationToken);
                return;
            }

            await WriteResponseAsync(
                stream,
                404,
                JsonSerializer.Serialize(new { error = "Not found." }),
                cancellationToken);
        }
        catch
        {
            // The bridge is optional presentation infrastructure; a malformed request
            // must never terminate the desktop Experience.
        }
    }

    private static async Task<HttpRequest?> ReadRequestAsync(
        NetworkStream stream,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[16 * 1024];
        var count = await stream.ReadAsync(buffer, cancellationToken);
        if (count == 0)
            return null;

        var requestText = Encoding.UTF8.GetString(buffer, 0, count);
        var separator = requestText.IndexOf("\r\n\r\n", StringComparison.Ordinal);
        var headerText = separator >= 0 ? requestText[..separator] : requestText;
        var body = separator >= 0 ? requestText[(separator + 4)..] : string.Empty;

        var lines = headerText.Split("\r\n");
        if (lines.Length == 0)
            return null;

        var requestLine = lines[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (requestLine.Length < 2)
            return null;

        var contentLength = 0;
        foreach (var line in lines.Skip(1))
        {
            var colon = line.IndexOf(':');
            if (colon < 0)
                continue;

            if (line[..colon].Trim().Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                int.TryParse(line[(colon + 1)..].Trim(), out contentLength);
        }

        var bodyBytes = Encoding.UTF8.GetBytes(body);
        while (bodyBytes.Length < contentLength)
        {
            var read = await stream.ReadAsync(buffer, cancellationToken);
            if (read == 0)
                break;

            body += Encoding.UTF8.GetString(buffer, 0, read);
            bodyBytes = Encoding.UTF8.GetBytes(body);
        }

        return new HttpRequest(requestLine[0], requestLine[1], body);
    }

    private async Task WriteResponseAsync(
        NetworkStream stream,
        int statusCode,
        string body,
        CancellationToken cancellationToken)
    {
        var reason = statusCode switch
        {
            200 => "OK",
            204 => "No Content",
            400 => "Bad Request",
            404 => "Not Found",
            _ => "Internal Server Error"
        };

        var payload = Encoding.UTF8.GetBytes(body);
        var headers =
            $"HTTP/1.1 {statusCode} {reason}\r\n" +
            "Access-Control-Allow-Origin: *\r\n" +
            "Access-Control-Allow-Methods: GET,POST,OPTIONS\r\n" +
            "Access-Control-Allow-Headers: Content-Type\r\n" +
            "Content-Type: application/json; charset=utf-8\r\n" +
            $"Content-Length: {payload.Length}\r\n" +
            "Connection: close\r\n\r\n";

        var headerBytes = Encoding.UTF8.GetBytes(headers);
        await stream.WriteAsync(headerBytes, cancellationToken);
        if (payload.Length != 0)
            await stream.WriteAsync(payload, cancellationToken);
    }

    private object Snapshot()
    {
        lock (_gate)
        {
            return new
            {
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

        _shutdown?.Dispose();
        _shutdown = null;
        _listener = null;
    }

    private sealed record BridgeConfiguration(string? WebBridgeEndpoint);
    private sealed record BridgeCommand(string? Command);
    private sealed record HttpRequest(string Method, string Path, string Body);
}
