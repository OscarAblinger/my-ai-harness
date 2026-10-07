using System.Collections.Generic;
using System.IO;
using Ablinger.MyAiHarness.Core.Plugins.Interfaces;

namespace Ablinger.MyAiHarness.Core.Harness.FileAccess;

public class LocalFileAccess : IFileAccess
{
    public string ReadAllText(string path)
    {
        return File.ReadAllText(path);
    }

    public void WriteAllText(string path, string contents)
    {
        File.WriteAllText(path, contents);
    }

    public void CreateDirectory(string path)
    {
        Directory.CreateDirectory(path);
    }

    public IEnumerable<string> EnumerateDirectories(string path)
    {
        return Directory.EnumerateDirectories(path);
    }

    public void DeleteDirectory(string directoryPath, bool recursive)
    {
        Directory.Delete(directoryPath, recursive);
    }

    public void DeleteFile(string filePath)
    {
        File.Delete(filePath);
    }
}
