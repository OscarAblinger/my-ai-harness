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
}
