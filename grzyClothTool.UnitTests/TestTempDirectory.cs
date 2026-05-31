namespace grzyClothTool.UnitTests;

internal sealed class TestTempDirectory : IDisposable
{
    public TestTempDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "grzyClothTool.UnitTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public string FilePath(params string[] parts)
    {
        return System.IO.Path.Combine([Path, .. parts]);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
        catch
        {
            // Best-effort cleanup for Windows file handles held by async loaders.
        }
    }
}
