namespace NoTarUltimate;

static class Utils
{
    static int Align16(int value)
    {
        return (value + 0xF) & ~0xF;
    }

    static NotarPackage Pack(Stream stream)
    {
        NotarPackage package = new NotarPackage();
        // Create NotarPackage, run NotarPackage.Factory for each piece
        // Then run NotarPackage.Item[i].Serialize
        // ??
        return package;
    }

    static void Unpack(Stream stream, NotarPackage package)
    {
        //This function will create all the directories and files from this
        // These 2 functions will have to use Align16
    }
}