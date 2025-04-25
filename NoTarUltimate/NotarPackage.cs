using System.Security.Cryptography;

namespace NoTarUltimate;

public class NotarPackage // This is the final object. It holds the header and any nested directories
{
    NotarHeader Header { get; set; } = new NotarHeader(); // This instantiates Header when I construct a package
    NotarFileList FileList { get; set; } = new NotarFileList();
    public string RelativeTo { get; set; } = string.Empty;

    public uint FileListSize => FileList.FileListSize;

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
        // I would just call this in a loop to create all the files I pass to it?
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
        
        using FileStream stream = new(outputPath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        
        // Gets past header and file list to start writing paths
        stream.Seek(Utils.Align16((int)(NotarHeader.HeaderSizeInBytes + FileListSize)), SeekOrigin.Begin); 
        
        // Writes all the paths
        foreach (NotarFile file in FileList.Files) 
        {
            // This should be extracted into a method
            stream.Seek(Utils.Align16((int)stream.Position), SeekOrigin.Current);
            byte[] path = File.ReadAllBytes(file.FilePath);
            stream.Write(path, 0, path.Length);
        }
        // The stream should now be to the payload data spot
        Header.PayloadOffset = (uint)Utils.Align16((int)stream.Position);
        foreach (NotarFile file in FileList.Files)
        {
            stream.Seek(Utils.Align16((int)stream.Position), SeekOrigin.Current);
            
            // capture the spot where the file starts
            file.ByteOffset = (ulong)stream.Position; 
            // pretty proud of myself for the above line, I hope its correct

            byte[] payLoad = File.ReadAllBytes(file.FilePath);
            stream.Write(payLoad, 0, payLoad.Length);
        }
        
        // Go back and write the File List
        stream.Seek(Utils.Align16((int)(NotarHeader.HeaderSizeInBytes + FileListSize)), SeekOrigin.Begin); // Gets past header to write file info
        // Presumably I have already ran (FromDirectory) and my File Info has been filled in?
       
        // Write the file info
        foreach (NotarFile file in FileList.Files)
        {
            file.Serialize(stream);
        }
        
        // back to the header
        stream.Seek(0, SeekOrigin.Begin);
        
        // Presumably things like Version are set in a different method?
        Header.FileCount = FileList.Count;
        Header.FileListSize = FileList.FileListSize;
        Header.PayloadSize = (ulong)(Header.PayloadOffset + stream.Length);
        Header.Serialize(stream);

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
        using FileStream stream = new(inputPath, FileMode.Open, FileAccess.Read);
        MemoryStream memStream = new MemoryStream();
        NotarPackage notarPackage = new NotarPackage();
        byte[] headerArrayBytes = new byte[NotarHeader.HeaderSizeInBytes];
        int bytesRead = stream.Read(headerArrayBytes, 0, NotarHeader.HeaderSizeInBytes);
        stream.Write(headerArrayBytes, 0, NotarHeader.HeaderSizeInBytes);
        notarPackage.Header.Deserialize(memStream);
        
        /*
        stream.Seek(NotarHeader.HeaderSizeInBytes + FileListSize, SeekOrigin.Begin);
        */
        // skipped past the header and FileListInfo
    }

    private byte[] ComputePayloadHash(Stream stream)
    {
        // Skip to payload Offset, then go forward PayloadSize
        // Create a byte array
        // Run SHA256??
        
        // 20 for the size of PayloadHash?
        byte[] payload = new byte[Header.PayloadSize];
        
        // Seek to the start of the payload
        stream.Seek(Header.PayloadOffset, SeekOrigin.Begin);
        stream.ReadExactly(payload, 0, (int)Header.PayloadSize);
        using var sha256 = SHA256.Create();
        byte[] payloadHash = sha256.ComputeHash(payload);
        return payloadHash;
    }
}