using System.Text;

namespace NoTarUltimate;

public class NotarFile                          // A single file
{
    internal string FilePath { get; set; }
    internal ulong FileSize { get; set; }
    internal DateTime CreationTime { get; set; }
    internal DateTime LastModifiedTime { get; set; }
    internal FileAttributes FileAttributes { get; set; }
    private ulong ByteOffset { get; set; }
    


    public void Serialize(Stream stream)
    {
        using BinaryWriter writer = new(stream, Encoding.UTF8, true);
        writer.Write(FilePath);
        writer.Write(FileSize);
        writer.Write(CreationTime.ToBinary());
        writer.Write(LastModifiedTime.ToBinary());
        //writer.Write(CreationTime.ToString("yyyy-MM-dd HH:mm:ss"));
        //writer.Write(LastModifiedTime.ToString("yyyy-MM-dd HH:mm:ss"));
        foreach(FileAttributes attr in Enum.GetValues(typeof(FileAttributes)))
            writer.Write(attr.ToString());
        writer.Write(ByteOffset);
    }

    public void Deserialize(Stream stream)
    {
        using var reader = new BinaryReader(stream, Encoding.UTF8, true);
        FilePath = reader.ReadString();
        FileSize = reader.ReadUInt64();
        CreationTime = DateTime.FromBinary(reader.ReadInt64());
        LastModifiedTime = DateTime.FromBinary(reader.ReadInt64());
        FileAttributes = (FileAttributes)reader.ReadInt32();
        ByteOffset = reader.ReadUInt64();
    }
    
    
}