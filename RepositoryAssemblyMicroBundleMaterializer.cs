using TheSingularityWorkshop.MicroBundleRepository.Core;

namespace TheSingularityWorkshop.AnyApp;

/// <summary>
/// Bridges repository artifacts to the host's existing assembly materializer.
/// The repository artifact contains opaque, hash-verified bytes; the existing
/// materializer defines the current host interpretation as raw .NET assembly bytes.
/// </summary>
internal static class RepositoryAssemblyMicroBundleMaterializer
{
    private static readonly AssemblyMicroBundleArtifactMaterializer Materializer = new();

    public static TheSingularityWorkshop.MicroBundleDomain.IMicroBundle Materialize(
        MicroBundleArtifact artifact) =>
        Materializer.Materialize(artifact);
}
