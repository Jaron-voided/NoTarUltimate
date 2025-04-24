namespace NoTarUltimate;

public class Extras
{
    
    /*Your code is very much like that, but in C# form...

    Stream s = File.OpenRead("path\to\file.notar");
    NotarHeader header = new();
    header.Deserialize(s); */
    // Init function
    // Serialize a directory
    NotarPackage package = NotarPackage.FromDirectory("path\to\directory");

// Deserialize to a directory
    NotarPackage.ToDirectory("path\to\file.notar", "path\to\directory");
    
      public NotarFile FileFactory(string pathToFile, Stream stream)
    {
        if (!File.Exists(pathToFile))
            throw new FileNotFoundException(pathToFile);
        
        FileInfo file = new FileInfo(pathToFile); // To get info from the file
        DirectoryInfo directory = file.Directory; // To get info about the dirPath
        string directoryPath = directory.FullName;
        string filePath = directoryPath + file.FullName;
        
        var notarFile = new NotarFile();
        notarFile.FilePath = filePath;
        notarFile.FileSize = (uint)file.Length;
        notarFile.CreationTime = file.CreationTime;
        notarFile.LastModifiedTime =  file.LastWriteTime;
        notarFile.FileAttributes = (uint)file.Attributes;
        notarFile.ByteOffset = (ulong)Utils.Align16((int)stream.Position);
        notarFile.IsDirectory = file.Attributes.HasFlag(FileAttributes.Directory);
        return notarFile;
    }
    
    internal void FileListFactory(List<NotarFile> files)
    {
        // NotarFileList fileList = new NotarFileList();
        foreach (NotarFile file in files)
        {
            FileList.Files.Add(file);
        }
        
    }
    
    // I can use a blank constructor, then use these methods to add the properties
    // NewPackage.Header = HeaderFactory(); Use this last and I'll have more information
    internal void HeaderFactory(string directoryPath)
    {
        DirectoryInfo directory = new DirectoryInfo(directoryPath);
        Header.VersionMajor = 1;
        Header.VersionMinor = 0;
        Header.FileLayoutVersion = 1;
        Header.FeatureFlags = 1;
        Header.DirectoryCount = (ushort)directory.GetDirectories().Length;
        Header.FileCount = (ushort)directory.GetFiles().Length;

        uint fileListSize = 0;
        foreach (var file in Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories))
        {
            fileListSize +=  (uint)file.Length;
        }
        
        Header.FileListSize = fileListSize;

        uint totalSize = 0;

        Header.PayloadOffset = fileListSize - 128;

        // Which one is PayloadSize vs FileListSize
        //Header.PayloadSize = ???
        //Header.PayloadHash = ???
}