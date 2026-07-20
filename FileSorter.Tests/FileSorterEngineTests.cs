using FileSorter;

namespace FileSorter.Tests;

public class FileSorterEngineTests
{
    [Fact]
    public void Run_WithEmptyDirectory_DoesNotCrash()
    {
        var tempDir = CreateTempDir();
        var engine = new FileSorterEngine(tempDir);
        engine.Run();
        Assert.True(Directory.Exists(tempDir));
        Cleanup(tempDir);
    }

    [Fact]
    public void Run_WithFiles_CreatesExtensionFolders()
    {
        var tempDir = CreateTempDir();
        File.WriteAllText(Path.Combine(tempDir, "test.txt"), "hello");
        File.WriteAllText(Path.Combine(tempDir, "image.png"), "fake png");

        var engine = new FileSorterEngine(tempDir);
        engine.Run();

        Assert.True(Directory.Exists(Path.Combine(tempDir, "txt")));
        Assert.True(Directory.Exists(Path.Combine(tempDir, "png")));
        Assert.True(File.Exists(Path.Combine(tempDir, "txt", "test.txt")));
        Assert.True(File.Exists(Path.Combine(tempDir, "png", "image.png")));
        Cleanup(tempDir);
    }

    [Fact]
    public void Run_DryRun_DoesNotMoveFiles()
    {
        var tempDir = CreateTempDir();
        File.WriteAllText(Path.Combine(tempDir, "test.txt"), "hello");

        var engine = new FileSorterEngine(tempDir, dryRun: true);
        engine.Run();

        // Files should still be in root
        Assert.True(File.Exists(Path.Combine(tempDir, "test.txt")));
        Assert.False(Directory.Exists(Path.Combine(tempDir, "txt")));
        Cleanup(tempDir);
    }

    [Fact]
    public void Run_HandlesNameCollisions()
    {
        var tempDir = CreateTempDir();
        // Create two txt files that would collide
        var subDir = Directory.CreateDirectory(Path.Combine(tempDir, "txt"));
        File.WriteAllText(Path.Combine(tempDir, "doc.txt"), "original");
        File.WriteAllText(Path.Combine(subDir.FullName, "doc.txt"), "existing");

        var engine = new FileSorterEngine(tempDir);
        engine.Run();

        // Original should be renamed with _1 suffix
        Assert.True(File.Exists(Path.Combine(tempDir, "txt", "doc_1.txt")));
        // Existing should still be there
        Assert.True(File.Exists(Path.Combine(tempDir, "txt", "doc.txt")));
        Cleanup(tempDir);
    }

    [Fact]
    public void Undo_RestoresFiles()
    {
        var tempDir = CreateTempDir();
        File.WriteAllText(Path.Combine(tempDir, "test.txt"), "hello");

        var engine = new FileSorterEngine(tempDir);
        engine.Run();
        Assert.True(File.Exists(Path.Combine(tempDir, "txt", "test.txt")));

        // Now undo
        engine.Undo();
        Assert.True(File.Exists(Path.Combine(tempDir, "test.txt")));
        Cleanup(tempDir);
    }

    [Fact]
    public void Run_WithFilesInSubfolders_OnlyProcessesRootFiles()
    {
        var tempDir = CreateTempDir();
        File.WriteAllText(Path.Combine(tempDir, "root.txt"), "root");
        var subDir = Directory.CreateDirectory(Path.Combine(tempDir, "sub"));
        File.WriteAllText(Path.Combine(subDir.FullName, "sub.txt"), "sub");

        var engine = new FileSorterEngine(tempDir);
        engine.Run();

        // Root file should be moved
        Assert.True(File.Exists(Path.Combine(tempDir, "txt", "root.txt")));
        // Subfolder file should stay
        Assert.True(File.Exists(Path.Combine(subDir.FullName, "sub.txt")));
        Cleanup(tempDir);
    }

    private static string CreateTempDir()
    {
        var path = Path.Combine(Path.GetTempPath(), "FileSorterTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(path);
        return path;
    }

    private static void Cleanup(string path)
    {
        try { Directory.Delete(path, recursive: true); }
        catch { /* best effort */ }
    }
}
