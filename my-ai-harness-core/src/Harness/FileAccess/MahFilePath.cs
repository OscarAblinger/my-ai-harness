using System;

namespace Ablinger.MyAiHarness.Core.Harness.FileAccess;

/// <summary>
/// Handles MAH specific file paths as well as normal file paths.
/// MAH specific file paths allow you to use certain variables, specifically so you can use relative paths
/// both from a project-directory and the MAH global directory.
/// </summary>
public class MahFilePath
{
    public static bool TryParse(string filepath, out MahFilePath mahFilePath)
    {
        throw new NotImplementedException();
    }

    public static MahFilePath ToMahFilePath(string filepath, Harness harness)
    {
        throw new NotImplementedException();
    }
}