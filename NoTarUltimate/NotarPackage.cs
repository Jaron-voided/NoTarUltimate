namespace NoTarUltimate;

class NotarPackage // This is the final object. It holds the header and any nested directories
{
    public NotarHeader Header  { get; init; }
    public NotarNestedDirectories Directories { get; init; }


    

    public void FileFactory(string pathToFile)
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
        notarFile.FileAttributes = file.Attributes;
        //notarFile.ByteOffset = (ulong)file.Length;
    }
    
    internal void FileListFactory(List<NotarFile> files)
    {
        NotarFileList fileList = new NotarFileList();
        foreach (NotarFile file in files)
        {
            fileList.Files.Add(file);
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

        uint fileCount = 0;
        foreach (FileInfo file in directory.GetFiles())
        {
            fileCount +=  (uint)file.Length;
        }
        
        Header.FileListSize = fileCount;

        uint totalSize = 0;

        Header.PayloadOffset = fileCount - 128;

        Header.PayloadSize = (uint)Directories.GetNestedDirectoryPayloadSize();
        //Header.PayloadHash = ???
    }
}