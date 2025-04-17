namespace NoTarUltimate;

internal class NotarFileInfo
{
    internal string FilePath { get; set; }
    internal ulong FileSize { get; set; }
    internal DateTime CreationTime { get; set; }
    internal DateTime LastModifiedTime { get; set; }
    internal FileAttributes FileAttributes { get; set; }
    internal ulong ByteOffset { get; set; }
    
    public NotarFileInfo FileFactory(FileStream file)
    {
        throw new NotImplementedException();
    }

    public Memory<byte> Serialize(Stream stream)
    {
        try
        {
            throw new NotImplementedException();
        }
        catch
        {
            throw new NotImplementedException();
        }
    }

    public NotarFileInfo Deserialize(Stream stream)
    {
        try
        {
            throw new NotImplementedException();
        }
        catch
        {
            throw new NotImplementedException();
        }
    }
}