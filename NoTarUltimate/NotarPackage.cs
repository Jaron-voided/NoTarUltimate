namespace NoTarUltimate;

public class NotarPackage // This is the final object. It holds the header and any nested directories
{
    NotarHeader Header { get; set; } = new NotarHeader(); // This instantiates Header when I construct a package
    NotarFileList FileList { get; set; } = new NotarFileList();
    public string RelativeTo { get; set; } = string.Empty;
    
    public uint FileListSize => FileList.FileListSize();

    public static NotarPackage FromDirectory(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");;
        }
        
        NotarPackage notarPackage = new NotarPackage();
        {
            // It doesn't want this to be a static method??
            RelativeTo = directoryPath;
        }

        foreach (var file in Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories))
        {
            notarPackage.FileList.AddFile(file);
        }
        return notarPackage;
        //I'll need to run this method then take the returned NotarPackage and add the header adn filelistInfo
    }

    public static void ToDirectory(string filePath, string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        if (!File.Exists(filePath))
        {
            File.Create(filePath).Close();
        }
        // I'm unsure on this one, I don't know what else I can do with only the 2 string inputs
        // Its a static method so I can't do "notarPackage.ToDirectory and have access to all the instances members and data??
    }

    public void Pack(string outputPath)
    {
        // Skip Header
        // Skip FileList
        // Align all stuff from here down with Align16
        // Write Paths
        // Write Payload (fills in FileList offsets and crc)
        // Complete Header
        // Seek Beginning
        // Write Header
        // Write FileList
    }

    public void UnPack(string inputPath)
    {
        // Skip 128 bytes past header
        // Use header.FileCount to know how many NotarFiles to skip past on stream??
        // Header | FileList | Paths | Payload data
        // Is FileList information stored seperately from the paths? Or just written seperately
        // Use the first path to create the directory or file.
        // Use ByteOffset to know where the payload is and write to new file.
        // Skip back and forth creating directories/files from paths then writing payload.
    }

    private byte[] ComputePayloadHash(Stream stream)
    {
        // Skip to payload Offset, then go forward PayloadSize
        // Create a byte array
        // Run SHA256??
    }
}