using System.Text;

namespace NoTarUltimate;

public class NotarFile                          // A single file
{
    internal string FilePath { get; set; }
    internal ulong FileSize { get; set; }
    internal DateTime CreationTime { get; set; }
    internal DateTime LastModifiedTime { get; set; }
    internal uint FileAttributes { get; set; } // Unsure what the data type should be since "FileAttribute" doesn't work with BinaryWriter
    internal ulong ByteOffset { get; set; } // How many bytes from the byte start of the file to the start of the Payload
    internal bool IsDirectory  { get; set; }
    


    public void Serialize(Stream stream) // Writes/Serializes the data into bytes/ a file
    {
        using BinaryWriter writer = new(stream, Encoding.UTF8, true);
        writer.Write(FilePath);
        writer.Write(FileSize);
        writer.Write(CreationTime.ToBinary());
        writer.Write(LastModifiedTime.ToBinary());
        writer.Write(FileAttributes);
        writer.Write(ByteOffset);
        writer.Write(IsDirectory);
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
        IsDirectory = reader.ReadBoolean();
    }
}