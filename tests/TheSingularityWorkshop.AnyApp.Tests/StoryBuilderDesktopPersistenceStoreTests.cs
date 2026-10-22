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

            var expectedEventIds = original.Workspace.Events.Select(item => item.Id).ToArray();
            var expectedManuscriptIds = original.Workspace.Manuscripts.Select(item => item.Id).ToArray();
            var expectedSectionIds = original.Workspace.Manuscripts
                .SelectMany(item => item.Sections)
                .Select(item => item.Id)
                .ToArray();

            StoryBuilderDesktopPersistenceStore.Save(original, path);

            var restored = new StoryBuilderAuthoringBundle().CreateSession("Fresh session");
            Assert.True(StoryBuilderDesktopPersistenceStore.LoadIfPresent(restored, path));

            Assert.Equal("Persistence test", restored.Workspace.Name);
            Assert.Equal(expectedEventIds, restored.Workspace.Events.Select(item => item.Id).ToArray());
            Assert.Equal(expectedManuscriptIds, restored.Workspace.Manuscripts.Select(item => item.Id).ToArray());
            Assert.Equal(expectedSectionIds, restored.Workspace.Manuscripts
                .SelectMany(item => item.Sections)
                .Select(item => item.Id)
                .ToArray());
            Assert.False(File.Exists(path + ".tmp"));
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
