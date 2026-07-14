using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using grzyClothTool.Constants;
using grzyClothTool.Models.Drawable;
using grzyClothTool.Models.Texture;
using grzyClothTool.Views;
using ImageMagick;

namespace grzyClothTool.Helpers;

public static class FileHelper
{
    /// <summary>
    /// Temporary context for resolving file paths during save file loading
    /// This helps resolve relative paths when ProjectName isn't set yet
    /// </summary>
    private static string? _loadContextProjectFolder = null;

    /// <summary>
    /// Sets the project folder context for file path resolution during loading
    /// </summary>
    public static void SetLoadContext(string saveFilePath)
    {
        try
        {
            // Extract project folder from save file path
            // Expected: MainProjectsFolder\ProjectName\autosave.json
            var saveFileDir = Path.GetDirectoryName(saveFilePath);
            if (!string.IsNullOrEmpty(saveFileDir) && Directory.Exists(saveFileDir))
            {
                _loadContextProjectFolder = saveFileDir;
            }
        }
        catch
        {
            _loadContextProjectFolder = null;
        }
    }

    /// <summary>
    /// Clears the load context after loading is complete
    /// </summary>
    public static void ClearLoadContext()
    {
        _loadContextProjectFolder = null;
    }
    public static string ReservedAssetsPath { get; private set; }

    private static byte[] _cachedReservedDrawableBytes;
    private static long _cachedReservedDrawableLength;

    /// <summary>
    /// Gets the full path to the project assets directory for the current project
    /// </summary>
    public static string GetProjectAssetsPath()
    {
        var mainProjectsFolder = PersistentSettingsHelper.Instance.MainProjectsFolder;
        var projectName = MainWindow.AddonManager.ProjectName;

        if (string.IsNullOrEmpty(mainProjectsFolder) || string.IsNullOrEmpty(projectName))
        {
            throw new InvalidOperationException("Main projects folder or project name is not set.");
        }

        var assetsPath = Path.Combine(mainProjectsFolder, projectName, GlobalConstants.ASSETS_FOLDER_NAME);
        Directory.CreateDirectory(assetsPath);
        return assetsPath;
    }

    /// <summary>
    /// Resolves a relative path (stored in drawable/texture) to an absolute path
    /// </summary>
    public static string ResolveFilePath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return path;

        // If it's already an absolute path, check if it exists first (backward compatibility)
        if (Path.IsPathRooted(path))
        {
            // Old save format: absolute paths - check if file exists
            if (File.Exists(path))
            {
                return path;
            }
            
            // If absolute path doesn't exist, it might be from an old save that needs migration
            // Try to extract just the filename and look in project assets
            try
            {
                var fileName = Path.GetFileName(path);
                var assetsPath = GetProjectAssetsPath();
                var newPath = Path.Combine(assetsPath, fileName);
                
                if (File.Exists(newPath))
                {
                    return newPath;
                }
            }
            catch
            {
                // Fall through to return original path
            }
            
            return path;
        }

