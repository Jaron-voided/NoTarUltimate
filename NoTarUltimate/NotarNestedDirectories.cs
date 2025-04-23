/*using System.Collections;

namespace NoTarUltimate;

public class NotarNestedDirectories : IEnumerable<NotarDirectory> // This stores any directory with a directory inside of it
{
    private List<NotarDirectory> Directories = new List<NotarDirectory>(); // Directories inside
    internal NotarFileList FileList { get; set; }       // Files inside this single directory

    
    public ulong GetNestedDirectoryPayloadSize()
    {
        ulong totalSize = FileList.GetPayloadSize();
        
        foreach (NotarDirectory dir in Directories)
        {
            totalSize += dir.GetSingleDirectoryPayloadSize();
        }

        return totalSize;
    }
    
    public IEnumerator<NotarDirectory> GetEnumerator()
    {
        IEnumerator<NotarDirectory> noDir = Directories.GetEnumerator(); //Boxing??
        return noDir;
    }
    
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator(); 
    }
}*/