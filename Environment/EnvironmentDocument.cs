namespace TheSingularityWorkshop.AnyApp.Environments;

/// <summary>
/// Versioned, declarative environment composition. This document contains authored intent,
/// not executable behavior or authoritative physical state.
/// </summary>
public sealed record EnvironmentDocument
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;
    public string Name { get; init; } = "Untitled environment";
    public IReadOnlyList<EnvironmentObject> Objects { get; init; } = Array.Empty<EnvironmentObject>();
    public IReadOnlyList<EnvironmentConnection> Connections { get; init; } = Array.Empty<EnvironmentConnection>();
}

/// <summary>A placed visual/world object. Id is independent of position and appearance.</summary>
public sealed record EnvironmentObject
{
    public string Id { get; init; } = string.Empty;
    public string Kind { get; init; } = string.Empty;
    public double X { get; init; }
    public double Y { get; init; }
    public double Width { get; init; } = 1;
    public double Height { get; init; } = 1;
    public string Appearance { get; init; } = "default";
    public IReadOnlyList<EnvironmentCapabilityBinding> Capabilities { get; init; } =
        Array.Empty<EnvironmentCapabilityBinding>();
}

/// <summary>
/// A reference to a separately registered capability. Binding a capability does not itself
/// grant permission to invoke it; the host validates and authorizes every operation.
/// </summary>
public sealed record EnvironmentCapabilityBinding
{
    public string CapabilityId { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
}

/// <summary>A typed connection between two stable object identities.</summary>
public sealed record EnvironmentConnection
{
    public string Id { get; init; } = string.Empty;
    public string SourceObjectId { get; init; } = string.Empty;
    public string TargetObjectId { get; init; } = string.Empty;
    public string ContractId { get; init; } = string.Empty;
    public string ContractVersion { get; init; } = string.Empty;
}

public enum EnvironmentDiagnosticSeverity
{
    Warning,
    Error
}

public sealed record EnvironmentDiagnostic(
    string Code,
    EnvironmentDiagnosticSeverity Severity,
    string Message,
    string? SubjectId = null);

public sealed record EnvironmentValidationResult(IReadOnlyList<EnvironmentDiagnostic> Diagnostics)
{
    public bool IsValid => Diagnostics.All(item => item.Severity != EnvironmentDiagnosticSeverity.Error);
}

/// <summary>Structural validation only; capability existence/compatibility is resolved by the host registry.</summary>
public static class EnvironmentDocumentValidator
{
    private static readonly HashSet<string> SupportedKinds = new(StringComparer.Ordinal)
    {
        "floor",
        "platform",
        "cube",
        "ball",
        "hoop",
        "crane-placeholder"
    };

