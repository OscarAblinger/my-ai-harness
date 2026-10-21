using System.IO;
using System.Linq;

namespace Ablinger.MyAiHarness.Core.Harness.FileAccess;

public static class PathMaker
{
    private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();
    
    public static string MakeFileName(string potentialName)
    {
        return new string(potentialName.Where(ch => !InvalidFileNameChars.Contains(ch)).ToArray());
    }

    public static string FindUniquePath(IFileAccess fileAccess, string basePath, string desiredName)
    {
        var fullPath = Path.Combine(basePath, MakeFileName(desiredName));

        if (!fileAccess.Exists(fullPath))
        {
            return fullPath;
        }

        var counter = 0;
        do
        {
            fullPath = Path.Combine(basePath, MakeFileName(desiredName) + counter);
            counter++;
        } while (fileAccess.Exists(fullPath));

        return fullPath;
    }
}