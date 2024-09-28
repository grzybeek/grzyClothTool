using System.IO;

namespace grzyClothTool.Helpers;
public static class ObfuscationHelper
{
    private const int key = 0x2B;

    // very simple XOR "obfuscation" so file cannot be opened in file explorer easily
    public static void XORFile(string inputFile, string outputFile)
    {
        const int bufferSize = 1048576;

        using FileStream fsInput = new(inputFile, FileMode.Open, FileAccess.Read, FileShare.Read);
        using FileStream fsOutput = new(outputFile, FileMode.Create, FileAccess.Write, FileShare.None);
        byte[] buffer = new byte[bufferSize];
        int bytesRead;

        while ((bytesRead = fsInput.Read(buffer, 0, bufferSize)) > 0)
        {
            for (int i = 0; i < bytesRead; i++)
            {
                buffer[i] ^= key;
            }
            fsOutput.Write(buffer, 0, bytesRead);
        }
    }
}
