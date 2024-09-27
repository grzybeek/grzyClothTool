using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using grzyClothTool.Models.Drawable;
using grzyClothTool.Models.Texture;
using grzyClothTool.Views;
using ImageMagick;

namespace grzyClothTool.Helpers;

public static class FileHelper
{
    public static string ReservedAssetsPath { get; private set; }

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

    public static Task<GDrawable> CreateDrawableAsync(string filePath, bool isMale, bool isProp, int typeNumber, int countOfType)
    {
        var name = EnumHelper.GetName(typeNumber, isProp);

        var matchingTextures = FindMatchingTextures(filePath, name, isProp);

        var drawableName = Guid.NewGuid().ToString();
        var drawableRaceSuffix = Path.GetFileNameWithoutExtension(filePath)[^1..];
        var drawableHasSkin = drawableRaceSuffix == "r";

        // Should we inform user, that they tried to add too many textures?
        var textures = new ObservableCollection<GTexture>(matchingTextures.Select((path, txtNumber) => new GTexture(path, typeNumber, countOfType, txtNumber, drawableHasSkin, isProp)).Take(GlobalConstants.MAX_DRAWABLE_TEXTURES));

        return Task.FromResult(new GDrawable(filePath, isMale, isProp, typeNumber, countOfType, drawableHasSkin, textures));
    }

    public static async Task CopyAsync(string sourcePath, string destinationPath)
    {
        using var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var destinationStream = new FileStream(destinationPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
        await sourceStream.CopyToAsync(destinationStream);
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

    public static List<string> FindMatchingTextures(string filePath, string name, bool isProp)
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
        if (nameParts.Length == 1) //this will happen when someone is adding weirdly named ydds (for example 5.ydd)
        {
            searchedNumber = nameParts[0];
            regexToSearch = $"^{searchedNumber}([a-z]|_[a-z])?"; //this will try to find 5.ytd 5a.ytd or 5_a.ytd files
        } 
        else
        {
            searchedNumber = isProp ? nameParts[2] : nameParts[1];
            regexToSearch = $"^{name}_diff_{searchedNumber}";
        }

        if (addonName != string.Empty)
        {
            regexToSearch = $"^{addonName}\\{regexToSearch}";
        }

        var allYtds = Directory.EnumerateFiles(folderPath)
            .Where(x => Path.GetExtension(x) == ".ytd" &&
                Regex.IsMatch(Path.GetFileNameWithoutExtension(x), regexToSearch))
            .ToList();

        return allYtds;
    }

    public static (bool, int) ResolveDrawableType(string file)
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

        var window = new DrawableSelectWindow(file);
        var result = window.ShowDialog();
        if (result == true)
        {
            var value = EnumHelper.GetValue(window.SelectedDrawableType, window.IsProp);
            return (window.IsProp, value);
        }

        return (false, -1);
    }

    public static async Task SaveTexturesAsync(List<GTexture> textures, string folderPath, string format)
    {

        // Ensure the directory exists or create it. Consider handling any exceptions if directory creation fails
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }


        // Map the format to file extension and MagickImage format. Throw an exception for unsupported formats
        var (fileExtension, imageFormat) = format.ToUpper() switch
        {
            "DDS" => (".dds", MagickFormat.Dds),
            "PNG" => (".png", MagickFormat.Png),
            _ => throw new ArgumentException($"Unsupported format: {format}", nameof(format))
        };

        // Process each texture asynchronously and save it to the specified folder
        var tasks = textures.Select(async texture =>
        {
            string filePath = Path.Combine(folderPath, $"{texture.DisplayName}{fileExtension}");
            using var image = ImgHelper.GetImage(texture.FilePath);
            image.Format = imageFormat;

            try
            {
                await File.WriteAllBytesAsync(filePath, image.ToByteArray());
            }
            catch (Exception ex)
            {
                // Log the error and continue processing other textures
                LogHelper.Log($"Could not save texture: {texture.DisplayName}. Error: {ex.Message}", LogType.Error);
            }
        });

        await Task.WhenAll(tasks);
    }
}
