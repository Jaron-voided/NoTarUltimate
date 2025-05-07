namespace NoTarUltimate;

public static class CommandOrchestrator
{
    public static void Handle(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("No Command Provided!");
            return;
        }

        var command = args[0].ToLower();

        switch (command)
        {
            case "pack":
                HandlePack(args);
                break;
            
            case "unpack":
                HandleUnpack(args);
                break;
            
            default:
                Console.WriteLine($"Invalid Command! {command}");
                break;
        }
    }

    private static void HandlePack(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: Notar Pack <DirectoryPath> <NotarOutputPath");
            return;
        }

        Utils.RunPack(args[1], args[2]);
    }

    private static void HandleUnpack(string[] args)
    {
        if (args.Length < 3)
        {
            Console.WriteLine("Usage: Notar Unpack <InputFile> <OutputDirectory>");
            return;
        }
        
        Utils.RunUnpack(args[1], args[2]);
    }
}