using StoryBuilder.Core.Services;
using StoryBuilder.Persistence;
using TheSingularityWorkshop.FSM_Serialization;

namespace TheSingularityWorkshop.AnyApp;

/// <summary>
/// Stores the native StoryBuilder host's workspace as a versioned FSM_Serialization binary document.
/// A failed load is surfaced to the caller so the host never silently overwrites an unreadable save.
/// </summary>
internal static class StoryBuilderDesktopPersistenceStore
{
    private const string FileName = "StoryBuilder.workspace.bin";

    public static string GetWorkspacePath() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "TheSingularityWorkshop",
            "AnyApp",
            FileName);

    /// <summary>Restores a saved workspace when present; leaves a new session unchanged when absent.</summary>
    public static bool LoadIfPresent(StoryBuilderSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var path = GetWorkspacePath();
        if (!File.Exists(path))
            return false;

        var bytes = File.ReadAllBytes(path);
        using var stream = new MemoryBinaryStream(bytes);
        var document = new StoryWorkspaceBinaryDocument();
        document.Unpack(stream);
        document.RestoreInto(session);
        return true;
    }

    /// <summary>Atomically replaces the host-local save after encoding the complete workspace.</summary>
    public static void Save(StoryBuilderSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        var path = GetWorkspacePath();
        var directory = Path.GetDirectoryName(path)
            ?? throw new InvalidOperationException("The StoryBuilder workspace path has no parent directory.");
        Directory.CreateDirectory(directory);

        using var stream = new MemoryBinaryStream();
        StoryWorkspaceBinaryDocument.FromSession(session).Pack(stream);
        var bytes = stream.ToArray();
        var temporaryPath = path + ".tmp";

        try
        {
            File.WriteAllBytes(temporaryPath, bytes);
            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }
}
