using System.Security.Cryptography;
using System.Text;

namespace NoTarUltimate;

public class NotarPackage // This is the final object. It holds the header and any nested directories
{
    NotarHeader Header { get; set; } = new NotarHeader(); // This instantiates Header when I construct a package
    NotarFileList FileList { get; set; } = new NotarFileList();
    public string RelativeTo { get; set; } = string.Empty; // changed this from init to stop the FromDirectory RelativeTo error...

    public uint FileListSize => FileList.FileListSize;

    internal NotarPackage()
    {
        
    }

    
    // This runs before running Pack. I instantiate a NotarPackage with this information
    // then run newNotarPackage.Pack
    public NotarPackage FromDirectory(string directoryPath) // changed this from static to get rid of RelativeTo error?
    {
        if (!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");;
        }
        
        // NotarPackage notarPackage = new();
        // {
        //     // It doesn't want this to be a static method??
        //     RelativeTo = directoryPath;
        // }

        this.RelativeTo = directoryPath;
        /*NotarPackage notarPackage = new();
        notarPackage.RelativeTo = directoryPath;*/

        foreach (var file in Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories))
        {
            this.FileList.AddFile(file);
        }
        return this;
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
            // Have I already gotten this information, or is it set in another function?
            // Or do I need to do this manually
            // Is the NotarPackage prepared in any sort before this function?
            file.SerializePath(stream, file.FilePath);
        }
        
        // The stream should now be to the payload data spot
        Header.PayloadOffset = (uint)Utils.Align16((int)stream.Position);
        foreach (NotarFile file in FileList.Files)
        {
            stream.Seek(Utils.Align16((int)stream.Position), SeekOrigin.Current);

            using BinaryWriter writer = new(stream, Encoding.UTF8, true);
            // capture the spot where the file starts
            file.ByteOffset = (ulong)stream.Position; 
            // pretty proud of myself for the above line, I hope its correct

            // I should use BinaryWriter here, and not just stream.write?
            byte[] payLoad = File.ReadAllBytes(file.FilePath);
            writer.Write(payLoad, 0, payLoad.Length);
        }
        
        // Go back and write the File List
        stream.Seek(Utils.Align16((int)(NotarHeader.HeaderSizeInBytes)), SeekOrigin.Begin); // Gets past header to write file info
    
        // Write the file info
        foreach (NotarFile file in FileList.Files)
        {
            file.Serialize(stream);
        }
        
        // back to the header
        stream.Seek(0, SeekOrigin.Begin);

        Header.VersionMajor = 1;
        Header.VersionMinor = 1;
        Header.FileLayoutVersion = 1;
        Header.FeatureFlags = 1;
        Header.FileCount = FileList.Count;
        Header.FileListSize = FileList.FileListSize;
        Header.PayloadSize = (ulong)(stream.Length - Header.PayloadOffset);

        MakeHash(stream);

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
        using FileStream readStream = new(inputPath, FileMode.Open, FileAccess.Read);
        //using FileStream writeStream = new FileStream();
        NotarPackage notarPackage = new NotarPackage();
        
        
        // I'm unsure if I need the following 2 lines anymore
        // Create a correctly sized array to write my header to
        //byte[] headerArrayBytes = new byte[NotarHeader.HeaderSizeInBytes];
        
        // Read the bytes into the headerArray
        // bytesRead = readStream.Read(headerArrayBytes, 0, NotarHeader.HeaderSizeInBytes);
        
        // Header.Deserialize movees itself the correct amount of bytes for each member
        // this should deserialize my header and move the stream to the spot after the header, file paths
        notarPackage.Header.Deserialize(readStream);
        
        // Create a list to hold partial files, i don't have the paths yet so I can't run 
        // FileList.AddFile(path)
        List<NotarFile> files = new();

        for (int i = 0; i < Header.FileCount; i++)
        {
            // Creates empty NotarFile
            NotarFile file = new NotarFile();
            file.Deserialize(readStream);
            
            // Adds the file to FileList, there's not a path yet so I can't run 
            files.Add(file);
        }
        
        // Deserialize file paths
        foreach (NotarFile file in files)
        {
            // Advances the stream to obtain each filepath
            file.DeserializePath(readStream);
            
            // Now add the complete file to my FileList
            FileList.AddFile(file.FilePath);
        }
        
        // Stream should be at payload now
        // Create files and fill in there payload
        // I should use ToDirectory
        foreach (NotarFile file in FileList.Files)
        {
            FileStream newFile = new FileStream(file.FilePath, FileMode.CreateNew, FileAccess.Write);
            
            // Create an empty byte[] to hold the payload I'm about to read
            byte[] payLoad = new byte[file.FileSize];
            
            int bytesRead = readStream.Read(payLoad, 0, payLoad.Length);
            newFile.Write(payLoad, 0, bytesRead);
        }
    }

    internal byte[] ComputePayloadHash(Stream stream)
    {
        // Skip to payload Offset, then go forward PayloadSize
        // Create a byte array
        // Run SHA256??
        
        // 20 for the size of PayloadHash?
        // I don't know how to run ReadExactly without passing a byte[]
        byte[] payload = new byte[Header.PayloadSize];
        
        // Seek to the start of the payload
        stream.Seek(Header.PayloadOffset, SeekOrigin.Begin);
        stream.ReadExactly(payload, 0, (int)Header.PayloadSize);
        using var sha256 = SHA256.Create();
        byte[] payloadHash = sha256.ComputeHash(payload);
        return payloadHash;
    }

    internal void MakeHash(Stream stream)
    {
        var hash = ComputePayloadHash(stream);
        Header.PayloadHash = new PayloadHash
        {
            PayloadHashPartA = BitConverter.ToUInt64(hash, 0),
            PayloadHashPartB = BitConverter.ToUInt64(hash, 8),
            PayloadHashPartC = BitConverter.ToUInt32(hash, 16)
        };
    }
}