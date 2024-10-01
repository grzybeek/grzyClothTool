using CodeWalker.GameFiles;
using grzyClothTool.Models;
using grzyClothTool.Models.Drawable;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using static grzyClothTool.Enums;

namespace grzyClothTool.Helpers;

public class BuildResourceHelper
{
    private Addon _addon;
    private int _number;
    private readonly string _projectName;
    private readonly string _buildPath;
    private readonly IProgress<int> _progress;

    private readonly bool shouldUseNumber = false;

    private readonly List<string> firstPersonFiles = [];
    private BuildResourceType _buildResourceType;

    public BuildResourceHelper(string name, string path, IProgress<int> progress, BuildResourceType resourceType)
    {
        _projectName = name;
        _buildPath = path;
        _progress = progress;
        _buildResourceType = resourceType;

        shouldUseNumber = MainWindow.AddonManager.Addons.Count > 1;
    }

    public void SetAddon(Addon addon)
    {
        _addon = addon;
    }

    public void SetNumber(int number)
    {
        _number = number;
    }

    public string GetProjectName(int? number = null)
    {
        number ??= _number;
        return shouldUseNumber ? $"{_projectName}_{number:D2}" : _projectName;
    }
    

    #region FiveM


    private async Task BuildFiveMFilesAsync(SexType sex, byte[] ymtBytes, int counter)
    {
        var pedName = GetPedName(sex);
        var projectName = GetProjectName(counter);

        var drawables = _addon.Drawables.Where(x => x.Sex == sex).ToList();
        var drawableGroups = drawables.Select((x, i) => new { Index = i, Value = x })
                                       .GroupBy(x => x.Value.Number / GlobalConstants.MAX_DRAWABLES_IN_ADDON)
                                       .Select(x => x.Select(v => v.Value).ToList())
                                       .ToList();

        // Prepare all directory paths first to minimize file system access
        var genderFolderName = sex == SexType.male ? "[male]" : "[female]";
        var directoriesToEnsure = drawableGroups.SelectMany(g => g.Select(d => Path.Combine(_buildPath, "stream", genderFolderName, d.TypeName))).Distinct();
        foreach (var dir in directoriesToEnsure)
        {
            Directory.CreateDirectory(dir);
        }

        var fileOperations = new List<Task>();

        foreach (var group in drawableGroups)
        {
            var ymtPath = Path.Combine(_buildPath, "stream", $"{pedName}_{projectName}.ymt");
            fileOperations.Add(File.WriteAllBytesAsync(ymtPath, ymtBytes));

            foreach (var d in group)
            {
                var drawablePedName = d.IsProp ? $"{pedName}_p" : pedName;
                var folderPath = Path.Combine(_buildPath, "stream", genderFolderName, d.TypeName);
                var prefix = RemoveInvalidChars($"{drawablePedName}_{projectName}^");
                var finalPath = Path.Combine(folderPath, $"{prefix}{d.Name}{Path.GetExtension(d.FilePath)}");
                fileOperations.Add(FileHelper.CopyAsync(d.FilePath, finalPath));

                if (!string.IsNullOrEmpty(d.ClothPhysicsPath))
                {
                    fileOperations.Add(FileHelper.CopyAsync(d.ClothPhysicsPath, Path.Combine(folderPath, $"{prefix}{d.Name}{Path.GetExtension(d.ClothPhysicsPath)}")));
                }

                if (!string.IsNullOrEmpty(d.FirstPersonPath))
                {
                    //todo: this probably shouldn't be hardcoded to "_1", handle it when there is option to add more alternate drawable versions
                    fileOperations.Add(FileHelper.CopyAsync(d.FirstPersonPath, Path.Combine(folderPath, $"{prefix}{d.Name}_1{Path.GetExtension(d.FirstPersonPath)}")));
                    
                    var name = $"{prefix}{d.Name}".Replace("^", "/");
                    firstPersonFiles.Add(name);
                }

                foreach (var t in d.Textures)
                {
                    var displayName = RemoveInvalidChars(t.DisplayName);
                    var finalTexPath = Path.Combine(folderPath, $"{prefix}{displayName}.ytd");

                    byte[] txtBytes = null;
                    if (t.IsOptimizedDuringBuild)
                    {
                        txtBytes = await ImgHelper.Optimize(t);
                        fileOperations.Add(File.WriteAllBytesAsync(finalTexPath, txtBytes));
                    }
                    else
                    {
                        if(t.Extension != ".ytd")
                        {
                            txtBytes = await ImgHelper.Optimize(t, true);
                        } 
                        else
                        {
                            txtBytes = File.ReadAllBytes(t.FilePath);
                        }

                        fileOperations.Add(File.WriteAllBytesAsync(finalTexPath, txtBytes));
                    }
                }
            }
        }

        int completedTasks = 0;
        int lastReportedProgress = 0;

        var runningTasks = fileOperations.ToList(); // Start all file operations
        int totalTasks = runningTasks.Count;

        while (completedTasks < totalTasks)
        {
            Task finishedTask = await Task.WhenAny(runningTasks);
            completedTasks++;

            if (completedTasks - lastReportedProgress >= 20 || runningTasks.Count == 0)
            {
                _progress.Report(completedTasks - lastReportedProgress);
                lastReportedProgress = completedTasks;
            }
        }

        await Task.Run(() =>
            {
                var generated = GenerateCreatureMetadata(drawables);
                generated?.Save(_buildPath + "/stream/mp_creaturemetadata_" + GetGenderLetter(sex) + "_" + projectName + ".ymt");
            }
        );
    }

