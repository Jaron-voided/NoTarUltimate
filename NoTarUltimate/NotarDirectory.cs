/*namespace NoTarUltimate;

public class NotarDirectory                                 // This stores a directory with only files
{
    internal NotarFileList FileList { get; set; }           
    internal string DirectoryPath { get; set; }
    internal DateTime CreationTime { get; set; }
    internal DateTime LastModifiedTime { get; set; }

    public ulong GetSingleDirectoryPayloadSize()
    {
        ulong totalSize = 0;
        foreach (NotarFile file in FileList)
        {
            totalSize += file.FileSize;
        }
        return totalSize;
    }
}*/