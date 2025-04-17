namespace NoTarUltimate;

internal class NotarFile
{
    internal string FilePath { get; set; }
    internal ulong FileSize { get; set; }
    internal DateTime CreationTime { get; set; }
    internal DateTime LastModifiedTime { get; set; }
    internal FileAttributes FileAttributes { get; set; }
    internal ulong ByteOffset { get; set; }
    


    public void Serialize(Stream stream)
    {
        throw new NotImplementedException();
    }

    public void Deserialize(Stream stream)
    {
        throw new NotImplementedException();
    }
    
    
}