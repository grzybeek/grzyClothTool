using grzyClothTool.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;

namespace grzyClothTool.Helpers;

/// <summary>
/// Helper class for handling drag-and-drop operations from virtual file systems
/// </summary>
public static class DragDropHelper
{
    /// <summary>
    /// Extracts virtual files from IDataObject
    /// </summary>
    /// <param name="data">The IDataObject containing file information</param>
    /// <param name="fileExtensionFilter">Optional filter for file extensions</param>
    /// <returns>List of extracted file paths in temp directory</returns>
    public static async Task<List<string>> ExtractVirtualFilesAsync(IDataObject data, Func<string, bool>? fileExtensionFilter = null)
    {
        var extractedFiles = new List<string>();
        
        try
        {
            var format = "FileGroupDescriptorW";
            if (!data.GetDataPresent(format))
            {
                format = "FileGroupDescriptor";
                if (!data.GetDataPresent(format))
                    return extractedFiles;
            }

            if (data.GetData(format) is not MemoryStream descriptorStream)
                return extractedFiles;

            if (!data.GetDataPresent("FileContents"))
            {
                return extractedFiles;
            }

            var tempDir = Path.Combine(Path.GetTempPath(), "grzyClothTool_dragdrop", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            using var reader = new BinaryReader(descriptorStream);
            int fileCount = reader.ReadInt32();

            for (int i = 0; i < fileCount; i++)
            {
                try
                {
                    long startPos = 4 + (i * 592);
                    descriptorStream.Seek(startPos, SeekOrigin.Begin);

                    byte[] descriptorBytes = reader.ReadBytes(592);
                    
                    int filenameOffset = 72;
                    byte[] filenameBytes = new byte[Math.Min(520, descriptorBytes.Length - filenameOffset)];
                    Array.Copy(descriptorBytes, filenameOffset, filenameBytes, 0, filenameBytes.Length);
                    
                    int nullIndex = -1;
                    for (int j = 0; j < filenameBytes.Length - 1; j += 2)
                    {
                        if (filenameBytes[j] == 0 && filenameBytes[j + 1] == 0)
                        {
                            nullIndex = j;
                            break;
                        }
                    }
                    
                    string filename;
                    if (nullIndex > 0)
                    {
                        filename = System.Text.Encoding.Unicode.GetString(filenameBytes, 0, nullIndex);
                    }
                    else
                    {
                        filename = System.Text.Encoding.Unicode.GetString(filenameBytes).TrimEnd('\0');
                    }
                    
                    string safeFilename = filename;
                    int lastSlashIndex = Math.Max(filename.LastIndexOf('\\'), filename.LastIndexOf('/'));
                    if (lastSlashIndex >= 0)
                    {
                        safeFilename = filename.Substring(lastSlashIndex + 1);
                    }
                    
                    if (string.IsNullOrEmpty(safeFilename))
                    {
                        safeFilename = Path.GetFileName(filename);
                    }
                    
                    uint fileSizeLow = BitConverter.ToUInt32(descriptorBytes, 60);
                    uint fileSizeHigh = BitConverter.ToUInt32(descriptorBytes, 56);

                    if (fileExtensionFilter != null && !fileExtensionFilter(safeFilename))
                    {
                        continue;
                    }

                    MemoryStream? contentStream = null;

                    try
                    {
                        if (data is System.Runtime.InteropServices.ComTypes.IDataObject comData)
                        {
                            System.Runtime.InteropServices.ComTypes.FORMATETC formatEtc = new()
                            {
                                cfFormat = (short)DataFormats.GetDataFormat("FileContents").Id,
                                dwAspect = System.Runtime.InteropServices.ComTypes.DVASPECT.DVASPECT_CONTENT,
                                lindex = i,
                                ptd = IntPtr.Zero,
                                tymed = System.Runtime.InteropServices.ComTypes.TYMED.TYMED_ISTREAM | 
                                       System.Runtime.InteropServices.ComTypes.TYMED.TYMED_HGLOBAL
                            };

                            System.Runtime.InteropServices.ComTypes.STGMEDIUM medium = new();
                            comData.GetData(ref formatEtc, out medium);

                            if (medium.unionmember != IntPtr.Zero)
                            {
                                if (medium.tymed == System.Runtime.InteropServices.ComTypes.TYMED.TYMED_ISTREAM)
                                {
                                    contentStream = ReadFromIStream(medium.unionmember);
                                }
                                else if (medium.tymed == System.Runtime.InteropServices.ComTypes.TYMED.TYMED_HGLOBAL)
                                {
                                    contentStream = ReadFromHGlobal(medium.unionmember, (int)fileSizeLow);
                                }

                                if (medium.pUnkForRelease != null)
                                {
                                    Marshal.ReleaseComObject(medium.pUnkForRelease);
                                }
                            }
                        }
                    }
                    catch (Exception comEx)
                    {
                        LogHelper.Log($"Failed to extract file {i + 1} ({safeFilename}): {comEx.Message}", LogType.Error);
                    }

                    if (contentStream != null && contentStream.Length > 0)
                    {
                        var outputPath = Path.Combine(tempDir, safeFilename);

                        using (var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                        {
                            contentStream.Seek(0, SeekOrigin.Begin);
                            await contentStream.CopyToAsync(fileStream);
                        }

                        extractedFiles.Add(outputPath);
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Log($"Error extracting file {i + 1}: {ex.Message}", LogType.Error);
                    continue;
                }
            }
        }
        catch (Exception ex)
        {
            LogHelper.Log($"Error in ExtractVirtualFilesAsync: {ex.Message}", LogType.Error);
        }

        return extractedFiles;
    }

    /// <summary>
    /// Checks if the FileGroupDescriptor contains files matching the filter.
    /// </summary>
    /// <param name="data">The IDataObject containing file information</param>
    /// <param name="fileExtensionFilter">Filter function to check file extensions</param>
    /// <returns>True if matching files are found</returns>
    public static bool CheckForFilesInDescriptor(IDataObject data, Func<string, bool> fileExtensionFilter)
    {
        try
        {
            var format = "FileGroupDescriptorW";
            if (!data.GetDataPresent(format))
            {
                format = "FileGroupDescriptor";
                if (!data.GetDataPresent(format))
                    return false;
            }

            if (data.GetData(format) is not MemoryStream stream)
                return false;

            using var reader = new BinaryReader(stream);
            int fileCount = reader.ReadInt32();

            for (int i = 0; i < fileCount; i++)
            {
                stream.Seek(4 + (i * 592) + 72, SeekOrigin.Begin);

                // Read filename (520 bytes, Unicode)
                byte[] filenameBytes = reader.ReadBytes(520);
                
                // Find null terminator
                int nullIndex = -1;
                for (int j = 0; j < filenameBytes.Length - 1; j += 2)
                {
                    if (filenameBytes[j] == 0 && filenameBytes[j + 1] == 0)
                    {
                        nullIndex = j;
                        break;
                    }
                }
                
                string filename;
                if (nullIndex > 0)
                {
                    filename = System.Text.Encoding.Unicode.GetString(filenameBytes, 0, nullIndex);
                }
                else
                {
                    filename = System.Text.Encoding.Unicode.GetString(filenameBytes).TrimEnd('\0');
                }

                if (fileExtensionFilter(filename))
                {
                    return true;
                }
            }

            return false;
        }
        catch (Exception ex)
        {
            LogHelper.Log($"Error checking FileGroupDescriptor: {ex.Message}", LogType.Error);
            return false;
        }
    }

    /// <summary>
    /// Reads content from an IStream COM object.
    /// </summary>
    private static MemoryStream ReadFromIStream(IntPtr streamPtr)
    {
        if (Marshal.GetObjectForIUnknown(streamPtr) is not System.Runtime.InteropServices.ComTypes.IStream iStream)
            return new MemoryStream();

        var contentStream = new MemoryStream();
        byte[] buffer = new byte[4096];
        IntPtr pcbRead = Marshal.AllocHGlobal(sizeof(int));
        
        try
        {
            int bytesRead;
            do
            {
                iStream.Read(buffer, buffer.Length, pcbRead);
                bytesRead = Marshal.ReadInt32(pcbRead);
                if (bytesRead > 0)
                {
                    contentStream.Write(buffer, 0, bytesRead);
                }
            } while (bytesRead > 0);
        }
        finally
        {
            Marshal.FreeHGlobal(pcbRead);
        }

        contentStream.Seek(0, SeekOrigin.Begin);
        return contentStream;
    }

    /// <summary>
    /// Reads content from an HGLOBAL memory handle.
    /// </summary>
    private static MemoryStream ReadFromHGlobal(IntPtr hGlobalPtr, int size)
    {
        var ptr = Marshal.ReadIntPtr(hGlobalPtr);
        byte[] buffer = new byte[size];
        Marshal.Copy(ptr, buffer, 0, size);
        return new MemoryStream(buffer);
    }

    /// <summary>
    /// Validates that dropped files are accessible on the file system.
    /// </summary>
    /// <param name="files">List of file paths to validate</param>
    /// <returns>Tuple containing accessible and inaccessible file lists</returns>
    public static (List<string> accessible, List<string> inaccessible) ValidateFileAccess(IEnumerable<string> files)
    {
        var accessibleFiles = new List<string>();
        var inaccessibleFiles = new List<string>();

        foreach (var file in files)
        {
            if (File.Exists(file))
            {
                accessibleFiles.Add(file);
            }
            else
            {
                inaccessibleFiles.Add(file);
            }
        }

        return (accessibleFiles, inaccessibleFiles);
    }

    /// <summary>
    /// Creates a filter function for checking file extensions.
    /// </summary>
    /// <param name="extensions">Array of extensions to check (e.g., ".ydd", ".ytd")</param>
    /// <returns>Filter function</returns>
    public static Func<string, bool> CreateExtensionFilter(params string[] extensions)
    {
        return filePath =>
        {
            var extension = Path.GetExtension(filePath);
            return extensions.Any(ext => ext.Equals(extension, StringComparison.OrdinalIgnoreCase));
        };
    }
}
