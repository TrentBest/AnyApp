using System.Buffers.Binary;
using System.Reflection;
using TheSingularityWorkshop.FSM_COS;
using TheSingularityWorkshop.MicroBundleDomain;
using TheSingularityWorkshop.MicroBundleRepository.Core;

namespace TheSingularityWorkshop.AnyApp;

/// <summary>
/// Materializes a verified repository artifact into the runtime MicroBundle
/// understood by the host composition boundary.
/// </summary>
public interface IMicroBundleArtifactMaterializer
{
    IMicroBundle Materialize(MicroBundleArtifact artifact);
}

/// <summary>
/// Loads verified .NET assembly bytes and discovers their public parameterless
/// MicroBundle by the identity carried by the repository artifact address.
/// Accepts raw assembly bytes and the Workshop FSMB v1 assembly envelope.
/// </summary>
public sealed class AssemblyMicroBundleArtifactMaterializer : IMicroBundleArtifactMaterializer
{
    private static ReadOnlySpan<byte> EnvelopeMagic => "FSMB"u8;
    private const int EnvelopeHeaderLength = 20;
    private const uint EnvelopeFormatVersion = 1;

    public IMicroBundle Materialize(MicroBundleArtifact artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);

        var assemblyBytes = GetAssemblyBytes(artifact);
        if (assemblyBytes.IsEmpty)
            throw new InvalidOperationException(
                $"MicroBundle artifact {artifact.Address} contains no assembly bytes.");

        var assembly = Assembly.Load(assemblyBytes.ToArray());
        var candidates = assembly
            .GetTypes()
            .Where(type =>
                !type.IsAbstract &&
                !type.IsInterface &&
                typeof(IMicroBundle).IsAssignableFrom(type) &&
                type.GetConstructor(Type.EmptyTypes) is not null)
            .Select(type => Activator.CreateInstance(type))
            .OfType<IMicroBundle>()
            .Where(bundle => bundle.Id == artifact.Address.BundleId)
            .ToArray();

        return candidates.Length switch
        {
            1 => candidates[0],
            0 => throw new InvalidOperationException(
                $"Assembly artifact {artifact.Address} does not contain a public parameterless MicroBundle with ID {artifact.Address.BundleId}."),
            _ => throw new InvalidOperationException(
                $"Assembly artifact {artifact.Address} contains multiple MicroBundles with ID {artifact.Address.BundleId}.")
        };
    }

    private static ReadOnlyMemory<byte> GetAssemblyBytes(MicroBundleArtifact artifact)
    {
        var content = artifact.Content;
        var bytes = content.Span;

        // Raw assembly artifacts remain supported for compatibility with the existing
        // repository catalog tests and previously generated raw-assembly publications.
        if (bytes.Length < EnvelopeMagic.Length ||
            !bytes[..EnvelopeMagic.Length].SequenceEqual(EnvelopeMagic))
            return content;

        if (bytes.Length < EnvelopeHeaderLength)
            throw new InvalidOperationException(
                $"MicroBundle artifact {artifact.Address} contains a truncated FSMB envelope.");

        var formatVersion = BinaryPrimitives.ReadUInt32LittleEndian(bytes[4..8]);
        if (formatVersion != EnvelopeFormatVersion)
            throw new InvalidOperationException(
                $"MicroBundle artifact {artifact.Address} uses unsupported FSMB envelope version {formatVersion}.");

        var envelopeBundleId = BinaryPrimitives.ReadUInt64LittleEndian(bytes[8..16]);
        if (envelopeBundleId != artifact.Address.BundleId)
            throw new InvalidOperationException(
                $"FSMB envelope identifies MicroBundle {envelopeBundleId}, but its repository address identifies {artifact.Address.BundleId}.");

        var assemblyLength = BinaryPrimitives.ReadUInt32LittleEndian(bytes[16..20]);
        var actualAssemblyLength = bytes.Length - EnvelopeHeaderLength;
        if (assemblyLength == 0 || assemblyLength != actualAssemblyLength)
            throw new InvalidOperationException(
                $"FSMB envelope for MicroBundle {artifact.Address.BundleId} declares {assemblyLength} assembly bytes but contains {actualAssemblyLength}.");

        return content[EnvelopeHeaderLength..];
    }
}
