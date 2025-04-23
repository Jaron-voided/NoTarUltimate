namespace NoTarUltimate;

static class Utils
{
    internal static int Align16(int value)
    {
        return (value + 0xF) & ~0xF;
    }

    static NotarPackage Pack(Stream stream, string directoryPath) // Pack a directory and return a NotarPackage
    {
        NotarPackage package = new NotarPackage();
        DirectoryInfo directory = new DirectoryInfo(directoryPath);
        List<NotarFile> files = new List<NotarFile>();
        stream.Position = 129; // Move forward to save room for the header
        foreach (var file in Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories))
        {
            NotarFile notarFile = package.FileFactory(file, stream);
            notarFile.Serialize(stream);
            stream.Write(File.ReadAllBytes(file)); // Add the files to the stream
            files.Add(notarFile);
            stream.Position = Utils.Align16((int)stream.Position);
        }
        
        package.FileListFactory(files);

        stream.Position = 0; // Reset to the beginning to add the 128 byte header
        
        package.HeaderFactory(directoryPath);
        
        return package;
    }

    static void Unpack(Stream stream, NotarPackage package)
    {
        stream.Position = 129;
        foreach (NotarFile file in package.FileList.Files)
        {
            if (file.IsDirectory) // Checks if the file is a directory
            {
                if (!Directory.Exists(file.FilePath)) // and not already created
                {
                    Directory.CreateDirectory(file.FilePath); // Creates the directory
                }
            }
            else
            {
                stream.Position += (long)file.ByteOffset; //Sets the stream to where the file payload is
                byte[] buffer = new byte[file.FileSize]; // know how long to read for
                File.WriteAllBytes(file.FilePath, buffer); // Creates a file and writes to it for as bytes = buffer
            }
        }
        
        
    }
}