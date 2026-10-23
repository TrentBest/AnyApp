# ExperiencePublisher UML

## Class diagram

~~~mermaid
classDiagram
    class ExperiencePublisher {
        -IMicroBundleCatalog catalog
        -IMicroBundleRepository repository
        +PublishAsync(ExperienceManifest, string, CancellationToken) Task~ExperiencePublicationResult~
    }
    class ExperienceManifest {
        +Bundles
        +ResolvedPlan
        +ToRuntimeManifest() RuntimeManifest
        +CreateConfigurationSource() IMicroBundleConfigurationSource
    }
    class FsmCos {
        +Execute(RuntimeManifest, IMicroBundleConfigurationSource) RuntimeAssembly
    }
    class RuntimeAssembly {
        +Bundles
        +ArbitrationRounds
        +ResolvedConfigurations
    }
    class IMicroBundleResolvedConfiguration {
        <<interface>>
        +ExportResolvedConfiguration() ReadOnlyMemory
    }
    class IMicroBundleRepository {
        <<interface>>
        +GetAsync(address) MicroBundleArtifact
        +PutAsync(artifact) void
    }
    class ExperiencePublicationResult {
        +Manifest
        +ManifestPath
        +ManifestContentHash
        +BundleArtifacts
        +ConfigurationArtifacts
    }

    ExperiencePublisher --> FsmCos : resolves once
    FsmCos --> RuntimeAssembly : produces
    RuntimeAssembly --> IMicroBundleResolvedConfiguration : captures effective config
    ExperiencePublisher --> IMicroBundleRepository : uploads immutable artifacts
    ExperiencePublisher --> ExperienceManifest : emits resolved plan
    ExperiencePublisher --> ExperiencePublicationResult : returns
~~~

## Use case: publish an Experience

1. The author supplies an authored manifest and the catalog containing its MicroBundle roots.
2. FSM_COS resolves dependencies and runs bounded arbitration.
3. The publisher requires every resolved bundle to export restorable effective configuration.
4. The publisher hashes and uploads each bundle assembly and its separate resolved-configuration artifact.
5. It writes the flat dependency-first plan, dependency edges, artifact identities, and convergence round into the manifest.
6. The manifest file is written last, only after artifact uploads succeed.
7. At runtime, AnyApp retrieves or uses cached artifacts; FSM_COS validates the flat plan and restores the saved configuration without rerunning arbitration.
