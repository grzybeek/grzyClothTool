using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using grzyClothTool.Models;

namespace grzyClothTool.Helpers;

public class FileHelper
{
    private DirectoryInfo ProjectPath;
    private readonly string ProjectName;
    public static string ReservedAssetsPath { get; private set; }

    public FileHelper(string projectName)
    {
        ProjectName = projectName;

        GenerateProjectFolder();
    }

    private void GenerateProjectFolder()
    {
        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var exeName = Assembly.GetExecutingAssembly().GetName().Name;

        var path = Path.Combine(documentsPath, exeName, ProjectName);
        ProjectPath = Directory.CreateDirectory(path);

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

        var textures = new ObservableCollection<GTexture>(matchingTextures.Select((path, txtNumber) => new GTexture(path, typeNumber, countOfType, txtNumber, drawableHasSkin, isProp)));

        return Task.FromResult(new GDrawable(filePath, isMale, isProp, typeNumber, countOfType, drawableHasSkin, textures));
    }

    public static async Task CopyAsync(string sourcePath, string destinationPath)
    {
        using var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var destinationStream = new FileStream(destinationPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
        await sourceStream.CopyToAsync(destinationStream);
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
        var searchedNumber = isProp ? nameParts[2] : nameParts[1];

        var regexToSearch = $"^{name}_diff_{searchedNumber}";
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

    public static (bool, int) IsValidComponent(string file)
    {
        string fileName = Path.GetFileNameWithoutExtension(file);
        if(fileName.Contains('^'))
        {
            fileName = fileName.Split("^")[1];
        }

        var componentsEnumNames = Enum.GetNames(typeof(Enums.ComponentNumbers));
        var compName = componentsEnumNames.FirstOrDefault(fileName.Contains);

        if(fileName.Contains($"p_{compName}"))
        {
            var prop = IsValidProp(file);
            return (false, prop.Item2);
        }

        if (!Enum.IsDefined(typeof(Enums.ComponentNumbers), compName))
        {
            return (false, -1); //todo: not found, ask for type
        }

        var compNumber = (int)(Enums.ComponentNumbers)Enum.Parse(typeof(Enums.ComponentNumbers), compName.ToLower());
        return (true, compNumber);
    }

    public static (bool, int) IsValidProp(string file)
    {
        string fileName = Path.GetFileNameWithoutExtension(file);
        if (fileName.Contains('^'))
        {
            fileName = fileName.Split("^")[1];
        }

        var propsEnumNames = Enum.GetNames(typeof(Enums.PropNumbers));
        var propName = propsEnumNames.FirstOrDefault(fileName.Contains);

        if (!Enum.IsDefined(typeof(Enums.PropNumbers), propName))
        {
            return (false, -1); //todo: not found, ask for type
        }

        var propNumber = (int)(Enums.PropNumbers)Enum.Parse(typeof(Enums.PropNumbers), propName.ToLower());
        return (true, propNumber);
    }


}
