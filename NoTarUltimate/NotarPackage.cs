using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace NoTarUltimate;

// This is the final object. It holds the header and any nested directories
public class NotarPackage 
{
    NotarHeader Header { get; set; } = new NotarHeader();
    NotarFileList FileList { get; set; } = new NotarFileList();
    public string RelativeTo { get; set; } = string.Empty;
    private uint FileListSize => FileList.FileListSize;

    internal NotarPackage() { }

    
    // This runs before running Pack. I instantiate a NotarPackage with this information
    public NotarPackage FromDirectory(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");;
        }
        
        this.RelativeTo = directoryPath;

        foreach (var file in Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories))
        {
            this.FileList.AddFile(file);
        }
        return this;
    }
    
    // Gets called to Unpack a file
    public static void ToDirectory(string filePath, string directoryPath)
    {
        NotarPackage notarPackage = new()
        {
            RelativeTo = directoryPath
        };

        notarPackage.UnPack(filePath);
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
        Debug.Assert(stream.Position == 0, "Stream position should be at the beginning");
        
        // Gets past header and file list to start writing paths
        stream.Seek(Utils.Align16((int)(NotarHeader.HeaderSizeInBytes + FileListSize)), SeekOrigin.Begin); 
        
        // Writes all the paths
        foreach (NotarFile file in FileList.Files) 
        {
            var baseDirectoryPath = Path.GetDirectoryName(file.FilePath);
            
            file.SerializePath(stream, baseDirectoryPath);

            var path = file.FilePath;
        }
        
        Header.PayloadOffset = (uint)Utils.Align16((int)stream.Position);
        
        foreach (NotarFile file in FileList.Files)
        {
            stream.Seek(Utils.Align16((int)stream.Position), SeekOrigin.Begin);

            using BinaryWriter writer = new(stream, Encoding.UTF8, true);
            
            // capture the spot where the file starts
            file.ByteOffset = (ulong)stream.Position;

            byte[] payLoad = File.ReadAllBytes(file.FilePath);
            writer.Write(payLoad, 0, payLoad.Length); 
        }
        
        // Go back and write the File List
        stream.Seek(Utils.Align16((int)(NotarHeader.HeaderSizeInBytes)), SeekOrigin.Begin); // Gets past header to write file info
        Debug.Assert(stream.Position == 128);
        
        // Write the file info
        foreach (NotarFile file in FileList.Files)
        {
            file.Serialize(stream);
        }
        
        // back to the header
        stream.Seek(0, SeekOrigin.Begin);
        Debug.Assert(stream.Position == 0);

        Header.VersionMajor = 1;
        Header.VersionMinor = 1;
        Header.FileLayoutVersion = 1;
        Header.FeatureFlags = 1;
        Header.FileCount = FileList.Count;
        Header.FileListSize = FileList.FileListSize;
        Header.PayloadSize = (ulong)(stream.Length - Header.PayloadOffset);

        MakeHash(stream);

        Header.Serialize(stream);
        Debug.Assert(stream.Position == 128);
    }

    public void UnPack(string inputPath)
    {
        // Skip 128 bytes past header
        // Use header.FileCount to know how many NotarFiles to skip past on stream
        // Header | FileList | Paths | Payload data
        // Skip back and forth creating directories/files from paths then writing payload.
        using FileStream readStream = new(inputPath, FileMode.Open, FileAccess.Read);
        
        NotarPackage notarPackage = new NotarPackage();
        
        notarPackage.Header.Deserialize(readStream);

        List<NotarFile> files = new();

        for (var i = 0; i < notarPackage.Header.FileCount; i++)
        {
            // Creates empty NotarFile
            NotarFile file = new NotarFile();
            file.Deserialize(readStream);
            files.Add(file);
        }
        
        // Seek to the place the paths were written
        readStream.Seek(Utils.Align16((int)(NotarHeader.HeaderSizeInBytes + notarPackage.Header.FileListSize)), SeekOrigin.Begin); 
        
        // Deserialize file paths
        foreach (NotarFile file in files)
        {
            // Advances the stream to obtain each filepath
            file.DeserializePath(readStream);
            var path = file.FilePath;
            
            // Now add the complete file to my FileList
            notarPackage.FileList.Files.Add(file);
        }
        
        foreach (NotarFile file in notarPackage.FileList.Files)
        {
            FileStream writeStream = new(file.FilePath, FileMode.CreateNew, FileAccess.Write);
            
            // Create an empty byte[] to hold the payload I'm about to read
            byte[] payLoad = new byte[file.FileSize];
            
            // Read bytes from the readstream and store the amount of bytes read
            int bytesRead = readStream.Read(payLoad, 0, payLoad.Length);
            
            // Write the same amount of bytes to the write stream
            writeStream.Write(payLoad, 0, bytesRead);
        }
    }

    private byte[] ComputePayloadHash(Stream stream)
    {
        byte[] payload = new byte[Header.PayloadSize];
        
        // Seek to the start of the payload
        var initialPosition = stream.Position;
        stream.Seek(Header.PayloadOffset, SeekOrigin.Begin);
        stream.ReadExactly(payload, 0, (int)Header.PayloadSize);
        
        //using var sha256 = SHA256.Create();
        //byte[] payloadHash = sha256.ComputeHash(payload);
       
        byte[] payloadHash = SHA256.HashData(payload);
        
        stream.Seek(initialPosition, SeekOrigin.Begin);
        return payloadHash;
    }

    private void MakeHash(Stream stream)
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