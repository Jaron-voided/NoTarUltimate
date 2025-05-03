using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace NoTarUltimate;

public class NotarPackage // This is the final object. It holds the header and any nested directories
{
    NotarHeader Header { get; set; } = new NotarHeader(); // This instantiates Header when I construct a package
    NotarFileList FileList { get; set; } = new NotarFileList();
    public string RelativeTo { get; set; } = string.Empty; // changed this from init to stop the FromDirectory RelativeTo error...

    private uint FileListSize => FileList.FileListSize;

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
        
        this.RelativeTo = directoryPath;

        foreach (var file in Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories))
        {
            this.FileList.AddFile(file);
        }
        return this;
    }

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
        // Debug.Assert((stream.Position - HeaderSizeInBytes) // 48 == 0)
        // The position should now be advanced header size in bytes, plus some multiple of FileListSize(48)
        
        // Writes all the paths
        // Debugging shows this starting at 224, (48 * 2) + 128
        foreach (NotarFile file in FileList.Files) 
        {
            // Have I already gotten this information, or is it set in another function?
            // Or do I need to do this manually
            // Is the NotarPackage prepared in any sort before this function?
            var baseDirectoryPath = Path.GetDirectoryName(file.FilePath);
            
            file.SerializePath(stream, baseDirectoryPath);

            var path = file.FilePath; // Paths seem to move the stream 3 spots
        }
        
        // The stream should now be to the payload data spot
        // Stream position is at 230, Align16 moves it to 240
        // 
        Header.PayloadOffset = (uint)Utils.Align16((int)stream.Position);
        foreach (NotarFile file in FileList.Files)
        {
            // second time around is 252
            stream.Seek(Utils.Align16((int)stream.Position), SeekOrigin.Begin);

            using BinaryWriter writer = new(stream, Encoding.UTF8, true); // second one starts at 256
            // capture the spot where the file starts
            file.ByteOffset = (ulong)stream.Position; //first byteoffset is 240 for file one

            byte[] payLoad = File.ReadAllBytes(file.FilePath); // first payload is 12?
            writer.Write(payLoad, 0, payLoad.Length); //
        }
        
        // Go back and write the File List
        stream.Seek(Utils.Align16((int)(NotarHeader.HeaderSizeInBytes)), SeekOrigin.Begin); // Gets past header to write file info

        Debug.Assert(stream.Position == 128);
        // Write the file info
        foreach (NotarFile file in FileList.Files)
        {
            //var position = stream.Position;
            file.Serialize(stream);
            //Debug.Assert(stream.Position - 48 == position); // These ended up in the correct positions
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
        // Use header.FileCount to know how many NotarFiles to skip past on stream??
        // Header | FileList | Paths | Payload data
        // Skip back and forth creating directories/files from paths then writing payload.
        using FileStream readStream = new(inputPath, FileMode.Open, FileAccess.Read);
        
        NotarPackage notarPackage = new NotarPackage();
        
        notarPackage.Header.Deserialize(readStream);
        // !!!!!!needs to go to 128

        List<NotarFile> files = new();

        for (int i = 0; i < notarPackage.Header.FileCount; i++)
        {
            // Creates empty NotarFile
            NotarFile file = new NotarFile();
            file.Deserialize(readStream);
            // !!!!!!!!!!!!Needs to go 48
            // Adds the file to FileList, there's not a path yet so I can't run 
            files.Add(file);
        }
        
        // Seek to the place the paths were written
        //readStream.Seek(Utils.Align16((int)(NotarHeader.HeaderSizeInBytes + FileListSize)), SeekOrigin.Begin); 
        readStream.Seek(Utils.Align16((int)(NotarHeader.HeaderSizeInBytes + notarPackage.Header.FileListSize)), SeekOrigin.Begin); 


        // Deserialize file paths
        foreach (NotarFile file in files)
        {
            // Advances the stream to obtain each filepath
            file.DeserializePath(readStream);
            var path = file.FilePath;
            // Now add the complete file to my FileList
            //notarPackage.FileList.AddFile(file.FilePath);
            notarPackage.FileList.Files.Add(file);

        }
        
        foreach (NotarFile file in notarPackage.FileList.Files)
        {
            FileStream newFile = new FileStream(file.FilePath, FileMode.CreateNew, FileAccess.Write);
            
            // Create an empty byte[] to hold the payload I'm about to read
            byte[] payLoad = new byte[file.FileSize];
            
            int bytesRead = readStream.Read(payLoad, 0, payLoad.Length);
            newFile.Write(payLoad, 0, bytesRead);
        }
    }

    private byte[] ComputePayloadHash(Stream stream)
    {
        // Skip to payload Offset, then go forward PayloadSize
        byte[] payload = new byte[Header.PayloadSize];
        
        // Seek to the start of the payload
        var initialPosition = stream.Position;
        stream.Seek(Header.PayloadOffset, SeekOrigin.Begin);
        stream.ReadExactly(payload, 0, (int)Header.PayloadSize);
        using var sha256 = SHA256.Create();
        byte[] payloadHash = sha256.ComputeHash(payload);
        
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