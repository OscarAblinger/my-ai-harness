namespace Ablinger.MyAiHarness.Core.Harness.FileAccess;

public interface IFileAccess
{
    /// <summary>
    /// Reads the content of the file at <paramref name="path"/>.
    /// </summary>
    string ReadAllText(string path);

    /// <summary>
    /// Writes <paramref name="contents"/> to the file at <paramref name="path"/>, overwriting an existing file.
    /// </summary>
    void WriteAllText(string path, string contents);

    /// <summary>
    /// Creates the directory at <paramref name="path"/> including all missing parent directories.
    /// </summary>
    void CreateDirectory(string path);
}
