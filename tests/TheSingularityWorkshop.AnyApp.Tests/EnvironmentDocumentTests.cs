using System.Text.Json;
using TheSingularityWorkshop.AnyApp.Environments;
using Xunit;

namespace TheSingularityWorkshop.AnyApp.Tests;

public sealed class EnvironmentDocumentTests
{
    [Fact]
    public void AcceptsAValidDeclarativeEnvironment()
    {
        var document = new EnvironmentDocument
        {
            Name = "Starter world",
            Objects =
            [
                new EnvironmentObject { Id = "floor-1", Kind = "floor", Width = 20, Height = 1 },
                new EnvironmentObject { Id = "ball-1", Kind = "ball", X = 4, Y = 2 }
            ]
        };

        var result = EnvironmentDocumentValidator.Validate(document);

        Assert.True(result.IsValid);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void RejectsDuplicateIdentityWithoutUsingPositionAsIdentity()
    {
        var document = new EnvironmentDocument
        {
            Objects =
            [
                new EnvironmentObject { Id = "object-1", Kind = "cube", X = 0 },
                new EnvironmentObject { Id = "object-1", Kind = "cube", X = 8 }
            ]
        };

        var result = EnvironmentDocumentValidator.Validate(document);

        Assert.Contains(result.Diagnostics, item => item.Code == "environment.object.id.duplicate");
    }

    [Fact]
    public void RejectsNonFiniteAndNonPositiveGeometry()
    {
        var document = new EnvironmentDocument
        {
            Objects =
            [
                new EnvironmentObject { Id = "bad", Kind = "cube", X = double.NaN, Width = 0, Height = double.PositiveInfinity }
            ]
        };

        var result = EnvironmentDocumentValidator.Validate(document);

        Assert.Contains(result.Diagnostics, item => item.Code == "environment.object.position.nonfinite");
        Assert.Contains(result.Diagnostics, item => item.Code == "environment.object.size.invalid");
    }

    [Fact]
    public void RejectsUnsupportedKindsAndDanglingConnections()
    {
        var document = new EnvironmentDocument
        {
            Objects = [new EnvironmentObject { Id = "object-1", Kind = "arbitrary-script" }],
            Connections =
            [
                new EnvironmentConnection
                {
                    Id = "link-1",
                    SourceObjectId = "object-1",
                    TargetObjectId = "missing",
                    ContractId = "example.inspect",
                    ContractVersion = "1.0.0"
                }
            ]
        };

        var result = EnvironmentDocumentValidator.Validate(document);

        Assert.Contains(result.Diagnostics, item => item.Code == "environment.object.kind.unsupported");
        Assert.Contains(result.Diagnostics, item => item.Code == "environment.connection.target.missing");
    }

    [Fact]
    public void PreservesStableIdsAndDocumentDataAcrossJsonRoundTrip()
    {
        var document = new EnvironmentDocument
        {
            Name = "Round trip",
            Objects =
            [
                new EnvironmentObject
                {
                    Id = "cube-7",
                    Kind = "cube",
                    X = 3.5,
                    Appearance = "steel",
                    Capabilities = [new EnvironmentCapabilityBinding { CapabilityId = "inspect.readonly", Version = "1.0.0" }]
                }
            ]
        };

        var json = JsonSerializer.Serialize(document);
        var restored = JsonSerializer.Deserialize<EnvironmentDocument>(json);

        Assert.NotNull(restored);
        Assert.Equal("cube-7", Assert.Single(restored!.Objects).Id);
        Assert.Equal("steel", restored.Objects[0].Appearance);
        Assert.True(EnvironmentDocumentValidator.Validate(restored).IsValid);
    }

    [Fact]
    public void RejectsInvalidWorldBoundsAndObjectsOutsideThem()
    {
        var document = new EnvironmentDocument
        {
            Bounds = new EnvironmentBounds { Width = 10, Height = 10 },
            Objects =
            [
                new EnvironmentObject { Id = "outside", Kind = "cube", X = 9, Y = 1, Width = 2, Height = 2 }
            ]
        };

        var result = EnvironmentDocumentValidator.Validate(document);

        Assert.Contains(result.Diagnostics, item => item.Code == "environment.object.bounds.exceeded");

        var invalidBounds = EnvironmentDocumentValidator.Validate(
            new EnvironmentDocument { Bounds = new EnvironmentBounds { Width = double.PositiveInfinity, Height = 0 } });
        Assert.Contains(invalidBounds.Diagnostics, item => item.Code == "environment.bounds.invalid");
    }

    [Fact]
    public void RejectsUnsupportedSchemaVersion()
    {
        var result = EnvironmentDocumentValidator.Validate(new EnvironmentDocument { SchemaVersion = 2 });

        Assert.Contains(result.Diagnostics, item => item.Code == "environment.schema.unsupported");
    }
}
