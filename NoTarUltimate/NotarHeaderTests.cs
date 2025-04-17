using NUnit.Framework;
using System.IO;

namespace NoTarUltimate;

[TestFixture]
public class NotarHeaderTests
{
    [Test]
    public void SerializeDeserialize()
    {
        // Arrange
        var originalHeader = new NotarHeader
        {
            VersionMajor = 1,
            VersionMinor = 0,
            FileLayoutVersion = 1,
            FeatureFlags = 0,
            DirectoryCount = 2,
            FileCount = 10,
            FileListSize = 1024,
            PayloadOffset = 128,
            PayloadSize = 2048,
            PayloadHash = 1234567890
        };

        
        // Act
        using MemoryStream stream = new MemoryStream();
        originalHeader.Serialize(stream);
        stream.Position = 0;
        NotarHeader deserializedHeader = new NotarHeader();
        deserializedHeader.Deserialize(stream);

        // Assert
        Assert.That(originalHeader, Is.EqualTo(deserializedHeader));
    }
}