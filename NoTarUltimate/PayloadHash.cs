using System.Text;

namespace NoTarUltimate;

internal struct PayloadHash
{
    internal ulong PayloadHashPartA { get; set; }  // 8 bytes
    internal ulong PayloadHashPartB { get; set; }  // 8 bytes
    internal ulong PayloadHashPartC { get; set; }  // 8 bytes
    internal ulong PayloadHashPartD { get; set; }  // 8 bytes

    internal void Serialize(Stream stream)
    {
        using BinaryWriter writer = new(stream, Encoding.UTF8, true);
        writer.Write(PayloadHashPartA);
        writer.Write(PayloadHashPartB);
        writer.Write(PayloadHashPartC);
        writer.Write(PayloadHashPartD);
    }    
    
    internal void Deserialize(Stream stream)
    {
        using BinaryReader reader = new(stream, Encoding.UTF8, true);
        PayloadHashPartA = reader.ReadUInt64();
        PayloadHashPartB = reader.ReadUInt64();
        PayloadHashPartC = reader.ReadUInt64();
        PayloadHashPartD = reader.ReadUInt64();
    }
}