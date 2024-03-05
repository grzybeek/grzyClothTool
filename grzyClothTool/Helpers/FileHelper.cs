using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using grzyClothTool.Models;

namespace grzyClothTool.Helpers;

public class FileHelper
{
    private DirectoryInfo ProjectPath;
    private readonly string ProjectName;

    public FileHelper(string projectName)
    {
        ProjectName = projectName;

        GenerateProjectFolder();
    }

    private void GenerateProjectFolder()
    {
        //todo: specify path where to save files in settings + on first launch of program?
        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var exeName = Assembly.GetExecutingAssembly().GetName().Name;

        var path = Path.Combine(documentsPath, exeName, ProjectName);
        ProjectPath = Directory.CreateDirectory(path);
    }

    public Task<Drawable> CreateDrawableAsync(string filePath, bool isMale, bool isProp, int typeNumber, int countOfType)
    {
        FileInfo file = new(filePath);
        var name = EnumHelper.GetName(typeNumber, isProp);

        var matchingTextures = FindMatchingTextures(file, name, isProp);

        var path = Path.Combine(name, countOfType.ToString("D3"));

        var drawableFolder = ProjectPath.CreateSubdirectory(path);
        var drawableName = Guid.NewGuid().ToString();
        var drawableRaceSuffix = Path.GetFileNameWithoutExtension(file.Name)[^1..];
        var drawableHasSkin = drawableRaceSuffix == "r";
        var drawableFile = CopyFile(file, drawableFolder, drawableName);

        //copy textures
        var textures = matchingTextures.Select((path, txtNumber) => new GTexture(path, typeNumber, countOfType, txtNumber, drawableHasSkin, isProp)).ToList();
        foreach (var texture in textures)
        {
            var txtFile = new FileInfo(path);
            var txtInternalName = Guid.NewGuid().ToString();
            var newTxtFile = CopyFile(texture.File, drawableFolder, txtInternalName);
            texture.File = newTxtFile;
            texture.InternalName = txtInternalName;
        }

        return Task.FromResult(new Drawable(drawableFile, isMale, isProp, typeNumber, countOfType, drawableHasSkin, textures));
    }

    private static FileInfo CopyFile(FileInfo file, DirectoryInfo directory, string newName)
    {
        var newPath = Path.Combine(directory.FullName, $"{newName}{file.Extension}");
        return file.CopyTo(newPath);
    }

    public static List<string> FindMatchingTextures(FileInfo file, string name, bool isProp)
    {
        var folderPath = file.Directory.FullName;
        var fileName = file.Name;
        if (fileName.Contains('^'))
        {
            fileName = fileName.Split("^")[1];
        }
        string[] nameParts = Path.GetFileNameWithoutExtension(fileName).Split("_");
        var searchedNumber = isProp ? nameParts[2] : nameParts[1];

        var allYtds = Directory.EnumerateFiles(folderPath)
            .Where(x => Path.GetExtension(x) == ".ytd" &&
                Path.GetFileNameWithoutExtension(x).Contains(name) &&
                Path.GetFileNameWithoutExtension(x).Contains(searchedNumber))
            .ToList();


        return allYtds;
    }

    public static Task<List<string>> FindMatchingTexturesAsync(FileInfo file, string name, bool isProp)
    {
        var folderPath = file.Directory.FullName;
        var fileName = file.Name;
        if (fileName.Contains('^'))
        {
            fileName = fileName.Split("^")[1];
        }
        string[] nameParts = Path.GetFileNameWithoutExtension(fileName).Split("_");
        var searchedNumber = isProp ? nameParts[2] : nameParts[1];

        var allYtds = Directory.EnumerateFiles(folderPath)
            .Where(x => Path.GetExtension(x) == ".ytd" &&
                Path.GetFileNameWithoutExtension(x).Contains(name) &&
                Path.GetFileNameWithoutExtension(x).Contains(searchedNumber))
            .ToList();

        return Task.FromResult(allYtds);
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
