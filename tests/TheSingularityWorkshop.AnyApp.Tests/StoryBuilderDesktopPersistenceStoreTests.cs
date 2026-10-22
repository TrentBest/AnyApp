using StoryBuilder.MicroBundle;
using Xunit;

namespace TheSingularityWorkshop.AnyApp.Tests;

public sealed class StoryBuilderDesktopPersistenceStoreTests
{
    [Fact]
    public void SaveAndLoad_RestoresWorkspaceRecordsWithStableIds()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var path = Path.Combine(directory, "workspace.bin");
            var original = new StoryBuilderAuthoringBundle().CreateSession("Persistence test");
            var surface = new StoryBuilderAuthoringSurface(original);

            Assert.True(surface.Execute(StoryBuilderAuthoringCommands.CreateEvent, "Opening incident").Succeeded);
            Assert.True(surface.Execute(StoryBuilderAuthoringCommands.CreateManuscriptSection, "Chapter one").Succeeded);
            Assert.True(surface.Execute(StoryBuilderAuthoringCommands.LinkLatestEventToLatestSection).Succeeded);

            var expectedEventIds = original.Workspace.Events.Select(item => item.Id).ToArray();
            var expectedManuscriptIds = original.Workspace.Manuscripts.Select(item => item.Id).ToArray();
            var expectedSections = original.Workspace.Manuscripts
                .SelectMany(item => item.Sections)
                .ToArray();
            var expectedSectionIds = expectedSections.Select(item => item.Id).ToArray();
            var expectedSectionEventIds = expectedSections.ToDictionary(
                item => item.Id,
                item => item.EventIds.ToArray());

            StoryBuilderDesktopPersistenceStore.Save(original, path);

            var restored = new StoryBuilderAuthoringBundle().CreateSession("Fresh session");
            Assert.True(StoryBuilderDesktopPersistenceStore.LoadIfPresent(restored, path));

            Assert.Equal("Persistence test", restored.Workspace.Name);
            Assert.Equal(expectedEventIds, restored.Workspace.Events.Select(item => item.Id).ToArray());
            Assert.Equal(expectedManuscriptIds, restored.Workspace.Manuscripts.Select(item => item.Id).ToArray());
            var restoredSections = restored.Workspace.Manuscripts
                .SelectMany(item => item.Sections)
                .ToArray();
            Assert.Equal(expectedSectionIds, restoredSections.Select(item => item.Id).ToArray());
            foreach (var section in restoredSections)
            {
                Assert.Equal(expectedSectionEventIds[section.Id], section.EventIds.ToArray());
            }
            var roundTripPath = Path.Combine(directory, "round-trip.bin");
            StoryBuilderDesktopPersistenceStore.Save(restored, roundTripPath);
            Assert.Equal(File.ReadAllBytes(path), File.ReadAllBytes(roundTripPath));
            Assert.False(File.Exists(path + ".tmp"));
            Assert.False(File.Exists(roundTripPath + ".tmp"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void LoadIfPresent_WhenSaveIsUnreadable_DoesNotModifyTheExistingFile()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var path = Path.Combine(directory, "workspace.bin");
            var invalidBytes = new byte[] { 0x53, 0x42, 0x00, 0xFF, 0x19 };
            File.WriteAllBytes(path, invalidBytes);

            var session = new StoryBuilderAuthoringBundle().CreateSession("Must remain empty");
            Assert.ThrowsAny<Exception>(() => StoryBuilderDesktopPersistenceStore.LoadIfPresent(session, path));

            Assert.Equal(invalidBytes, File.ReadAllBytes(path));
            Assert.Empty(session.Workspace.Events);
            Assert.Empty(session.Workspace.Manuscripts);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "AnyApp-StoryBuilder-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
