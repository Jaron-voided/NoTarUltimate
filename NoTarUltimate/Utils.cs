namespace NoTarUltimate;

static class Utils
{
    internal static int Align16(int value)
    {
        return (value + 0xF) & ~0xF;
    }
}