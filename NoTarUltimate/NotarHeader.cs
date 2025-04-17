using System.Text;

namespace NoTarUltimate;

internal class NotarHeader
{
    const ulong MagicValue = 0x3037317261746F6E; // "notar170"
    const ushort HeaderSizeInBytes = 0x80;       // 128 bytes
    
    ulong _magic = MagicValue;                    // 8 bytes   
    ushort _headerSize = HeaderSizeInBytes;       // 2 bytes                        
    internal byte VersionMajor { get; set;}               // 1 byte   
    internal byte VersionMinor { get; set;}               // 1 byte
    internal uint FileLayoutVersion { get; set; }         // 4 bytes
    internal ulong FeatureFlags { get; set; }             // 8 bytes
    internal ushort DirectoryCount { get; set; }         // 4 bytes
    internal uint FileCount { get; set; }                // 4 bytes
    internal uint FileListSize { get; set; }             // 4 bytes
    internal uint PayloadOffset { get; set; }            // 4 bytes
    internal uint PayloadSize { get; set; }             // 4 bytes
    internal ulong PayloadHash { get; set; }             // 20 bytes
    const int PaddingSize = 68;                         // 68 bytes
    
    public void Serialize(Stream stream)
    {
        using BinaryWriter writer = new(stream, Encoding.UTF8, true);
        writer.Write(_magic);
        writer.Write(_headerSize);
        writer.Write(VersionMajor);
        writer.Write(VersionMinor);
        writer.Write(FileLayoutVersion);
        writer.Write(FeatureFlags);
        writer.Write(DirectoryCount);
        writer.Write(FileCount);
        writer.Write(FileListSize);
        writer.Write(PayloadOffset);
        writer.Write(PayloadSize);
        writer.Write(PayloadHash);
        
        // Add padding to the end
        for (int i = 0; i < PaddingSize; i += 4)
        {
            writer.Write(0U);
        }
        
    }

    public void Deserialize(Stream stream)
    {
        NotarHeader header = new NotarHeader();

        using BinaryReader reader = new BinaryReader(stream, Encoding.UTF8, true);
        _magic = reader.ReadUInt64();
        _headerSize = reader.ReadUInt16();
        VersionMajor = reader.ReadByte();
        VersionMinor = reader.ReadByte();
        FileLayoutVersion = reader.ReadUInt32();
        FeatureFlags = reader.ReadUInt64();
        DirectoryCount = reader.ReadUInt16();
        FileCount = reader.ReadUInt32();
        FileListSize = reader.ReadUInt32();
        PayloadOffset = reader.ReadUInt32();
        PayloadSize = reader.ReadUInt32();
        PayloadHash = reader.ReadUInt64();
    }

    bool IsValidHeader()
    {
        return _magic == MagicValue && _headerSize == 0x80;
    }
}