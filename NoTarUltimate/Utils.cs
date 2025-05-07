namespace NoTarUltimate;

internal static class Utils
{
    internal static int Align16(int value)
    {
        // Takes the bitwise AND hexadecimal value of the current
        // stream.Position and aligns it to the next multiple of 16
        return (value + 0xF) & ~0xF;
    }

    public static void RunPack(string directoryPath, string notarOutput)
    {
        NotarPackage package = new NotarPackage()
            .FromDirectory(directoryPath);
        
        package.Pack(notarOutput);
    }
    
    public static void RunUnpack(string notarFile, string outputDirectory)
    {
        NotarPackage.ToDirectory(notarFile, outputDirectory);
    }
}