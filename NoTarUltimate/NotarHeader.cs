using System.Text;

namespace NoTarUltimate;

internal class NotarHeader
{
    const ulong MagicValue = 0x3037317261746F6E; // "notar170"
    const ushort HeaderSizeInBytes = 0x80;       // 128 bytes
    
    ulong Magic = MagicValue;                    // 8 bytes   
    ushort HeaderSize = HeaderSizeInBytes;       // 2 bytes                        
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
    const int PaddingSize = 68;                 // 68 bytes

    public NotarHeader HeaderFactory(NotarFileList files)
    {
        throw new NotImplementedException();
    }
    
    public Memory<byte> Serialize(Stream stream)
    {
        
        byte[] serializedHeader = new byte[HeaderSize];
        try
        {
            using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                writer.Write(Magic);
                writer.Write(HeaderSize);
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
                writer.Write(PaddingSize);
            }

            stream.Write(serializedHeader, 0, HeaderSize);
            return new Memory<byte>(serializedHeader);
        }
        catch(Exception e)
        {
            throw new Exception("Could not write header: " + e.Message);
        }
    }

    public NotarHeader Deserialize(Stream stream)
    {
        NotarHeader header = new NotarHeader();
        try
        {
            using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8, true))
            {
                header.Magic = reader.ReadUInt64();
                header.HeaderSize = reader.ReadUInt16();
                header.VersionMajor = reader.ReadByte();
                header.VersionMinor = reader.ReadByte();
                header.FileLayoutVersion = reader.ReadUInt32();
                header.FeatureFlags = reader.ReadUInt64();
                header.DirectoryCount = reader.ReadUInt16();
                header.FileCount = reader.ReadUInt32();
                header.FileListSize = reader.ReadUInt32();
                header.PayloadOffset = reader.ReadUInt32();
                header.PayloadSize = reader.ReadUInt32();
                header.PayloadHash = reader.ReadUInt64();
            }
            if (header.IsValidHeader())
                return header;
            else
                throw new InvalidDataException("Header is invalid");
        }
        
        catch(Exception e)
        {
            throw new Exception("Could not read header: " + e.Message);
        }
    }

    public bool IsValidHeader()
    {
        return Magic == MagicValue && HeaderSize == 0x80;
    }
}