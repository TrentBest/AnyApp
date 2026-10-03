using TheSingularityWorkshop.FSM_COS;

namespace TheSingularityWorkshop.AnyApp;

public static class AnyAppRuntime
{
    public const ulong RuntimeId = MonikerExperience.RuntimeId;

    public static RuntimeAssembly Compose(RuntimeManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        var cos = new FsmCos(new AnyAppMicroBundleCatalog());
        return cos.Execute(manifest);
    }

    public static async Task<RuntimeAssembly> ComposeAsync(
        ExperienceManifest experience,
        Uri repositoryEndpoint,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(experience);
        ArgumentNullException.ThrowIfNull(repositoryEndpoint);

        if (!experience.UsesRepositoryArtifacts)
            return Compose(experience.ToRuntimeManifest());

        var catalog = new RepositoryMicroBundleCatalog(
            repositoryEndpoint,
            experience);

        var runtimeManifest = experience.ToRuntimeManifest();
        await catalog.PreloadClosureAsync(
            runtimeManifest.Bundles.Select(bundle => bundle.BundleId),
            cancellationToken);

        return new FsmCos(catalog).Execute(runtimeManifest);
    }
}
