using System.Collections;

namespace NoTarUltimate;

public class NotarFileList    // Stores a list of files
{
    internal readonly List<NotarFile> Files = new List<NotarFile>();

    internal uint FileListSize()
    {
        uint size = 0;
        foreach (var file in Files)
        {
            size += (uint)file.FileSize;
        }
        return size;
    }

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
        //notarFile.ByteOffset = (ulong)Utils.Align16((int)stream.Position);

        notarFile.IsDirectory = file.Attributes.HasFlag(FileAttributes.Directory);

        Files.Add(notarFile);
        // I feel like more should be happening in here, I'll come back to it
    }

 
    
}