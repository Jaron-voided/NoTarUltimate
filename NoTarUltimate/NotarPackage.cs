namespace NoTarUltimate;

class NotarPackage // This is the final object. It holds the header and any nested directories
{
    public NotarHeader Header  { get; init; }
    public NotarFileList FileList { get; init; }


    
    // Should this one return void also? I'm unsure since Package doesn't contain just a NotarFile, but NotarFileList
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
}