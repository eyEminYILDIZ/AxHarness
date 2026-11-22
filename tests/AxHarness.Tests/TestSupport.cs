namespace AxHarness.Tests;

internal static class TestSupport
{
    /// <summary>Creates a directory symlink; returns false when the platform or user may not (Windows without Developer Mode).</summary>
    public static bool TryCreateDirectorySymlink(string link, string target)
    {
        try
        {
            Directory.CreateSymbolicLink(link, target);
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
        {
            return false;
        }
    }
}
