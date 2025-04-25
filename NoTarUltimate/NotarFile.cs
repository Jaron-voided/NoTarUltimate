using System.Text;

namespace NoTarUltimate;

// A single file
public class NotarFile                          
{
    internal string FilePath { get; set; }
    internal ulong FileSize { get; set; }
    internal DateTime CreationTime { get; set; }
    internal DateTime LastModifiedTime { get; set; }
    internal uint FileAttributes { get; set; }
    internal ulong ByteOffset { get; set; }
    
    internal const uint NotarFileInfoSize = 0x30;
    


    public void Serialize(Stream stream) // Writes/Serializes the data into bytes/a file
    {
        using BinaryWriter writer = new(stream, Encoding.UTF8, true);
        // writer.Write(FilePath);
        writer.Write(FileSize);
        writer.Write(CreationTime.ToBinary());
        writer.Write(LastModifiedTime.ToBinary());
        writer.Write(FileAttributes);
        writer.Write(ByteOffset);
    }

    public void SerializePath(Stream stream, string relativeTo)
    {
        using BinaryWriter writer = new(stream, Encoding.UTF8, true);
        var relativePath = Path.GetRelativePath(relativeTo, FilePath);
        // Turn the relative path into a byte array
        //var relativePathBytes = Encoding.UTF8.GetBytes(relativePath);
        //writer.Write((ushort)relativePathBytes.Length);
        writer.Write(relativePath);
    }

    public void Deserialize(Stream stream) // Deserializes the data, is it really this simple for these 2?
    {
        using var reader = new BinaryReader(stream, Encoding.UTF8, true);
        FilePath = reader.ReadString();
        FileSize = reader.ReadUInt64();
        CreationTime = DateTime.FromBinary(reader.ReadInt64());
        LastModifiedTime = DateTime.FromBinary(reader.ReadInt64());
        FileAttributes = (uint)reader.ReadInt32();
        ByteOffset = reader.ReadUInt64();
    }

    public void DeserializePath(Stream stream)
    {
        using var reader = new BinaryReader(stream, Encoding.UTF8, true);
        FilePath = reader.ReadString();
    }
}