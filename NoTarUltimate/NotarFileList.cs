using System.Collections;

namespace NoTarUltimate;

// Stores a list of files
public class NotarFileList    
{
    internal readonly List<NotarFile> Files = new List<NotarFile>();
    internal uint Count => (uint)Files.Count;
    internal uint FileListSize => (Count * NotarFile.NotarFileInfoSize);

    internal void AddFile(string filePath)
    {
        FileInfo file = new FileInfo(filePath);
        NotarFile notarFile = new NotarFile();

        notarFile.FilePath = filePath;
        //notarFile.FileSize = (uint)file.Length;
        notarFile.FileSize = (uint)((file.Attributes & FileAttributes.Directory) 
            == FileAttributes.Directory ? 0 : file.Length);
        notarFile.CreationTime = file.CreationTime;
        notarFile.LastModifiedTime =  file.LastWriteTime;
        notarFile.FileAttributes = (uint)file.Attributes;
        
        Files.Add(notarFile);
    }
}