        // It's a relative path, resolve it from project assets
        try
        {
            // First, check if we have a load context (during save file loading)
            if (_loadContextProjectFolder != null)
            {
                var contextAssetsPath = Path.Combine(_loadContextProjectFolder, GlobalConstants.ASSETS_FOLDER_NAME);
                if (Directory.Exists(contextAssetsPath))
                {
                    var contextResolvedPath = Path.Combine(contextAssetsPath, path);
                    if (File.Exists(contextResolvedPath))
                    {
                        return contextResolvedPath;
                    }
                }
            }

            // Second, try to get the project assets path from the project structure
            var assetsPath = GetProjectAssetsPath();
            var resolvedPath = Path.Combine(assetsPath, path);
            
            // Verify the file exists
            if (File.Exists(resolvedPath))
            {
                return resolvedPath;
            }
            
            // If not found and path looks like a GUID-based filename, search in all project folders
            if (Guid.TryParse(Path.GetFileNameWithoutExtension(path), out _))
            {
                var mainProjectsFolder = PersistentSettingsHelper.Instance.MainProjectsFolder;
                if (!string.IsNullOrEmpty(mainProjectsFolder) && Directory.Exists(mainProjectsFolder))
                {
                    // Search all project subfolders for the file
                    foreach (var projectDir in Directory.GetDirectories(mainProjectsFolder))
                    {
                        var projectAssetsPath = Path.Combine(projectDir, GlobalConstants.ASSETS_FOLDER_NAME);
                        if (Directory.Exists(projectAssetsPath))
                        {
                            var searchPath = Path.Combine(projectAssetsPath, path);
                            if (File.Exists(searchPath))
                            {
                                return searchPath;
                            }
                        }
                    }
                }
            }
            
            // If still not found, return the original path (might be relative to current directory)
            return path;
        }
        catch (Exception ex)
        {
            LogHelper.Log($"Error resolving file path '{path}': {ex.Message}", Views.LogType.Warning);
            return path;
        }
    }

    /// <summary>
    /// Copies a file to the project assets folder and returns the relative path
    /// </summary>
    public static async Task<string> CopyToProjectAssetsAsync(string sourceFilePath, string guid)
    {
        var assetsPath = GetProjectAssetsPath();
        var extension = Path.GetExtension(sourceFilePath);
        var fileName = $"{guid}{extension}";
        var destinationPath = Path.Combine(assetsPath, fileName);

        // Only copy if destination doesn't exist
        if (!File.Exists(destinationPath))
        {
            await CopyAsync(sourceFilePath, destinationPath);
        }

        return fileName; // Return relative path
    }

    public static async Task<string> CopyToProjectAssetsWithReplaceAsync(string sourceFilePath, string fileNameWithoutExtension)
    {
        var assetsPath = GetProjectAssetsPath();
        var extension = Path.GetExtension(sourceFilePath);
        var fileName = $"{fileNameWithoutExtension}{extension}";
        var destinationPath = Path.Combine(assetsPath, fileName);

        if (File.Exists(destinationPath))
        {
            File.Delete(destinationPath);
        }

        await CopyAsync(sourceFilePath, destinationPath);

        return fileName;
    }

    public static void GenerateReservedAssets()
    {
        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var exeName = Assembly.GetExecutingAssembly().GetName().Name;

        ReservedAssetsPath = Path.Combine(documentsPath, exeName, "reservedAssets");
        Directory.CreateDirectory(ReservedAssetsPath);
        CreateReservedAsset("reservedDrawable", ".ydd");
        CreateReservedAsset("reservedTexture", ".ytd");
    }

    private static void CreateReservedAsset(string name, string extension)
    {
        var outputPath = Path.Combine(ReservedAssetsPath, name + extension);

        if(!File.Exists(outputPath))
        {
            byte[] resourceData = (byte[])Properties.Resources.ResourceManager.GetObject(name, CultureInfo.InvariantCulture);
            if (resourceData != null)
            {
                File.WriteAllBytes(outputPath, resourceData);
                return;
            }
        }
    }

    public static async Task<bool> IsReservedDrawable(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
                return false;

            var reservedDrawablePath = Path.Combine(ReservedAssetsPath, "reservedDrawable.ydd");
            if (!File.Exists(reservedDrawablePath))
                return false;

            var fileInfo = new FileInfo(filePath);

            // Initialize cache if needed
            if (_cachedReservedDrawableBytes == null)
            {
                var reservedFileInfo = new FileInfo(reservedDrawablePath);
                _cachedReservedDrawableLength = reservedFileInfo.Length;
                _cachedReservedDrawableBytes = await ReadAllBytesAsync(reservedDrawablePath);
            }

            if (fileInfo.Length != _cachedReservedDrawableLength)
                return false;

            var fileBytes = await ReadAllBytesAsync(filePath);
            return fileBytes.SequenceEqual(_cachedReservedDrawableBytes);
        }
        catch
        {
            return false;
        }
    }

    public static async Task<GDrawable> CreateDrawableAsync(string filePath, Enums.SexType sex, bool isProp, int typeNumber, int countOfType, bool useFolderCache = false)
    {
        var isReserved = await IsReservedDrawable(filePath);
        if (isReserved)
        {
            return new GDrawableReserved(sex, isProp, typeNumber, countOfType);
        }

        var name = EnumHelper.GetName(typeNumber, isProp);

        var matchingTextures = FindMatchingTextures(filePath, name, isProp, useFolderCache);

        var drawableGuid = Guid.NewGuid();
        var drawableRaceSuffix = Path.GetFileNameWithoutExtension(filePath)[^1..];
        var drawableHasSkin = drawableRaceSuffix == "r";

        string drawablePath;
        bool isExternal = MainWindow.AddonManager.IsExternalProject;
        if (isExternal)
        {
            drawablePath = filePath;
        }
        else
        {
            try
            {
                drawablePath = await CopyToProjectAssetsAsync(filePath, drawableGuid.ToString());
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Failed to copy drawable to project assets: {ex.Message}. Using original path.", LogType.Warning);
                drawablePath = filePath; // Fallback to original path
            }
        }

        // Process texture files
        var texturesList = new List<(string path, int txtNumber)>();
        for (int txtNumber = 0; txtNumber < Math.Min(matchingTextures.Count, GlobalConstants.MAX_DRAWABLE_TEXTURES); txtNumber++)
        {
            var texturePath = matchingTextures[txtNumber];
            
            if (isExternal)
            {
                texturesList.Add((texturePath, txtNumber));
            }
            else
            {
                var textureGuid = Guid.NewGuid();
                try
                {
                    var textureRelativePath = await CopyToProjectAssetsAsync(texturePath, textureGuid.ToString());
                    texturesList.Add((textureRelativePath, txtNumber));
                }
                catch (Exception ex)
                {
                    LogHelper.Log($"Failed to copy texture to project assets: {ex.Message}. Using original path.", LogType.Warning);
                    texturesList.Add((texturePath, txtNumber)); // Fallback to original path
                }
            }
        }

        // Create texture objects
        var textures = new ObservableCollection<GTexture>(
            texturesList.Select(t => new GTexture(Guid.Empty, t.path, typeNumber, countOfType, t.txtNumber, drawableHasSkin, isProp))
        );

        var drawable = new GDrawable(drawableGuid, drawablePath, sex, isProp, typeNumber, countOfType, drawableHasSkin, textures);
        return drawable;
    }

    public static Task CopyAsync(string sourcePath, string destinationPath)
    {
        // File.Copy uses the OS fast-path (CopyFileEx) which is dramatically faster than
        // streaming with small buffers. overwrite: false preserves the previous
        // FileMode.CreateNew behavior (throws IOException if destination exists).
        return Task.Run(() => File.Copy(sourcePath, destinationPath, overwrite: false));
    }

    /// <summary>
    /// Reads all bytes from a file asynchronously with file sharing enabled to prevent access conflicts
    /// </summary>
    public static async Task<byte[]> ReadAllBytesAsync(string filePath)
    {
        using var sourceStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var memoryStream = new MemoryStream();
        await sourceStream.CopyToAsync(memoryStream);
        return memoryStream.ToArray();
    }

    public static void OpenFileLocation(string path)
    {
        try
        {
            Process.Start("explorer.exe", $"/select, \"{path}\"");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"An error occurred while trying to open the file location: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Per-folder cache of .ytd file paths, used during batch drawable imports so each folder
    /// is enumerated once instead of once per drawable. Cleared after every batch.
    /// </summary>
    private static readonly ConcurrentDictionary<string, string[]> _ytdFolderCache = new(StringComparer.OrdinalIgnoreCase);

    public static void ClearTextureSearchCache() => _ytdFolderCache.Clear();

    public static List<string> FindMatchingTextures(string filePath, string name, bool isProp, bool useFolderCache = false)
    {
        var folderPath = Path.GetDirectoryName(filePath);
        var fileName = Path.GetFileName(filePath);
        var addonName = string.Empty;

        if (fileName.Contains('^'))
        {
            var split = fileName.Split("^");
            addonName = split[0];
            fileName = split[1];
        }
        string[] nameParts = Path.GetFileNameWithoutExtension(fileName).Split("_");

        string searchedNumber, regexToSearch;
        if (nameParts.Length == 1) //this will happen when someone is adding weirdly named ydds (for example 5.ydd or m.ydd)
        {
            searchedNumber = nameParts[0];
            var escapedNumber = Regex.Escape(searchedNumber);
            regexToSearch = $"^{escapedNumber}([a-z]|_[a-z])?$";
        } 
        else
        {
            searchedNumber = isProp ? nameParts[2] : nameParts[1];
            var searchedName = isProp ? nameParts[0] + "_" + nameParts[1] : nameParts[0];
            regexToSearch = $"^{Regex.Escape(searchedName)}_diff_{Regex.Escape(searchedNumber)}";
        }

        if (addonName != string.Empty)
        {
            regexToSearch = $"^{Regex.Escape(addonName)}\\^{regexToSearch.TrimStart('^')}";
        }

        static string[] EnumerateYtds(string path) => Directory.EnumerateFiles(path)
            .Where(x => Path.GetExtension(x).Equals(".ytd", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        var ytds = useFolderCache
            ? _ytdFolderCache.GetOrAdd(folderPath, EnumerateYtds)
            : EnumerateYtds(folderPath);

        // One regex instance for the whole folder scan; the static Regex.IsMatch cache is small
        // and every drawable uses a different pattern, so it constantly missed during batch imports.
        var regex = new Regex(regexToSearch, RegexOptions.IgnoreCase);

        return ytds
            .Where(x => regex.IsMatch(Path.GetFileNameWithoutExtension(x)))
            .ToList();
    }

    public static (bool IsProp, int DrawableType)? TryResolveDrawableTypeFromFileName(string file)
    {
        string fileName = Path.GetFileNameWithoutExtension(file);
        if (fileName.Contains('^'))
        {
            fileName = fileName.Split("^")[1];
        }

        var componentsList = EnumHelper.GetDrawableTypeList();
        var propsList = EnumHelper.GetPropTypeList();

        var compName = componentsList.FirstOrDefault(name => fileName.StartsWith(name + "_"));
        var propName = propsList.FirstOrDefault(name => fileName.StartsWith(name + "_"));

        if (compName != null)
        {
            var value = EnumHelper.GetValue(compName, false);
            return (false, value);
        }

        if (propName != null)
        {
            var value = EnumHelper.GetValue(propName, true);
            return (true, value);
        }

        return null;
    }

    public static async Task<Dictionary<string, (bool IsProp, int DrawableType)>> ResolveDrawableTypes(IEnumerable<string> files)
    {
        var resolvedTypes = new Dictionary<string, (bool IsProp, int DrawableType)>();
        var unresolvedFiles = new List<string>();

        foreach (var file in files)
        {
            var resolved = TryResolveDrawableTypeFromFileName(file);
            if (resolved.HasValue)
            {
                resolvedTypes[file] = resolved.Value;
            }
            else
            {
                unresolvedFiles.Add(file);
            }
        }

        if (unresolvedFiles.Count == 0)
        {
            return resolvedTypes;
        }

        return await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var detectedTypes = unresolvedFiles.ToDictionary(
                file => file,
                _ => ((bool IsProp, int DrawableType)?)null);
            var window = new DrawableImportResolveWindow(
                unresolvedFiles,
                detectedDrawableTypes: detectedTypes,
                showDrawableProperties: true);

            var result = window.ShowDialog();
            if (result == true)
            {
                foreach (var selection in window.SelectedDrawableTypes)
                {
                    resolvedTypes[selection.Key] = selection.Value;
                }
            }

            return resolvedTypes;
        });
    }

    public static int? GetDrawableNumberFromFileName(string fileName)
    {
        Regex numberRegex = new(@"_(\d{3})(?:_([a-zA-Z])(?:_\d+)?)?\.(yld|ydd)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        Match match = numberRegex.Match(fileName);

        if (match.Success)
        {
            return int.Parse(match.Groups[1].Value);
        }
        return null;
    }

    public static async Task SaveTexturesAsync(List<GTexture> textures, string folderPath, string format)
    {
        Directory.CreateDirectory(folderPath);

        // Determine file extension
        string fileExtension = format.ToUpper() switch
        {
            "DDS" => ".dds",
            "PNG" => ".png",
            "YTD" => ".ytd",
            _ => throw new ArgumentException($"Unsupported format: {format}", nameof(format))
        };

        ProgressHelper.Start("Started exporting textures");

        int successfulExports = 0;

        // Process each texture asynchronously and save it to the specified folder
        var tasks = textures.Select(async texture =>
        {
            string filePath = Path.Combine(folderPath, $"{texture.GetBuildName()}{fileExtension}");

            // check if file exists
            if (File.Exists(filePath))
            {
                LogHelper.Log($"Could not save texture: {texture.DisplayName}. Error: File already exists.", LogType.Error);
                return;
            }
             
            if (fileExtension == ".ytd") 
            {
                try
                {
                    byte[] textureBytes;

                    if (texture.IsOptimizedDuringBuild)
                    {
                        textureBytes = await ImgHelper.Optimize(texture);
                    }
                    else if (string.Equals(texture.Extension, ".ytd", StringComparison.OrdinalIgnoreCase))
                    {
                        textureBytes = await ReadAllBytesAsync(texture.FullFilePath);
                    }
                    else
                    {
                        textureBytes = ImgHelper.GetDDSBytes(texture);
                    }

                    if (textureBytes == null)
                    {
                        LogHelper.Log($"Could not save texture: {texture.DisplayName}. Error: Texture is corrupted.", LogType.Error);
                        return;
                    }

                    await File.WriteAllBytesAsync(filePath, textureBytes);
                    successfulExports++;
                } 
                catch (Exception ex)
                {
                    // Log the error and continue processing other textures
                    LogHelper.Log($"Could not save texture: {texture.DisplayName}. Error: {ex.Message}.", LogType.Error);
                } 
            }
            else if (fileExtension == ".dds")
            {
                try
                {
                    byte[] textureBytes = ImgHelper.GetDDSFileBytes(texture);
                    if (textureBytes.Length == 0)
                    {
                        LogHelper.Log($"Could not save texture: {texture.DisplayName}. Error: Texture is empty.", LogType.Error);
                        return;
                    }

                    await File.WriteAllBytesAsync(filePath, textureBytes);
                    successfulExports++;
                }
                catch (Exception ex)
                {
                    LogHelper.Log($"Could not save texture: {texture.DisplayName}. Error: {ex.Message}.", LogType.Error);
                }
            }
            else
            {
                using var image = ImgHelper.GetImage(texture.FullFilePath);
                image.Format = format.ToUpper() switch
                {
                    "PNG" => MagickFormat.Png,
                    _ => throw new ArgumentException($"Unsupported format for MagickImage: {format}", nameof(format))
                };

                try
                {
                    await File.WriteAllBytesAsync(filePath, image.ToByteArray());
                    successfulExports++;
                }
                catch (Exception ex)
                {
                    // Log the error and continue processing other textures
                    LogHelper.Log($"Could not save texture: {texture.DisplayName}. Error: {ex.Message}.", LogType.Error);
                }
            }
        });

        try
        {
            await Task.WhenAll(tasks);
            ProgressHelper.Stop($"Exported {successfulExports} texture(s) in {{0}}", true);
        }
        catch (Exception ex)
        {
            LogHelper.Log($"Texture export failed: {ex.Message}", LogType.Error);
            ProgressHelper.Stop("Texture export failed", false);
        }
    }

    /// <summary>
    /// Exports embedded (in-drawable) textures to a folder as DDS or PNG. Unlike normal textures
    /// these have no source file on disk - the pixel data lives inside the parent YDD, so the
    /// data is loaded on demand and converted straight from the in-memory texture.
    /// </summary>
    public static async Task SaveEmbeddedTexturesAsync(List<GTextureEmbedded> textures, string folderPath, string format)
    {
        Directory.CreateDirectory(folderPath);

        string fileExtension = format.ToUpperInvariant() switch
        {
            "DDS" => ".dds",
            "PNG" => ".png",
            _ => throw new ArgumentException($"Unsupported format: {format}", nameof(format))
        };

        ProgressHelper.Start("Started exporting embedded textures");

        int successfulExports = 0;

        try
        {
            foreach (var texture in textures)
            {
                var textureName = texture.Details?.Name ?? texture.OriginalName ?? "embedded_texture";

                try
                {
                    var loaded = await texture.EnsureTextureDataLoadedAsync();
                    var textureData = texture.DisplayTextureData;

                    if (!loaded || textureData?.Data?.FullData == null || textureData.Data.FullData.Length == 0)
                    {
                        LogHelper.Log($"Could not export embedded texture: {textureName}. Error: no texture data available (missing or encrypted).", LogType.Error);
                        continue;
                    }

                    var baseName = RemoveInvalidFileNameChars(textureName);
                    var filePath = GetAvailableFilePath(folderPath, baseName, fileExtension);

                    var ddsBytes = CodeWalker.Utils.DDSIO.GetDDSFile(textureData);

                    if (fileExtension == ".dds")
                    {
                        await File.WriteAllBytesAsync(filePath, ddsBytes);
                    }
                    else
                    {
                        using var image = new MagickImage(ddsBytes) { Format = MagickFormat.Png };
                        await File.WriteAllBytesAsync(filePath, image.ToByteArray());
                    }

                    successfulExports++;
                }
                catch (Exception ex)
                {
                    LogHelper.Log($"Could not export embedded texture: {textureName}. Error: {ex.Message}.", LogType.Error);
                }
            }

            ProgressHelper.Stop($"Exported {successfulExports} embedded texture(s) in {{0}}", true);
        }
        catch (Exception ex)
        {
            LogHelper.Log($"Embedded texture export failed: {ex.Message}", LogType.Error);
            ProgressHelper.Stop("Embedded texture export failed", false);
        }
    }

    private static string RemoveInvalidFileNameChars(string name)
    {
        return string.Concat(name.Split(Path.GetInvalidFileNameChars()));
    }

    /// <summary>
    /// Returns a file path in <paramref name="folderPath"/> for the given base name and extension,
    /// appending a numeric suffix if a file with that name already exists.
    /// </summary>
    private static string GetAvailableFilePath(string folderPath, string baseName, string extension)
    {
        var candidate = Path.Combine(folderPath, $"{baseName}{extension}");
        int counter = 1;
        while (File.Exists(candidate))
        {
            candidate = Path.Combine(folderPath, $"{baseName}_{counter}{extension}");
            counter++;
        }
        return candidate;
    }

    public static async Task SaveDrawablesAsync(List<GDrawable> drawables, string folderPath)
    {
        Directory.CreateDirectory(folderPath);
        ProgressHelper.Start("Started exporting drawables");

        int successfulExports = 0;

        // Process each drawable asynchronously and save it to the specified folder
        var tasks = drawables.Select(async drawable =>
        {
            string filePath = Path.Combine(folderPath, $"{drawable.Name}{Path.GetExtension(drawable.FullFilePath)}");

            // check if file exists
            if (File.Exists(filePath))
            {
                LogHelper.Log($"Could not save drawable: {drawable.Name}. Error: File already exists.", LogType.Error);
                return;
            }

            try
            {
                await CopyAsync(drawable.FullFilePath, filePath);
                successfulExports++;
            }
            catch (Exception ex)
            {
                // Log the error and continue processing other drawables
                LogHelper.Log($"Could not save drawable: {drawable.Name}. Error: {ex.Message}.", LogType.Error);
            }
        });

        try
        {
            await Task.WhenAll(tasks);
            ProgressHelper.Stop($"Exported {successfulExports} drawable(s) in {{0}}", true);
        }
        catch (Exception ex)
        {
            LogHelper.Log($"Drawable export failed: {ex.Message}", LogType.Error);
            ProgressHelper.Stop("Drawable export failed", false);
        }
    }
}
