using System.Text;
using System.Diagnostics;


namespace NoTarUltimate;

// A single file
public class NotarFile                          
{
    // These are set with NotarFileList.AddFile
    internal string FilePath { get; set; }
    internal ulong FileSize { get; set; }                       // 8 bytes
    internal DateTime CreationTime { get; set; }                // 8 bytes
    internal DateTime LastModifiedTime { get; set; }            // 8 bytes
    internal uint FileAttributes { get; set; }                  // 4 bytes
    
    // ByteOffset is set in Pack
    internal ulong ByteOffset { get; set; }                     // 8 bytes
    
    internal const uint NotarFileInfoSize = 0x30; // 48 bytes
    


    public void Serialize(Stream stream) // Writes/Serializes the data into bytes/a file
    {
        long start = stream.Position;
        using BinaryWriter writer = new(stream, Encoding.UTF8, true);
        writer.Write(FileSize);
        writer.Write(CreationTime.ToBinary());
        writer.Write(LastModifiedTime.ToBinary());
        writer.Write(FileAttributes);
        writer.Write(ByteOffset);
        
        // Pad the rest of the NotarFileInfoSize
        stream.Seek(NotarFileInfoSize - (stream.Position - start), SeekOrigin.Current);
        Debug.Assert(stream.Position - 48 == start); // These ended up in the correct positions

    }

    public void SerializePath(Stream stream, string relativeTo)
    {
        using BinaryWriter writer = new(stream, Encoding.UTF8, true);
        var relativePath = Path.GetRelativePath(relativeTo, FilePath);
        
        var relativePathBytes = Encoding.UTF8.GetBytes(relativePath);
        // Turn the relative path into a byte array
        
        
        //writer.Write((ushort)relativePathBytes.Length);
        writer.Write(relativePathBytes);
    }
    public void Deserialize(Stream stream) // Deserializes the data, is it really this simple for these 2?
    {
        var start = stream.Position;
        using var reader = new BinaryReader(stream, Encoding.UTF8, true);
        FilePath = reader.ReadString();
        FileSize = reader.ReadUInt64();
        CreationTime = DateTime.FromBinary(reader.ReadInt64());
        LastModifiedTime = DateTime.FromBinary(reader.ReadInt64());
        FileAttributes = (uint)reader.ReadInt32();
        ByteOffset = reader.ReadUInt64();
        stream.Seek(NotarFileInfoSize - (stream.Position - start), SeekOrigin.Current);
    }
    public void DeserializePath(Stream stream)
    {
        using var reader = new BinaryReader(stream, Encoding.UTF8, true);
        var filePathLength = reader.ReadUInt16();
        FilePath = Encoding.UTF8.GetString(reader.ReadBytes(filePathLength));
    }
}