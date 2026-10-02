using System.Reflection;
using TheSingularityWorkshop.FSM_COS;
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
/// </summary>
public sealed class AssemblyMicroBundleArtifactMaterializer : IMicroBundleArtifactMaterializer
{
    public IMicroBundle Materialize(MicroBundleArtifact artifact)
    {
        ArgumentNullException.ThrowIfNull(artifact);

        if (artifact.Content.IsEmpty)
            throw new InvalidOperationException(
                $"MicroBundle artifact {artifact.Address} contains no assembly bytes.");

        var assembly = Assembly.Load(artifact.Content.ToArray());
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
}