    public async Task BuildFiveMResource()
    {
        int counter = 1;
        var metaFiles = new List<string>();
        var tasks = new List<Task>();

        foreach (var selectedAddon in MainWindow.AddonManager.Addons)
        {
            SetAddon(selectedAddon);
            SetNumber(counter);

            AddBuildTasksForSex(selectedAddon, SexType.male, tasks, metaFiles, counter);
            AddBuildTasksForSex(selectedAddon, SexType.female, tasks, metaFiles, counter);

            counter++;
        }

        await Task.WhenAll(tasks);
        BuildFirstPersonAlternatesMeta();
        BuildFxManifest(metaFiles);
    }

    private void BuildFxManifest(List<string> metaFiles)
    {
        StringBuilder contentBuilder = new();
        contentBuilder.AppendLine("-- This resource was generated by grzyClothTool :)");
        contentBuilder.AppendLine($"-- {GlobalConstants.DISCORD_INVITE_URL}");
        contentBuilder.AppendLine();
        contentBuilder.AppendLine("fx_version 'cerulean'");
        contentBuilder.AppendLine("game 'gta5'");
        contentBuilder.AppendLine("author 'grzyClothTool'");
        contentBuilder.AppendLine();
        contentBuilder.AppendLine("files {");

        var filesSectionBuilder = new StringBuilder();
        filesSectionBuilder.Append(string.Join(",\n  ", metaFiles.Select(f => $"'{f}'")));

        if (firstPersonFiles.Count > 0)
        {
            filesSectionBuilder.Append($",\n  'first_person_alternates_{_projectName}.meta'");
        }

        contentBuilder.AppendLine($"  {filesSectionBuilder}");

        contentBuilder.AppendLine("}");
        contentBuilder.AppendLine();

        foreach (var file in metaFiles)
        {
            contentBuilder.AppendLine($"data_file 'SHOP_PED_APPAREL_META_FILE' '{file}'");
        }

        if (firstPersonFiles.Count > 0)
        {
            contentBuilder.AppendLine($"data_file 'PED_FIRST_PERSON_ALTERNATE_DATA' 'first_person_alternates_{_projectName}.meta'");
        }

        var finalPath = Path.Combine(_buildPath, "fxmanifest.lua");
        File.WriteAllText(finalPath, contentBuilder.ToString());

        //clear firstPerson files
        firstPersonFiles.Clear();
    }

    #endregion

    #region AltV

    public async Task BuildAltVResource()
    {
        int counter = 1;
        var metaFiles = new List<string>();
        var tasks = new List<Task>();

        foreach (var selectedAddon in MainWindow.AddonManager.Addons)
        {
            SetAddon(selectedAddon);
            SetNumber(counter);

            AddBuildTasksForSex(selectedAddon, SexType.male, tasks, metaFiles, counter);
            AddBuildTasksForSex(selectedAddon, SexType.female, tasks, metaFiles, counter);

            counter++;
        }

        await Task.WhenAll(tasks);
        BuildAltVTomls(metaFiles);
    }

    private async Task BuildAltVFilesAsync(SexType sex, byte[] ymtBytes, int counter)
    {
        var pedName = GetPedName(sex);
        var projectName = GetProjectName(counter);

        var drawables = _addon.Drawables.Where(x => x.Sex == sex).ToList();
        var drawableGroups = drawables.Select((x, i) => new { Index = i, Value = x })
                                       .GroupBy(x => x.Value.Number / GlobalConstants.MAX_DRAWABLES_IN_ADDON)
                                       .Select(x => x.Select(v => v.Value).ToList())
                                       .ToList();

        // Prepare all directory paths first to minimize file system access
        var genderFolderName = sex == SexType.male ? $"{projectName}_male.rpf" : $"{projectName}_female.rpf";

        //Describe the 3 levels of a AltV clothes resource
        var firstLevelFolder = Path.Combine(_buildPath, "stream");
        var secondLevelFolder = Path.Combine(_buildPath, "stream", genderFolderName);
        var thirdLevelFolder = Path.Combine(_buildPath, "stream", genderFolderName, $"{pedName}_{projectName}");

        var directoriesToEnsure = new List<string> {
            firstLevelFolder, //First level is the stream folder
            secondLevelFolder,
            thirdLevelFolder
        };

        string thirdLevelPropFolder = null;
        //Additionaly if props are present, create a folder for them
        if(drawables.Any(x => x.IsProp)) {
            var genderPropFolderName = sex == SexType.male ? $"{projectName}_male_p.rpf" : $"{projectName}_female_p.rpf";

            var secondLevelPropFolder = Path.Combine(_buildPath, "stream", genderPropFolderName);
            thirdLevelPropFolder = Path.Combine(_buildPath, "stream", genderPropFolderName, $"{pedName}_p_{projectName}");
            
            directoriesToEnsure.Add(secondLevelPropFolder);
            directoriesToEnsure.Add(thirdLevelPropFolder);
        }

        foreach(var dir in directoriesToEnsure) {
            Directory.CreateDirectory(dir);
        }

        var fileOperations = new List<Task>();

        foreach(var group in drawableGroups) {
            var ymtPath = Path.Combine(secondLevelFolder, $"{pedName}_{projectName}.ymt");
            fileOperations.Add(File.WriteAllBytesAsync(ymtPath, ymtBytes));

            foreach(var d in group) {
                var drawablePedName = d.IsProp ? $"{pedName}_p" : pedName;
                var folderPath = d.IsProp ? thirdLevelPropFolder : thirdLevelFolder;

                fileOperations.Add(FileHelper.CopyAsync(d.FilePath, Path.Combine(folderPath, $"{d.Name}{Path.GetExtension(d.FilePath)}")));

                foreach(var t in d.Textures) {
                    var displayName = RemoveInvalidChars(t.DisplayName);
                    var finalTexPath = Path.Combine(folderPath, $"{displayName}{Path.GetExtension(t.FilePath)}");
                    if(t.IsOptimizedDuringBuild) {
                        var optimizedBytes = await ImgHelper.Optimize(t);
                        fileOperations.Add(File.WriteAllBytesAsync(finalTexPath, optimizedBytes));
                    } else {
                        fileOperations.Add(FileHelper.CopyAsync(t.FilePath, finalTexPath));
                    }
                }
            }
        }

        int completedTasks = 0;
        int lastReportedProgress = 0;

        var runningTasks = fileOperations.ToList(); // Start all file operations
        int totalTasks = runningTasks.Count;

        while (completedTasks < totalTasks)
        {
            Task finishedTask = await Task.WhenAny(runningTasks);
            completedTasks++;

            if (completedTasks - lastReportedProgress >= 20 || runningTasks.Count == 0)
            {
                _progress.Report(completedTasks - lastReportedProgress);
                lastReportedProgress = completedTasks;
            }
        }

        await Task.Run(() => {
            var generated = GenerateCreatureMetadata(drawables);
            if (generated != null)
            {
                string creatureMetadataFolder = Path.Combine(firstLevelFolder, "creaturemetadata.rpf");
                Directory.CreateDirectory(creatureMetadataFolder);
                generated.Save(Path.Combine(creatureMetadataFolder, $"mp_creaturemetadata_{GetGenderLetter(sex)}_{projectName}.ymt"));
            }
        });
    }

