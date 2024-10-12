using System;
using System.IO;
using System.Threading.Tasks;

namespace grzyClothTool.Helpers;
public static class ObfuscationHelper
{
    private const int key = 0x2B;

    // very simple XOR "obfuscation" so file cannot be opened in file explorer easily
    public static async Task XORFile(string inputFile, string outputFile)
    {
        const int bufferSize = 1048576;

        await using FileStream fsInput = new(inputFile, FileMode.Open, FileAccess.Read, FileShare.Read);
        await using FileStream fsOutput = new(outputFile, FileMode.Create, FileAccess.Write, FileShare.None);
        byte[] buffer = new byte[bufferSize];
        int bytesRead;

        while ((bytesRead = await fsInput.ReadAsync(buffer.AsMemory(0, bufferSize))) > 0)
        {
            for (int i = 0; i < bytesRead; i++)
            {
                buffer[i] ^= key;
            }
            await fsOutput.WriteAsync(buffer.AsMemory(0, bytesRead));
        }
    }

    public static string HashString(string input)
    {
        byte[] data = System.Text.Encoding.UTF8.GetBytes(input);
        byte[] hash = System.Security.Cryptography.SHA256.HashData(data);
        return BitConverter.ToString(hash).Replace("-", string.Empty);
    }
}
