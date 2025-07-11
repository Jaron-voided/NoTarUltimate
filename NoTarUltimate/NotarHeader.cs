using System.Text;

namespace NoTarUltimate;

internal class NotarHeader
{
    private const ulong MagicValue = 0x3037317261746F6E;  // 8 bytes "notar170" 
    internal const ushort HeaderSizeInBytes = 0x80;       // 2 bytes, header = 128 bytes total
                                                          // These are not serialized or counted

    private ulong _magic = MagicValue;                    // 8 bytes   
    private ushort _headerSize = HeaderSizeInBytes;       // 2 bytes                        
    internal byte VersionMajor { get; set;}               // 1 byte   
    internal byte VersionMinor { get; set;}               // 1 byte
    internal uint FileLayoutVersion { get; set; }         // 4 bytes
    internal ulong FeatureFlags { get; set; }             // 8 bytes
    internal uint FileCount { get; set; }                 // 4 bytes
    internal uint FileListSize { get; set; }              // 4 bytes
    internal uint PayloadOffset { get; set; }             // 4 bytes
    internal ulong PayloadSize { get; set; }              // 8 bytes
    internal PayloadHash PayloadHash { get; set; }        // 32 bytes
                                                          // ========  
                                                          // 76 bytes 
                                                          //  +
    private const int PaddingSize = 52;                   // 52 bytes
    
    public void Serialize(Stream stream)
    {
        using BinaryWriter writer = new(stream, Encoding.UTF8, true);
        writer.Write(_magic);
        writer.Write(_headerSize);
        writer.Write(VersionMajor);
        writer.Write(VersionMinor);
        writer.Write(FileLayoutVersion);
        writer.Write(FeatureFlags);
        writer.Write(FileCount);
        writer.Write(FileListSize);
        writer.Write(PayloadOffset);
        writer.Write(PayloadSize);
        PayloadHash.Serialize(stream);
        
        // Add padding to the end
        for (var i = 0; i < PaddingSize; i += 4)
        {
            // Writes 4 bytes of padding each loop
            writer.Write(0U);
        }
        
    }

    public void Deserialize(Stream stream)
    {
        using var reader = new BinaryReader(stream, Encoding.UTF8, true);
        _magic = reader.ReadUInt64();
        _headerSize = reader.ReadUInt16();
        VersionMajor = reader.ReadByte();
        VersionMinor = reader.ReadByte();
        FileLayoutVersion = reader.ReadUInt32();
        FeatureFlags = reader.ReadUInt64();
        FileCount = reader.ReadUInt32();
        FileListSize = reader.ReadUInt32();
        PayloadOffset = reader.ReadUInt32();
        PayloadSize = reader.ReadUInt64();
        PayloadHash.Deserialize(stream);

        // New Lines
        stream.Seek(52, SeekOrigin.Current);
    }

    // I need to use this IsValidHeader method places
    bool IsValidHeader()
    {
        return _magic == MagicValue && _headerSize == 0x80;
    }
}