    private void BuildAltVTomls(List<string> metaFiles)
    {
        StringBuilder resourceTomlBuilder = new();
        resourceTomlBuilder.AppendLine("# This resource was generated by grzyClothTool :)");
        resourceTomlBuilder.AppendLine($"# {GlobalConstants.DISCORD_INVITE_URL}");
        resourceTomlBuilder.AppendLine();
        resourceTomlBuilder.AppendLine("type = 'dlc'");
        resourceTomlBuilder.AppendLine("main = 'stream.toml'");
        resourceTomlBuilder.AppendLine("client-files = [ 'stream/*' ]");

        var finalResourceTomlPath = Path.Combine(_buildPath, "resource.toml");
        File.WriteAllText(finalResourceTomlPath, resourceTomlBuilder.ToString());

        StringBuilder streamTomlBuilder = new();
        streamTomlBuilder.AppendLine("# This resource was generated by grzyClothTool :)");
        streamTomlBuilder.AppendLine($"# {GlobalConstants.DISCORD_INVITE_URL}");
        streamTomlBuilder.AppendLine();
        streamTomlBuilder.AppendLine("files = [");
        streamTomlBuilder.AppendLine("'stream/*',");
        streamTomlBuilder.AppendLine("]");
        streamTomlBuilder.AppendLine();
        streamTomlBuilder.AppendLine("[meta]");

        foreach(var file in metaFiles) {
            streamTomlBuilder.AppendLine($"'stream/{file}' = 'SHOP_PED_APPAREL_META_FILE'");
        }

        var finalStreamTomlPath = Path.Combine(_buildPath, "stream.toml");
        File.WriteAllText(finalStreamTomlPath, streamTomlBuilder.ToString());
    }

    #endregion


    #region Singleplayer

    public async Task BuildSingleplayerResource()
    {
        var dlcRpf = RpfFile.CreateNew(_buildPath, "dlc.rpf", RpfEncryption.OPEN);
        var creatureMetadatas = BuildContentXml(dlcRpf.Root);
        BuildSetupXml(dlcRpf.Root);

        var x64 = RpfFile.CreateDirectory(dlcRpf.Root, "x64");
        var common = RpfFile.CreateDirectory(dlcRpf.Root, "common");
        var dataFolder = RpfFile.CreateDirectory(common, "data");

        var models = RpfFile.CreateDirectory(x64, "models");
        var cdimages = RpfFile.CreateDirectory(models, "cdimages");

        if (creatureMetadatas.Count > 0)
        {
            var animFolder = RpfFile.CreateDirectory(x64, "anim");
            var creature = RpfFile.CreateNew(animFolder, "creaturemetadata.rpf");

            foreach (var meta in creatureMetadatas)
            {
                RpfFile.CreateFile(creature.Root, meta.SingleplayerFileName + ".ymt", meta.Save());
            }
        }

        int counter = 1;
        var tasks = new List<Task>();
        var metaFiles = new List<string>();

        foreach (var selectedAddon in MainWindow.AddonManager.Addons)
        {
            SetAddon(selectedAddon);
            SetNumber(counter);

            AddBuildTasksForSex(selectedAddon, SexType.male, tasks, metaFiles, counter, cdimages, dataFolder);
            AddBuildTasksForSex(selectedAddon, SexType.female, tasks, metaFiles, counter, cdimages, dataFolder);

            counter++;
        }

        await Task.WhenAll(tasks);
    }

