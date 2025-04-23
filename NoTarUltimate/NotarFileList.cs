using System.Collections;

namespace NoTarUltimate;

public class NotarFileList    // Stores a list of files
{
    internal readonly List<NotarFile> Files = new List<NotarFile>();

    // I took away the methods I created that emulated Directory.EnumerateFiles()
    // What else could go in here?

    public IEnumerator<NotarFile> GetEnumerator()
    {
        throw new NotImplementedException();
    }
}