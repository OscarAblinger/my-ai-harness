using System;
using System.Collections.Generic;
using System.IO;

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

    public IEnumerable<string> EnumerateFiles(string projectPath)
    {
        return Directory.EnumerateFiles(projectPath);
    }

    public void DeleteDirectory(string directoryPath, bool recursive)
    {
        Directory.Delete(directoryPath, recursive);
    }

    public void DeleteFile(string filePath)
    {
        File.Delete(filePath);
    }

    public bool Exists(string path)
    {
        return Directory.Exists(path) || File.Exists(path);
    }

    public Stream OpenRead(string filePath)
    {
        return File.OpenRead(filePath);
    }
}