    public static EnvironmentValidationResult Validate(EnvironmentDocument? document)
    {
        var diagnostics = new List<EnvironmentDiagnostic>();
        if (document is null)
        {
            diagnostics.Add(Error("environment.null", "The environment document is missing."));
            return new EnvironmentValidationResult(diagnostics);
        }

        if (document.SchemaVersion != EnvironmentDocument.CurrentSchemaVersion)
            diagnostics.Add(Error("environment.schema.unsupported",
                $"Schema version {document.SchemaVersion} is not supported."));

        if (string.IsNullOrWhiteSpace(document.Name))
            diagnostics.Add(Error("environment.name.empty", "The environment name must not be empty."));

        if (document.Objects is null)
        {
            diagnostics.Add(Error("environment.objects.null", "The object collection must not be null."));
            return new EnvironmentValidationResult(diagnostics);
        }

        var objectIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in document.Objects)
        {
            if (item is null)
            {
                diagnostics.Add(Error("environment.object.null", "The object collection contains a null entry."));
                continue;
            }

            if (string.IsNullOrWhiteSpace(item.Id))
                diagnostics.Add(Error("environment.object.id.empty", "Every object must have a stable, non-empty ID."));
            else if (!objectIds.Add(item.Id))
                diagnostics.Add(Error("environment.object.id.duplicate", $"Object ID '{item.Id}' is duplicated.", item.Id));

            if (!SupportedKinds.Contains(item.Kind ?? string.Empty))
                diagnostics.Add(Error("environment.object.kind.unsupported",
                    $"Object kind '{item.Kind}' is not supported.", item.Id));

            if (!double.IsFinite(item.X) || !double.IsFinite(item.Y))
                diagnostics.Add(Error("environment.object.position.nonfinite",
                    "Object position must contain finite coordinates.", item.Id));

            if (!double.IsFinite(item.Width) || !double.IsFinite(item.Height) ||
                item.Width <= 0 || item.Height <= 0)
                diagnostics.Add(Error("environment.object.size.invalid",
                    "Object width and height must be finite and greater than zero.", item.Id));

            if (string.IsNullOrWhiteSpace(item.Appearance))
                diagnostics.Add(Error("environment.object.appearance.empty",
                    "Object appearance must be explicitly named.", item.Id));

            if (item.Capabilities is null)
            {
                diagnostics.Add(Error("environment.object.capabilities.null",
                    "The capability collection must not be null.", item.Id));
                continue;
            }

            var capabilityKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var capability in item.Capabilities)
            {
                if (capability is null || string.IsNullOrWhiteSpace(capability.CapabilityId) ||
                    string.IsNullOrWhiteSpace(capability.Version))
                {
                    diagnostics.Add(Error("environment.capability.binding.invalid",
                        "Capability bindings require a capability ID and version.", item.Id));
                    continue;
                }

                var key = capability.CapabilityId + "@" + capability.Version;
                if (!capabilityKeys.Add(key))
                    diagnostics.Add(Error("environment.capability.binding.duplicate",
                        $"Capability binding '{key}' is duplicated.", item.Id));
            }
        }

        if (document.Connections is null)
        {
            diagnostics.Add(Error("environment.connections.null", "The connection collection must not be null."));
            return new EnvironmentValidationResult(diagnostics);
        }

        var connectionIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var connection in document.Connections)
        {
            if (connection is null)
            {
                diagnostics.Add(Error("environment.connection.null", "The connection collection contains a null entry."));
                continue;
            }

            if (string.IsNullOrWhiteSpace(connection.Id))
                diagnostics.Add(Error("environment.connection.id.empty", "Every connection must have a stable, non-empty ID."));
            else if (!connectionIds.Add(connection.Id))
                diagnostics.Add(Error("environment.connection.id.duplicate",
                    $"Connection ID '{connection.Id}' is duplicated.", connection.Id));

            if (string.IsNullOrWhiteSpace(connection.SourceObjectId) ||
                !objectIds.Contains(connection.SourceObjectId))
                diagnostics.Add(Error("environment.connection.source.missing",
                    $"Source object '{connection.SourceObjectId}' does not exist.", connection.Id));

            if (string.IsNullOrWhiteSpace(connection.TargetObjectId) ||
                !objectIds.Contains(connection.TargetObjectId))
                diagnostics.Add(Error("environment.connection.target.missing",
                    $"Target object '{connection.TargetObjectId}' does not exist.", connection.Id));

            if (!string.IsNullOrWhiteSpace(connection.SourceObjectId) &&
                string.Equals(connection.SourceObjectId, connection.TargetObjectId, StringComparison.Ordinal))
                diagnostics.Add(Error("environment.connection.self",
                    "A connection cannot target the same object that it originates from.", connection.Id));

            if (string.IsNullOrWhiteSpace(connection.ContractId) ||
                string.IsNullOrWhiteSpace(connection.ContractVersion))
                diagnostics.Add(Error("environment.connection.contract.missing",
                    "Connections require an explicit contract ID and version.", connection.Id));
        }

        return new EnvironmentValidationResult(diagnostics);
    }

    private static EnvironmentDiagnostic Error(string code, string message, string? subjectId = null) =>
        new(code, EnvironmentDiagnosticSeverity.Error, message, subjectId);
}
