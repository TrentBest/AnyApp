using System.Net;
using System.Text;
using System.Text.Json;

namespace TheSingularityWorkshop.AnyApp;

/// <summary>
/// Small localhost control plane between a running AnyApp instance and the Workshop web page.
/// The endpoint is configured by the Experience manifest rather than hard-coded into the Experience.
/// </summary>
public sealed class AnyAppWebBridge : IDisposable
{
    public event Action<string>? CommandReceived;
    private readonly object _gate = new();
    private HttpListener? _listener;
    private CancellationTokenSource? _shutdown;
    private string _state = "starting";
    private bool _hubVisible;
    private int _generation;

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
                    endpoint.Scheme is "http" or "https")
                {
                    Endpoint = endpoint;
                    return;
                }
            }
            catch (FormatException)
            {
                // Ignore malformed optional configuration; the default local endpoint remains usable.
            }
            catch (JsonException)
            {
                // Ignore malformed optional configuration; the default local endpoint remains usable.
            }
        }
    }

    public void Start()
    {
        if (_listener is not null)
            return;

        var listener = new HttpListener();
        listener.Prefixes.Add(Endpoint.AbsoluteUri.EndsWith("/", StringComparison.Ordinal)
            ? Endpoint.AbsoluteUri
            : Endpoint.AbsoluteUri + "/");

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

    private async Task ListenAsync(HttpListener listener, CancellationToken cancellationToken)
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
                break;
            }
            catch (HttpListenerException)
            {
                break;
            }

            _ = Task.Run(() => HandleAsync(context), cancellationToken);
        }
    }

    private async Task HandleAsync(HttpListenerContext context)
    {
        try
        {
            context.Response.Headers["Access-Control-Allow-Origin"] = "*";
            context.Response.Headers["Access-Control-Allow-Methods"] = "GET,POST,OPTIONS";
            context.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type";

            if (context.Request.HttpMethod.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = 204;
                return;
            }

            var path = context.Request.Url?.AbsolutePath.TrimEnd('/') ?? string.Empty;

            if (context.Request.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase) &&
                path.Equals("/state", StringComparison.OrdinalIgnoreCase))
            {
                await WriteJsonAsync(context, Snapshot());
                return;
            }

            if (context.Request.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase) &&
                path.Equals("/command", StringComparison.OrdinalIgnoreCase))
            {
                using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding);
                var body = await reader.ReadToEndAsync();
                var command = JsonSerializer.Deserialize<BridgeCommand>(body);

                if (string.Equals(command?.Command, "return-hub", StringComparison.OrdinalIgnoreCase))
                {
                    SetState("living", false);
                    CommandReceived?.Invoke("return-hub");
                    await WriteJsonAsync(context, new { accepted = true, command = "return-hub" });
                    return;
                }

                context.Response.StatusCode = 400;
                await WriteJsonAsync(context, new { accepted = false, error = "Unknown command." });
                return;
            }

            context.Response.StatusCode = 404;
        }
        catch
        {
            context.Response.StatusCode = 500;
        }
        finally
        {
            context.Response.Close();
        }
    }

    private object Snapshot()
    {
        lock (_gate)
        {
            return new
            {
                state = _state,
                hubVisible = _hubVisible,
                generation = _generation,
                endpoint = Endpoint.AbsoluteUri
            };
        }
    }

    private static async Task WriteJsonAsync(HttpListenerContext context, object value)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value));
        context.Response.ContentLength64 = bytes.Length;
        await context.Response.OutputStream.WriteAsync(bytes);
    }

    public void Dispose()
    {
        _shutdown?.Cancel();

        try { _listener?.Stop(); }
        catch { }

        try { _listener?.Close(); }
        catch { }

        _shutdown?.Dispose();
        _shutdown = null;
        _listener = null;
    }

    private sealed record BridgeConfiguration(string? WebBridgeEndpoint);
    private sealed record BridgeCommand(string? Command);
}
