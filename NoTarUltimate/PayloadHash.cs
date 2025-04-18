using System.Text;

namespace NoTarUltimate;

internal struct PayloadHash
{
    private ulong PayloadHashPartA { get; set; }  // 8 bytes
    private ulong PayloadHashPartB { get; set; }  // 8 bytes
    private uint PayloadHashPartC { get; set; }  // 4 bytes

    internal void Serialize(Stream stream)
    {
        using BinaryWriter writer = new(stream, Encoding.UTF8, true);
        writer.Write(PayloadHashPartA);
        writer.Write(PayloadHashPartB);
        writer.Write(PayloadHashPartC);
    }    
    
    internal void Deserialize(Stream stream)
    {
        using BinaryReader reader = new(stream, Encoding.UTF8, true);
        PayloadHashPartA = reader.ReadUInt64();
        PayloadHashPartB = reader.ReadUInt64();
        PayloadHashPartC = reader.ReadUInt32();
    }

}