    private List<RbfFile> BuildContentXml(RpfDirectoryEntry dir)
    {
        StringBuilder sb = new();

        sb.AppendLine($"<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine($"<CDataFileMgr__ContentsOfDataFileXml>");
        sb.AppendLine($"  <disabledFiles />");
        sb.AppendLine($"  <includedXmlFiles />");
        sb.AppendLine($"  <includedDataFiles />");
        sb.AppendLine($"  <dataFiles>");

        var generatedCreatureMetadatas = new List<RbfFile>();
        var filesToEnable = new List<string>();
        foreach (var addon in MainWindow.AddonManager.Addons)
        {
            if (addon.HasSex(SexType.male))
            {
                sb.AppendLine($"    <Item>");
                sb.AppendLine($"      <filename>dlc_{_projectName}:/common/data/mp_m_freemode_01_{_projectName}.meta</filename>");
                sb.AppendLine($"      <fileType>SHOP_PED_APPAREL_META_FILE</fileType>");
                sb.AppendLine($"      <overlay value=\"false\" />");
                sb.AppendLine($"      <disabled value=\"true\" />");
                sb.AppendLine($"      <persistent value=\"false\" />");
                sb.AppendLine($"    </Item>");

                sb.AppendLine($"    <Item>");
                sb.AppendLine($"      <filename>dlc_{_projectName}:/%PLATFORM%/models/cdimages/{_projectName}_male.rpf</filename>");
                sb.AppendLine($"      <fileType>RPF_FILE</fileType>");
                sb.AppendLine($"      <overlay value=\"false\" />");
                sb.AppendLine($"      <disabled value=\"true\" />");
                sb.AppendLine($"      <persistent value=\"true\" />");
                sb.AppendLine($"    </Item>");

                filesToEnable.Add($"dlc_{_projectName}:/common/data/mp_m_freemode_01_{_projectName}.meta");
                filesToEnable.Add($"dlc_{_projectName}:/%PLATFORM%/models/cdimages/{_projectName}_male.rpf");

                if (addon.HasProps)
                {
                    sb.AppendLine($"    <Item>");
                    sb.AppendLine($"      <filename>dlc_{_projectName}:/%PLATFORM%/models/cdimages/{_projectName}_male_p.rpf</filename>");
                    sb.AppendLine($"      <fileType>RPF_FILE</fileType>");
                    sb.AppendLine($"      <overlay value=\"false\" />");
                    sb.AppendLine($"      <disabled value=\"true\" />");
                    sb.AppendLine($"      <persistent value=\"true\" />");
                    sb.AppendLine($"    </Item>");

                    filesToEnable.Add($"dlc_{_projectName}:/%PLATFORM%/models/cdimages/{_projectName}_male_p.rpf");
                }

                var drawables = addon.Drawables.Where(x => x.Sex == SexType.male).ToList();
                var generated = GenerateCreatureMetadata(drawables);
                if (generated != null)
                {
                    generated.SingleplayerFileName = $"mp_creaturemetadata_m_{_projectName}";
                    generatedCreatureMetadatas.Add(generated);
                }
            }

            if (addon.HasSex(SexType.female))
            {
                sb.AppendLine($"    <Item>");
                sb.AppendLine($"      <filename>dlc_{_projectName}:/common/data/mp_f_freemode_01_{_projectName}.meta</filename>");
                sb.AppendLine($"      <fileType>SHOP_PED_APPAREL_META_FILE</fileType>");
                sb.AppendLine($"      <overlay value=\"false\" />");
                sb.AppendLine($"      <disabled value=\"true\" />");
                sb.AppendLine($"      <persistent value=\"false\" />");
                sb.AppendLine($"    </Item>");

                sb.AppendLine($"    <Item>");
                sb.AppendLine($"      <filename>dlc_{_projectName}:/%PLATFORM%/models/cdimages/{_projectName}_female.rpf</filename>");
                sb.AppendLine($"      <fileType>RPF_FILE</fileType>");
                sb.AppendLine($"      <overlay value=\"false\" />");
                sb.AppendLine($"      <disabled value=\"true\" />");
                sb.AppendLine($"      <persistent value=\"true\" />");
                sb.AppendLine($"    </Item>");

                filesToEnable.Add($"dlc_{_projectName}:/common/data/mp_f_freemode_01_{_projectName}.meta");
                filesToEnable.Add($"dlc_{_projectName}:/%PLATFORM%/models/cdimages/{_projectName}_female.rpf");

                if (addon.HasProps)
                {
                    sb.AppendLine($"    <Item>");
                    sb.AppendLine($"      <filename>dlc_{_projectName}:/%PLATFORM%/models/cdimages/{_projectName}_female_p.rpf</filename>");
                    sb.AppendLine($"      <fileType>RPF_FILE</fileType>");
                    sb.AppendLine($"      <overlay value=\"false\" />");
                    sb.AppendLine($"      <disabled value=\"true\" />");
                    sb.AppendLine($"      <persistent value=\"true\" />");
                    sb.AppendLine($"    </Item>");


                    filesToEnable.Add($"dlc_{_projectName}:/%PLATFORM%/models/cdimages/{_projectName}_female_p.rpf");
                }

                var drawables = addon.Drawables.Where(x => x.Sex == SexType.female).ToList();
                var generated = GenerateCreatureMetadata(drawables);
                if (generated != null)
                {
                    generated.SingleplayerFileName = $"mp_creaturemetadata_f_{_projectName}";
                    generatedCreatureMetadatas.Add(generated);
                }
            }
        }

        if (generatedCreatureMetadatas.Count > 0)
        {
            sb.AppendLine($"    <Item>");
            sb.AppendLine($"      <filename>dlc_{_projectName}:/%PLATFORM%/anim/creaturemetadata.rpf</filename>");
            sb.AppendLine($"      <fileType>RPF_FILE</fileType>");
            sb.AppendLine($"      <overlay value=\"false\" />");
            sb.AppendLine($"      <disabled value=\"true\" />");
            sb.AppendLine($"      <persistent value=\"true\" />");
            sb.AppendLine($"    </Item>");

            filesToEnable.Add($"dlc_{_projectName}:/%PLATFORM%/anim/creaturemetadata.rpf");
        }

        sb.AppendLine($"  </dataFiles>");
        sb.AppendLine($"  <contentChangeSets>");
        sb.AppendLine($"    <Item>");
        sb.AppendLine($"      <changeSetName>{_projectName.ToUpper()}_GEN</changeSetName>");
        sb.AppendLine($"      <mapChangeSetData />");
        sb.AppendLine($"      <filesToInvalidate />");
        sb.AppendLine($"      <filesToDisable />");
        sb.AppendLine($"	  <filesToEnable>");

        foreach (var file in filesToEnable)
        {
            sb.AppendLine($"	    <Item>{file}</Item>");
        }

        sb.AppendLine($"	  </filesToEnable>");
        sb.AppendLine($"      <txdToLoad />");
        sb.AppendLine($"      <txdToUnload />");
        sb.AppendLine($"      <residentResources />");
        sb.AppendLine($"      <unregisterResources />");
        sb.AppendLine($"      <requiresLoadingScreen value=\"false\" />");
        sb.AppendLine($"    </Item>");
        sb.AppendLine($"  </contentChangeSets>");
        sb.AppendLine($"  <patchFiles />");
        sb.AppendLine($"</CDataFileMgr__ContentsOfDataFileXml>");

        RpfFile.CreateFile(dir, "content.xml", Encoding.UTF8.GetBytes(sb.ToString()));

        return generatedCreatureMetadatas;
    }

    private void BuildSetupXml(RpfDirectoryEntry dir)
    {
        var timestamp = DateTimeOffset.UtcNow.ToString("dd/MM/yyyy HH:mm:ss");

        StringBuilder sb = new();
        sb.AppendLine($"<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine($"<SSetupData>");
        sb.AppendLine($"  <deviceName>dlc_{_projectName}</deviceName>");
        sb.AppendLine($"  <datFile>content.xml</datFile>");
        sb.AppendLine($"  <timeStamp>{timestamp}</timeStamp>");
        sb.AppendLine($"  <nameHash>{_projectName}</nameHash>");
        sb.AppendLine($"  <contentChangeSets />");
        sb.AppendLine($"  <contentChangeSetGroups>");
        sb.AppendLine($"    <Item>");
        sb.AppendLine($"      <NameHash>GROUP_STARTUP</NameHash>");
        sb.AppendLine($"      <ContentChangeSets>");
        sb.AppendLine($"        <Item>{_projectName.ToUpper()}_GEN</Item>");
        sb.AppendLine($"      </ContentChangeSets>");
        sb.AppendLine($"    </Item>");
        sb.AppendLine($"  </contentChangeSetGroups>");
        sb.AppendLine($"  <startupScript />");
        sb.AppendLine($"  <scriptCallstackSize value=\"0\" />");
        sb.AppendLine($"  <type>EXTRACONTENT_COMPAT_PACK</type>");
        sb.AppendLine($"  <order value=\"999\" />");
        sb.AppendLine($"  <minorOrder value=\"0\" />");
        sb.AppendLine($"  <isLevelPack value=\"false\" />");
        sb.AppendLine($"  <dependencyPackHash />");
        sb.AppendLine($"  <requiredVersion />");
        sb.AppendLine($"  <subPackCount value=\"0\" />");
        sb.AppendLine($"</SSetupData>");

        RpfFile.CreateFile(dir, "setup2.xml", Encoding.UTF8.GetBytes(sb.ToString()));
    }

    private async Task BuildSingleplayerFilesAsync(SexType sex, byte[] ymtBytes, int counter, RpfDirectoryEntry cdimages)
    {
        var pedName = GetPedName(sex);
        var projectName = GetProjectName(counter);

        var drawables = _addon.Drawables.Where(x => x.Sex == SexType.male).ToList();
        var drawableGroups = drawables.Select((x, i) => new { Index = i, Value = x })
                                       .GroupBy(x => x.Value.Number / GlobalConstants.MAX_DRAWABLES_IN_ADDON)
                                       .Select(x => x.Select(v => v.Value).ToList())
                                       .ToList();

        var fileOperations = new List<Task>();

        var hasProps = drawables.Any(x => x.IsProp);

        var genderRpfName = sex == SexType.male ? "_male" : "_female";
        var componentsRpf = RpfFile.CreateNew(cdimages, projectName + genderRpfName + ".rpf");
        var componentsFolder = RpfFile.CreateDirectory(componentsRpf.Root, pedName + "_" + projectName);
        RpfFile propsRpf = null;
        RpfDirectoryEntry propsFolder = null;
        if (hasProps)
        {
            propsRpf = RpfFile.CreateNew(cdimages, projectName + genderRpfName + "_p" + ".rpf");
            propsFolder = RpfFile.CreateDirectory(propsRpf.Root, pedName + "_p_" + projectName);
        }

        RpfFile.CreateFile(componentsRpf.Root, $"{pedName}_{projectName}.ymt", ymtBytes);

        foreach (var group in drawableGroups)
        {
            foreach (var d in group)
            {
                var drawableBytes = File.ReadAllBytes(d.FilePath);

                RpfDirectoryEntry folder = d.IsProp ? propsFolder : componentsFolder;
                RpfFile.CreateFile(folder, $"{d.Name}{Path.GetExtension(d.FilePath)}", drawableBytes);

                foreach (var t in d.Textures)
                {
                    var displayName = RemoveInvalidChars(t.DisplayName);

                    if (t.IsOptimizedDuringBuild)
                    {
                        var optimizedBytes = await ImgHelper.Optimize(t);
                        RpfFile.CreateFile(folder, $"{displayName}{Path.GetExtension(t.FilePath)}", optimizedBytes);
                    }
                    else
                    {
                        var texBytes = File.ReadAllBytes(t.FilePath);
                        RpfFile.CreateFile(folder, $"{displayName}{Path.GetExtension(t.FilePath)}", texBytes);
                    }
                }
            }
        }
    }


    #endregion


    #region Generic

    private void AddBuildTasksForSex(Addon selectedAddon, SexType sexType, List<Task> tasks, List<string> metaFiles, int counter, RpfDirectoryEntry cdimages = null, RpfDirectoryEntry dataFolder = null)
    {
        if (selectedAddon.HasSex(sexType))
        {
            var bytes = BuildYMT(sexType);
            if (_buildResourceType == BuildResourceType.FiveM)
            {
                tasks.Add(BuildFiveMFilesAsync(sexType, bytes, counter));
            }
            else if (_buildResourceType == BuildResourceType.AltV)
            {
                tasks.Add(BuildAltVFilesAsync(sexType, bytes, counter));
            }
            else if (_buildResourceType == BuildResourceType.Singleplayer)
            {
                var (name, metaBytes) = BuildMeta(sexType);
                RpfFile.CreateFile(dataFolder, name, metaBytes);
                tasks.Add(BuildSingleplayerFilesAsync(sexType, bytes, counter, cdimages));
            }

            if (_buildResourceType != BuildResourceType.Singleplayer)
            {
                var (metaName, metaContent) = BuildMeta(sexType);
                metaFiles.Add(metaName);

                var path = _buildResourceType == BuildResourceType.FiveM ? Path.Combine(_buildPath, metaName) : Path.Combine(_buildPath, "stream", metaName);
                tasks.Add(File.WriteAllBytesAsync(path, metaContent));
            }
        }
    }

    private byte[] BuildYMT(SexType sex)
    {
        var mb = new MetaBuilder();
        var mdb = mb.EnsureBlock(MetaName.CPedVariationInfo);
        var CPed = new CPedVariationInfo
        {
            bHasDrawblVariations = 1,
            bHasTexVariations = 1,
            bHasLowLODs = 0,
            bIsSuperLOD = 0
        };

        ArrayOfBytes12 availComp = new();
        var generatedAvailComp = _addon.GenerateAvailComp();
        availComp.SetBytes(generatedAvailComp);
        CPed.availComp = availComp;

        var allDrawables = _addon.Drawables.Where(x => x.Sex == sex).ToList();
        var allCompDrawablesArray = allDrawables.Where(x => !x.IsProp).ToArray();
        var allPropDrawablesArray = allDrawables.Where(x => x.IsProp).ToArray();

        var components = new Dictionary<byte, CPVComponentData>();
        for (byte i = 0; i < generatedAvailComp.Length; i++)
        {
            if (generatedAvailComp[i] == 255) { continue; }

            var drawablesArray = allCompDrawablesArray.Where(x => x.TypeNumeric == i).ToArray();
            var drawables = new CPVDrawblData[drawablesArray.Length];

            for (int d = 0; d < drawables.Length; d++)
            {
                drawables[d].propMask = (byte)(drawablesArray[d].HasSkin ? 17 : 1);
                drawables[d].numAlternatives = (byte)(string.IsNullOrEmpty(drawablesArray[d].FirstPersonPath) ? 0 : 1);
                drawables[d].clothData = new CPVDrawblData__CPVClothComponentData() { ownsCloth = (byte)(string.IsNullOrEmpty(drawablesArray[d].ClothPhysicsPath) ? 0 : 1) };

                var texturesArray = drawablesArray[d].Textures.ToArray();
                var textures = new CPVTextureData[texturesArray.Length];
                for (int t = 0; t < textures.Length; t++)
                {
                    textures[t].texId = (byte)(drawablesArray[d].HasSkin ? 1 : 0);
                    textures[t].distribution = 255; //seems to be always 255
                }
                drawables[d].aTexData = mb.AddItemArrayPtr(MetaName.CPVTextureData, textures);
            };

            components[i] = new CPVComponentData()
            {
                numAvailTex = (byte)drawablesArray.Sum(y => y.Textures.Count),
                aDrawblData3 = mb.AddItemArrayPtr(MetaName.CPVDrawblData, drawables)
            };
        }

        CPed.aComponentData3 = mb.AddItemArrayPtr(MetaName.CPVComponentData, components.Values.ToArray());

        var compInfos = new CComponentInfo[allCompDrawablesArray.Length];
        for (int i = 0; i < compInfos.Length; i++)
        {
            var drawable = allCompDrawablesArray[i];
            compInfos[i].pedXml_audioID = JenkHash.GenHash(drawable.Audio);
            compInfos[i].pedXml_audioID2 = JenkHash.GenHash("none"); //todo
            compInfos[i].pedXml_expressionMods = new ArrayOfFloats5 { f0 = 0, f1 = 0, f2 = 0, f3 = 0, f4 = drawable.EnableHighHeels ? drawable.HighHeelsValue : 0 }; //expression mods
            compInfos[i].flags = (uint)drawable.Flags;
            compInfos[i].inclusions = 0;
            compInfos[i].exclusions = 0;
            compInfos[i].pedXml_vfxComps = ePedVarComp.PV_COMP_HEAD;
            compInfos[i].pedXml_flags = 0;
            compInfos[i].pedXml_compIdx = (byte)drawable.TypeNumeric;
            compInfos[i].pedXml_drawblIdx = (byte)drawable.Number;
        }

        CPed.compInfos = mb.AddItemArrayPtr(MetaName.CComponentInfo, compInfos);

        var propInfo = new CPedPropInfo
        {
            numAvailProps = (byte)allPropDrawablesArray.Length
        };

        var props = new CPedPropMetaData[allPropDrawablesArray.Length];
        for (int i = 0; i < props.Length; i++)
        {
            var prop = allPropDrawablesArray[i];
            props[i].audioId = JenkHash.GenHash(prop.Audio);
            props[i].expressionMods = new ArrayOfFloats5 { f0 = prop.EnableHairScale ? -prop.HairScaleValue : 0, f1 = 0, f2 = 0, f3 = 0, f4 = 0 };

            var texturesArray = prop.Textures.ToArray();
            var textures = new CPedPropTexData[texturesArray.Length];
            for (int t = 0; t < textures.Length; t++)
            {
                textures[t].inclusions = 0;
                textures[t].exclusions = 0;
                textures[t].texId = (byte)t;
                textures[t].inclusionId = 0;
                textures[t].exclusionId = 0;
                textures[t].distribution = 255;
            }

            ePropRenderFlags renderFlag = 0;
            if (Enum.TryParse(prop.RenderFlag, out ePropRenderFlags res))
            {
                renderFlag = res;
            }

            props[i].texData = mb.AddItemArrayPtr(MetaName.CPedPropTexData, textures);
            props[i].renderFlags = renderFlag;
            props[i].propFlags = (uint)prop.Flags;
            props[i].flags = 0;
            props[i].anchorId = (byte)prop.TypeNumeric;
            props[i].propId = (byte)prop.Number;
            props[i].Unk_2894625425 = 0;
        }
        propInfo.aPropMetaData = mb.AddItemArrayPtr(MetaName.CPedPropMetaData, props);

        var uniqueProps = allPropDrawablesArray.GroupBy(x => x.TypeNumeric).Select(g => g.First()).ToArray();
        var anchors = new CAnchorProps[uniqueProps.Length];
        for (int i = 0; i < anchors.Length; i++)
        {
            var propsOfType = allPropDrawablesArray.Where(x => x.TypeNumeric == uniqueProps[i].TypeNumeric);
            List<byte> items = propsOfType.Select(p => (byte)p.Textures.Count).ToList();

            anchors[i].props = mb.AddByteArrayPtr([.. items]);
            anchors[i].anchor = ((eAnchorPoints)propsOfType.First().TypeNumeric);
        }
        propInfo.aAnchors = mb.AddItemArrayPtr(MetaName.CAnchorProps, anchors);

        CPed.propInfo = propInfo;
        var projectName = GetProjectName();
        CPed.dlcName = JenkHash.GenHash(projectName);

        mb.AddItem(MetaName.CPedVariationInfo, CPed);

        mb.AddStructureInfo(MetaName.CPedVariationInfo);
        mb.AddStructureInfo(MetaName.CPedPropInfo);
        mb.AddStructureInfo(MetaName.CPedPropTexData);
        mb.AddStructureInfo(MetaName.CAnchorProps);
        mb.AddStructureInfo(MetaName.CComponentInfo);
        mb.AddStructureInfo(MetaName.CPVComponentData);
        mb.AddStructureInfo(MetaName.CPVDrawblData);
        mb.AddStructureInfo(MetaName.CPVDrawblData__CPVClothComponentData);
        mb.AddStructureInfo(MetaName.CPVTextureData);
        mb.AddStructureInfo(MetaName.CPedPropMetaData);
        mb.AddEnumInfo(MetaName.ePedVarComp);
        mb.AddEnumInfo(MetaName.eAnchorPoints);
        mb.AddEnumInfo(MetaName.ePropRenderFlags);

        Meta meta = mb.GetMeta();
        meta.Name = projectName;

        byte[] data = ResourceBuilder.Build(meta, 2);

        return data;
    }

    private (string, byte[]) BuildMeta(SexType sex)
    {
        var eCharacter = sex == SexType.male ? "SCR_CHAR_MULTIPLAYER" : "SCR_CHAR_MULTIPLAYER_F";
        var genderLetter = GetGenderLetter(sex);
        var pedName = GetPedName(sex);
        var projectName = GetProjectName();

        StringBuilder sb = new();
        sb.AppendLine(MetaXmlBase.XmlHeader);

        MetaXmlBase.OpenTag(sb, 0, "ShopPedApparel");
        MetaXmlBase.StringTag(sb, 4, "pedName", pedName);
        MetaXmlBase.StringTag(sb, 4, "dlcName", projectName);
        MetaXmlBase.StringTag(sb, 4, "fullDlcName", pedName + "_" + projectName);
        MetaXmlBase.StringTag(sb, 4, "eCharacter", eCharacter);
        MetaXmlBase.StringTag(sb, 4, "creatureMetaData", "mp_creaturemetadata_" + genderLetter + "_" + projectName);

        MetaXmlBase.OpenTag(sb, 4, "pedOutfits");
        MetaXmlBase.CloseTag(sb, 4, "pedOutfits");
        MetaXmlBase.OpenTag(sb, 4, "pedComponents");
        MetaXmlBase.CloseTag(sb, 4, "pedComponents");
        MetaXmlBase.OpenTag(sb, 4, "pedProps");
        MetaXmlBase.CloseTag(sb, 4, "pedProps");

        MetaXmlBase.CloseTag(sb, 0, "ShopPedApparel");

        var xml = sb.ToString();


        var name = pedName + "_" + projectName + ".meta";

        var bytes = Encoding.UTF8.GetBytes(xml);
        return (name, bytes);
    }

    private void BuildFirstPersonAlternatesMeta()
    {
        if (firstPersonFiles.Count > 0)
        {

            StringBuilder sb = new();
            sb.AppendLine(MetaXmlBase.XmlHeader);

            MetaXmlBase.OpenTag(sb, 0, "FirstPersonAlternateData");
            MetaXmlBase.OpenTag(sb, 4, "alternates");

            foreach (var file in firstPersonFiles)
            {
                MetaXmlBase.OpenTag(sb, 8, "Item");
                MetaXmlBase.StringTag(sb, 12, "assetName", file);
                MetaXmlBase.ValueTag(sb, 12, "alternate", "1");
                MetaXmlBase.CloseTag(sb, 8, "Item");
            }

            MetaXmlBase.CloseTag(sb, 4, "alternates");
            MetaXmlBase.CloseTag(sb, 0, "FirstPersonAlternateData");

            var finalPath = Path.Combine(_buildPath, $"first_person_alternates_{_projectName}.meta");
            File.WriteAllText(finalPath, sb.ToString());
        }
    }

    private static RbfFile GenerateCreatureMetadata(List<GDrawable> drawables)
    {
        //taken from ymteditor because it works fine xd
        var shouldGenCreatureHeels = drawables.Any(x => x.EnableHighHeels);
        var shouldGenCreatureHats = drawables.Any(x => x.EnableHairScale);
        if (!shouldGenCreatureHeels && !shouldGenCreatureHats) return null;

        XElement xml = new("CCreatureMetaData");
        XElement pedCompExpressions = new("pedCompExpressions");
        if (shouldGenCreatureHeels)
        {
            var feetDrawables = drawables.Where(x => x.TypeNumeric == 6 && x.IsComponent);
            foreach (var comp in feetDrawables)
            {
                XElement pedCompItem = new("Item");
                pedCompItem.Add(new XElement("pedCompID", new XAttribute("value", string.Format("0x{0:X}", 6))));
                pedCompItem.Add(new XElement("pedCompVarIndex", new XAttribute("value", string.Format("0x{0:X}", comp.Number))));
                pedCompItem.Add(new XElement("pedCompExpressionIndex", new XAttribute("value", string.Format("0x{0:X}", 4))));
                pedCompItem.Add(new XElement("tracks", new XAttribute("content", "char_array"), 33));
                pedCompItem.Add(new XElement("ids", new XAttribute("content", "short_array"), 28462));
                pedCompItem.Add(new XElement("types", new XAttribute("content", "char_array"), 2));
                pedCompItem.Add(new XElement("components", new XAttribute("content", "char_array"), 1));
                pedCompExpressions.Add(pedCompItem);
            }
        }
        xml.Add(pedCompExpressions);

        XElement pedPropExpressions = new("pedPropExpressions");
        if (shouldGenCreatureHats)
        {
            //all original GTA have that one first entry, without it, fivem was sometimes crashing(?)
            XElement FirstpedPropItem = new("Item");
            FirstpedPropItem.Add(new XElement("pedPropID", new XAttribute("value", string.Format("0x{0:X}", 0))));
            FirstpedPropItem.Add(new XElement("pedPropVarIndex", new XAttribute("value", string.Format("0x{0:X}", -1))));
            FirstpedPropItem.Add(new XElement("pedPropExpressionIndex", new XAttribute("value", string.Format("0x{0:X}", -1))));
            FirstpedPropItem.Add(new XElement("tracks", new XAttribute("content", "char_array"), 33));
            FirstpedPropItem.Add(new XElement("ids", new XAttribute("content", "short_array"), 13201));
            FirstpedPropItem.Add(new XElement("types", new XAttribute("content", "char_array"), 2));
            FirstpedPropItem.Add(new XElement("components", new XAttribute("content", "char_array"), 1));
            pedPropExpressions.Add(FirstpedPropItem);

            foreach (var prop in drawables.Where(x => x.TypeNumeric == 0 && x.IsProp))
            {
                XElement pedPropItem = new("Item");
                pedPropItem.Add(new XElement("pedPropID", new XAttribute("value", string.Format("0x{0:X}", 0))));
                pedPropItem.Add(new XElement("pedPropVarIndex", new XAttribute("value", string.Format("0x{0:X}", prop.Number))));
                pedPropItem.Add(new XElement("pedPropExpressionIndex", new XAttribute("value", string.Format("0x{0:X}", 0))));
                pedPropItem.Add(new XElement("tracks", new XAttribute("content", "char_array"), 33));
                pedPropItem.Add(new XElement("ids", new XAttribute("content", "short_array"), 13201));
                pedPropItem.Add(new XElement("types", new XAttribute("content", "char_array"), 2));
                pedPropItem.Add(new XElement("components", new XAttribute("content", "char_array"), 1));
                pedPropExpressions.Add(pedPropItem);
            }
        }
        xml.Add(pedPropExpressions);

        //create XmlDocument from XElement
        var xmldoc = new XmlDocument();
        xmldoc.Load(xml.CreateReader());

        RbfFile rbf = XmlRbf.GetRbf(xmldoc);
        return rbf;
    }

    private static string RemoveInvalidChars(string input)
    {
        return string.Concat(input.Split(Path.GetInvalidFileNameChars()));
    }

    private static string GetGenderLetter(SexType sex)
    {
        return sex switch
        {
            SexType.male => "m",
            SexType.female => "f",
            _ => throw new ArgumentOutOfRangeException(nameof(sex), sex, "Wrong sexType GetGenderLetter")
        };
    }

    private static string GetPedName(SexType sex)
    {
        return sex switch
        {
            SexType.male => "mp_m_freemode_01",
            SexType.female => "mp_f_freemode_01",
            _ => throw new ArgumentOutOfRangeException(nameof(sex), sex, "Wrong sexType GetPedName")
        };
    }

    #endregion
}
