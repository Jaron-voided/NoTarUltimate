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
        notarFile.FileSize = (uint)file.Length;
        notarFile.CreationTime = file.CreationTime;
        notarFile.LastModifiedTime =  file.LastWriteTime;
        notarFile.FileAttributes = (uint)file.Attributes;
        
        // Gotta work on these 2
        // notarFile.ByteOffset = (ulong)Utils.Align16((int)stream.Position);
        
        Files.Add(notarFile);
        // I feel like more should be happening in here, I'll come back to it
    }

 
    
}