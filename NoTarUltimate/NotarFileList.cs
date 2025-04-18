using System.Collections;

namespace NoTarUltimate;

public class NotarFileList : IEnumerable<NotarFile>    // Stores a list of files
{
    internal readonly List<NotarFile> Files = new List<NotarFile>();
    
    public ulong GetPayloadSize()
    {
        ulong totalSize = 0;
        foreach (NotarFile file in Files)
        {
            totalSize += file.FileSize;
        }
        return totalSize;
    }

    public IEnumerator<NotarFile> GetEnumerator()
    {
        IEnumerator<NotarFile> files = Files.GetEnumerator(); //Boxing??
        return files;
    }
    
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator(); // Calls the generic version
    }
}