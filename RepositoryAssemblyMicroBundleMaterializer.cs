using System.Reflection;
using TheSingularityWorkshop.MicroBundleDomain;
using TheSingularityWorkshop.MicroBundleRepository.Core;

namespace TheSingularityWorkshop.AnyApp;

/// <summary>
/// Host-side adapter for the repository's current assembly artifact representation.
/// </summary>
/// <remarks>
/// The repository verifies the immutable artifact identity before this boundary.
/// AnyApp owns the final step from opaque bytes to an <see cref="IMicroBundle"/>
/// until the shared MicroBundleRepository.FSM_COS adapter is published as a consumable package.
/// </remarks>
internal static class RepositoryAssemblyMicroBundleMaterializer
{
    public static IMicroBundle Materialize(MicroBundleArtifact artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);

        var payload = MicroBundleAssemblyPayload.FromBytes(artifact.Content);
        if (payload.BundleId != artifact.Address.BundleId)
        {
            throw new InvalidOperationException(
                $"MicroBundle payload ID {payload.BundleId} does not match artifact ID {artifact.Address.BundleId}.");
        }

        var assembly = Assembly.Load(payload.AssemblyBytes.ToArray());
        var matches = assembly
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

        return matches.Length switch
        {
            1 => matches[0],
            0 => throw new InvalidOperationException(
                $"Assembly artifact {artifact.Address} does not contain a public parameterless MicroBundle with ID {artifact.Address.BundleId}."),
            _ => throw new InvalidOperationException(
                $"Assembly artifact {artifact.Address} contains multiple MicroBundles with ID {artifact.Address.BundleId}.")
        };
    }
}
