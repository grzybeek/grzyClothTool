using CodeWalker.GameFiles;
using grzyClothTool.Constants;
using grzyClothTool.Controls;
using grzyClothTool.Extensions;
using grzyClothTool.Helpers;
using grzyClothTool.Models.Drawable;
using grzyClothTool.Models.Other;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace grzyClothTool.Models
{

    public class AddonManagerDesign : AddonManager
    {
        public AddonManagerDesign()
        {
            ProjectName = "Design";
            Addons = [];

            Addons.Add(new Addon("design"));
            SelectedAddon = Addons.First();
        }
    }

    public class AddonManager : INotifyPropertyChanged
    {
        private abstract record WorkItem;
        private record DrawableWorkItem(
            string FilePath,
            Enums.SexType Sex,
            string BasePath,
            PedFile Ymt,
            PedAlternativeVariations PedAltVariations,
            Dictionary<(int, int), MCComponentInfo> CompInfoDict,
            Dictionary<(int, int), MCPedPropMetaData> PedPropMetaDataDict,
            Dictionary<(int, bool), int> TypeNumericCounts
        ) : WorkItem;
        private record CompletionMarker(TaskCompletionSource Tcs) : WorkItem;

        private readonly BlockingCollection<WorkItem> _drawableQueue = new();
        private readonly Task _drawableProcessingTask;

        public static readonly object AddonsLock = new();

        private static readonly Regex AlternateRegex = new(@"_\w_\d+\.ydd$", RegexOptions.Compiled);
        private static readonly Regex PhysicsRegex = new(@"\.yld$", RegexOptions.Compiled);

        public event PropertyChangedEventHandler PropertyChanged;

        public string ProjectName { get; set; }

        [JsonInclude]
        private string SavedAt => DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");

        public ObservableCollection<string> Groups { get; set; } = [];
        
        [JsonIgnore]
        public ObservableCollection<MoveMenuItem> MoveMenuItems { get; set; } = [];

        private ObservableCollection<Addon> _addons = [];
        public ObservableCollection<Addon> Addons
        {
            get { return _addons; }
            set
            {
                if (_addons != value)
                {
                    _addons = value;
                    OnPropertyChanged();
                }
            }
        }

        private Addon _selectedAddon;
        [JsonIgnore]
        public Addon SelectedAddon
        {
            get { return _selectedAddon; }
            set
            {
                if (_selectedAddon != value)
                {
                    _selectedAddon = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isPreviewEnabled;
        [JsonIgnore]
        public bool IsPreviewEnabled
        {
            get { return _isPreviewEnabled; }
            set
            {
                _isPreviewEnabled = value;
                OnPropertyChanged();
            }
        }

        public AddonManager()
        {
            _drawableProcessingTask = Task.Run(ProcessDrawableQueue);
        }

        public void CreateAddon()
        {
            var name = "Addon " + (Addons.Count + 1);

            Addons.Add(new Addon(name));
            OnPropertyChanged("Addons");
        }

        private async Task<PedAlternativeVariations> LoadPedAlternativeVariationsFileAsync(string dirPath, string addonName)
        {
            try
            {
                var pedAltVariationsFiles = await Task.Run(() => 
                    Directory.GetFiles(dirPath, "pedalternativevariations*.meta", SearchOption.AllDirectories)
                        .Where(x => x.Contains(addonName))
                        .ToArray());

                if (pedAltVariationsFiles.Length == 0)
                {
                    return null;
                }

                var pedAltVariationsFile = pedAltVariationsFiles.FirstOrDefault();
                if (pedAltVariationsFile == null)
                {
                    return null;
                }

                var xmlDoc = await Task.Run(() => XDocument.Load(pedAltVariationsFile));
                return PedAlternativeVariations.FromXml(xmlDoc);
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Error loading pedalternativevariations.meta: {ex.Message}", Views.LogType.Warning);
                return null;
            }
        }

        public async Task LoadAddon(string path, bool shouldSetProjectName = false)
        {
            var dirPath = Path.GetDirectoryName(path);
            var addonName = Path.GetFileNameWithoutExtension(path);

            // Determine if the addonName indicates male or female
            Enums.SexType sex = addonName.Contains("mp_m_freemode_01") ? Enums.SexType.male : Enums.SexType.female;

            // Build the appropriate regex pattern based on whether it's male or female
            string genderSpecificPart = sex == Enums.SexType.male ? "mp_m_freemode_01" : "mp_f_freemode_01";
            string addonNameWithoutGender = addonName.Replace(genderSpecificPart, "").TrimStart('_');

            if (shouldSetProjectName)
            {
                MainWindow.AddonManager.ProjectName = addonNameWithoutGender;
            }

            var (yddFiles, ymtFile, yldFiles) = await Task.Run(() =>
            {
                string pattern = $@"^{genderSpecificPart}(_p)?.*?{Regex.Escape(addonNameWithoutGender)}\^";
                var compiledPattern = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);

                var allFiles = Directory.GetFiles(dirPath, "*.*", SearchOption.AllDirectories);
                
                var ydds = allFiles
                    .Where(f => f.EndsWith(".ydd", StringComparison.OrdinalIgnoreCase))
                    .Where(f => compiledPattern.IsMatch(Path.GetFileName(f)))
                    .OrderBy(x =>
                    {
                        var number = FileHelper.GetDrawableNumberFromFileName(Path.GetFileName(x));
                        return number ?? int.MaxValue;
                    })
                    .ThenBy(Path.GetFileName)
                    .ToArray();

                var ymt = allFiles
                    .Where(f => f.EndsWith(".ymt", StringComparison.OrdinalIgnoreCase))
                    .FirstOrDefault(x => x.Contains(addonName));

                var ylds = allFiles
                    .Where(f => f.EndsWith(".yld", StringComparison.OrdinalIgnoreCase))
                    .Where(f => compiledPattern.IsMatch(Path.GetFileName(f)))
                    .OrderBy(x =>
                    {
                        var number = FileHelper.GetDrawableNumberFromFileName(Path.GetFileName(x));
                        return number ?? int.MaxValue;
                    })
                    .ThenBy(Path.GetFileName)
                    .ToArray();

                return (ydds, ymt, ylds);
            });

            if (yddFiles.Length == 0)
            {
                CustomMessageBox.Show($"No .ydd files found for selected .meta file ({Path.GetFileName(path)})", "Error");
                return;
            }

            if (ymtFile == null)
            {
                CustomMessageBox.Show($"No .ymt file found for selected .meta file ({Path.GetFileName(path)})", "Error");
                return;
            }

            var ymt = new PedFile();
            var ymtBytes = await FileHelper.ReadAllBytesAsync(ymtFile);
            RpfFile.LoadResourceFile(ymt, ymtBytes, 2);
            
            var pedAltVariations = await LoadPedAlternativeVariationsFileAsync(dirPath, addonNameWithoutGender);
            
            //merge ydd with yld files
            var mergedFiles = yddFiles.Concat(yldFiles).ToArray();

            await AddDrawables(mergedFiles, sex, ymt, dirPath, pedAltVariations);
        }

        public Task AddDrawables(string[] filePaths, Enums.SexType sex, PedFile ymt = null, string basePath = null, PedAlternativeVariations pedAltVariations = null)
        {
            var tcs = new TaskCompletionSource();

            // We need to count how many drawables of each type we have added so far
            // this is because if we are loading from ymt file, numbers are relative to this ymt file, once adding it to existing project
            // we need to adjust numbers to get proper properties
            Dictionary<(int, bool), int> typeNumericCounts = [];

            //read properties from provided ymt file if there is any
            Dictionary<(int, int), MCComponentInfo> compInfoDict = [];
            Dictionary<(int, int), MCPedPropMetaData> pedPropMetaDataDict = [];
            if (ymt is not null)
            {
                var hasCompInfos = ymt.VariationInfo.CompInfos != null;
                if (hasCompInfos)
                {
                    foreach (var compInfo in ymt.VariationInfo.CompInfos)
                    {
                        var key = (compInfo.ComponentType, compInfo.ComponentIndex);
                        compInfoDict[key] = compInfo;
                    }
                }

                var hasProps = ymt.VariationInfo.PropInfo.PropMetaData != null && ymt.VariationInfo.PropInfo.Data.numAvailProps > 0;
                if (hasProps)
                {
                    foreach (var pedPropMetaData in ymt.VariationInfo.PropInfo.PropMetaData)
                    {
                        var key = (pedPropMetaData.Data.anchorId, pedPropMetaData.Data.propId);
                        pedPropMetaDataDict[key] = pedPropMetaData;
                    }
                }
            }

            foreach (var filePath in filePaths)
            {
                var workItem = new DrawableWorkItem(
                    filePath,
                    sex,
                    basePath,
                    ymt,
                    pedAltVariations,
                    compInfoDict,
                    pedPropMetaDataDict,
                    typeNumericCounts
                );
                _drawableQueue.Add(workItem);
            }

            _drawableQueue.Add(new CompletionMarker(tcs));
            return tcs.Task;
        }

        private async void ProcessDrawableQueue()
        {
            foreach (var workItem in _drawableQueue.GetConsumingEnumerable())
            {
                if (workItem is CompletionMarker marker)
                {
                    marker.Tcs.SetResult();
                    continue;
                }

                var (filePath, sex, basePath, ymt, pedAltVariations, compInfoDict, pedPropMetaDataDict, typeNumericCounts) = (DrawableWorkItem)workItem;

                var (isProp, drawableType) = FileHelper.ResolveDrawableType(filePath);
                if (drawableType == -1)
                {
                    continue;
                }

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (Addons.Count == 0)
                    {
                        CreateAddon();
                    }
                });

                var drawablesOfType = new List<GDrawable>();
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    foreach (var addon in Addons)
                    {
                        drawablesOfType.AddRange(addon.Drawables.Where(x => x.TypeNumeric == drawableType && x.IsProp == isProp && x.Sex == sex));
                    }
                });

                if (AlternateRegex.IsMatch(filePath))
                {
                    if (filePath.EndsWith("_1.ydd"))
                    {
                        var number = FileHelper.GetDrawableNumberFromFileName(Path.GetFileName(filePath));
                        if (number == null)
                        {
                            LogHelper.Log($"Could not find associated YDD file for first person file: {filePath}, please do it manually", Views.LogType.Warning);
                            continue;
                        }

                        var foundDrawable = drawablesOfType.FirstOrDefault(x => x.Number == number);
                        if (foundDrawable != null)
                        {
                            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => foundDrawable.FirstPersonPath = filePath);
                        }
                    }
                    continue;
                }

                if (PhysicsRegex.IsMatch(filePath))
                {
                    var number = FileHelper.GetDrawableNumberFromFileName(Path.GetFileName(filePath));
                    if (number == null)
                    {
                        LogHelper.Log($"Could not find associated YDD file for this YLD: {filePath}, please do it manually", Views.LogType.Warning);
                        continue;
                    }

                    var foundDrawable = drawablesOfType.FirstOrDefault(x => x.Number == number);
                    if (foundDrawable != null)
                    {
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => foundDrawable.ClothPhysicsPath = filePath);
                    }
                    continue;
                }

                var drawable = await FileHelper.CreateDrawableAsync(filePath, sex, isProp, drawableType, 0); // Number is set by AddDrawable
                if (!string.IsNullOrEmpty(basePath) && filePath.StartsWith(basePath))
                {
                    var extractedGroup = ExtractGroupFromPath(filePath, basePath, sex, isProp);
                    if (!string.IsNullOrWhiteSpace(extractedGroup))
                    {
                        drawable.Group = extractedGroup;
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            if (!Groups.Contains(extractedGroup))
                            {
                                Groups.Add(extractedGroup);
                                GroupManager.Instance.AddGroup(extractedGroup);
                            }
                        });
                    }
                }

                if (ymt is not null)
                {
                    var key = (drawableType, isProp);
                    if (typeNumericCounts.TryGetValue(key, out int value))
                    {
                        typeNumericCounts[key] = ++value;
                    }
                    else
                    {
                        typeNumericCounts[key] = 1;
                    }

                    var ymtKey = (drawable.TypeNumeric, typeNumericCounts[(drawable.TypeNumeric, drawable.IsProp)] - 1);
                    if (compInfoDict.TryGetValue(ymtKey, out MCComponentInfo compInfo))
                    {
                        var list = EnumHelper.GetFlags((int)compInfo.Data.flags);
                        drawable.Audio = compInfo.Data.pedXml_audioID.ToString();
                        drawable.SelectedFlags = list.ToObservableCollection();
                        if (compInfo.Data.pedXml_expressionMods.f4 != 0)
                        {
                            drawable.EnableHighHeels = true;
                            drawable.HighHeelsValue = compInfo.Data.pedXml_expressionMods.f4;
                        }
                    }

                    if (drawable.IsProp && pedPropMetaDataDict.TryGetValue(ymtKey, out MCPedPropMetaData pedPropMetaData))
                    {
                        drawable.Audio = pedPropMetaData.Data.audioId.ToString();
                        drawable.RenderFlag = pedPropMetaData.Data.renderFlags.ToString();
                        var list = EnumHelper.GetFlags((int)pedPropMetaData.Data.propFlags);
                        drawable.SelectedFlags = list.ToObservableCollection();
                        if (pedPropMetaData.Data.expressionMods.f0 != 0)
                        {
                            drawable.EnableHairScale = true;
                            drawable.HairScaleValue = Math.Abs(pedPropMetaData.Data.expressionMods.f0);
                        }
                    }
                }

                if (pedAltVariations != null && !drawable.IsProp)
                {
                    string pedName = sex == Enums.SexType.male ? "mp_m_freemode_01" : "mp_f_freemode_01";
                    var pedVariation = pedAltVariations.Peds.FirstOrDefault(p => p.Name == pedName);
                    if (pedVariation != null)
                    {
                        foreach (var alternateSwitch in pedVariation.Switches)
                        {
                            var matchingAsset = alternateSwitch.SourceAssets.FirstOrDefault(asset =>
                                asset.Component == drawable.TypeNumeric && asset.Index == drawable.Number);
                            if (matchingAsset != null)
                            {
                                drawable.HidesHair = true;
                                break;
                            }
                        }
                    }
                }

                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    AddDrawable(drawable);
                    Addons.Sort(true);
                });
            }
        }


        /// <summary>
        /// Extracts group path from file path relative to base path
        /// Expected structure for FiveM: basePath/stream/[gender]/[group]/[type]/file.ydd
        /// </summary>
        private static string ExtractGroupFromPath(string filePath, string basePath, Enums.SexType sex, bool isProp)
        {
            try
            {
                filePath = Path.GetFullPath(filePath);
                basePath = Path.GetFullPath(basePath);

                var relativePath = Path.GetRelativePath(basePath, Path.GetDirectoryName(filePath));
                
                var parts = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                
                // Expected structure: stream/[gender]/[group(s)]/[type]
                if (parts.Length > 3)
                {
                    int genderIndex = -1;
                    string expectedGenderFolder = sex == Enums.SexType.male ? "[male]" : "[female]";
                    
                    for (int i = 0; i < parts.Length; i++)
                    {
                        if (parts[i].Equals(expectedGenderFolder, StringComparison.OrdinalIgnoreCase))
                        {
                            genderIndex = i;
                            break;
                        }
                    }
                    
                    if (genderIndex >= 0 && genderIndex < parts.Length - 2)
                    {
                        var groupParts = parts.Skip(genderIndex + 1).Take(parts.Length - genderIndex - 2).ToArray();
                        if (groupParts.Length > 0)
                        {
                            return string.Join("/", groupParts);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Error extracting group from path: {ex.Message}", Views.LogType.Warning);
            }
            
            return null;
        }

        public void AddDrawable(GDrawable drawable)
        {
            lock (AddonsLock)
            {
                int nextNumber = 0;
                int currentAddonIndex = 0;
                Addon currentAddon;

                // find to which addon we should add the drawable
                while (currentAddonIndex < Addons.Count)
                {
                    currentAddon = Addons[currentAddonIndex];
                    int countOfType = currentAddon.Drawables.Count(x => x.TypeNumeric == drawable.TypeNumeric && x.IsProp == drawable.IsProp && x.Sex == drawable.Sex);

                    // If the number of drawables of this type has reached 128, move to the next addon
                    if (countOfType >= GlobalConstants.MAX_DRAWABLES_IN_ADDON)
                    {
                        currentAddonIndex++;
                        continue;
                    }

                    nextNumber = countOfType;
                    break;
                }

                // make sure we are adding to correct addon
                if (currentAddonIndex < Addons.Count)
                {
                    currentAddon = Addons[currentAddonIndex];
                }
                else
                {
                    // Create a new Addon
                    currentAddon = new Addon("Addon " + (currentAddonIndex + 1));
                    Addons.Add(currentAddon);
                }

                // Update name and number
                // mark as new, to make it easier to find
                drawable.IsNew = true;
                drawable.Number = nextNumber;
                drawable.SetDrawableName();

                // drawable.IsEncrypted = IsDrawableEncrypted(drawable.FilePath);
                currentAddon.Drawables.Add(drawable);

                SaveHelper.SetUnsavedChanges(true);
            }
        }

        public void DeleteDrawables(List<GDrawable> drawables)
        {
            SaveHelper.SetUnsavedChanges(true);
            var addon = SelectedAddon;
            foreach (GDrawable drawable in drawables)
            {
                addon.Drawables.Remove(drawable);

                if (SettingsHelper.Instance.AutoDeleteFiles)
                {
                    foreach (var texture in drawable.Textures)
                    {
                        File.Delete(texture.FilePath);
                    }
                    File.Delete(drawable.FilePath);
                }
            }

            // if addon is empty, remove it
            if (addon.Drawables.Count == 0)
            {
                DeleteAddon(addon);
                return;
            }

            addon.Drawables.Sort(true);
        }

        public void MoveDrawable(GDrawable drawable, Addon targetAddon)
        {
            if (drawable == null || targetAddon == null)
            {
                var nullParam = drawable == null ? nameof(drawable) : nameof(targetAddon);
                throw new ArgumentNullException(nullParam, $"{nullParam} cannot be null.");
            }

            var currentAddon = Addons.FirstOrDefault(a => a.Drawables.Contains(drawable));
            if (currentAddon != null)
            {
                currentAddon.Drawables.Remove(drawable);

                drawable.Number = currentAddon.GetNextDrawableNumber(drawable.TypeNumeric, drawable.IsProp, drawable.Sex);
                drawable.SetDrawableName();

                targetAddon.Drawables.Add(drawable);
            }
        }

        public int GetTotalDrawableAndTextureCount()
        {
            return Addons.Sum(addon => addon.GetTotalDrawableAndTextureCount());
        }

        private void DeleteAddon(Addon addon)
        {
            if (Addons.Count <= 1)
            {
                return;
            }

            int index = Addons.IndexOf(addon);
            if (index < 0) { return; } // if not found, don't remove

            Addons.RemoveAt(index);
            AdjustAddonNames();
        }

        private void AdjustAddonNames()
        {
            for (int i = 0; i < Addons.Count; i++)
            {
                Addons[i].Name = $"Addon {i + 1}";
            }

            OnPropertyChanged("Addons");
        }

        private static bool IsDrawableEncrypted(string filePath)
        {
            // RSC7 magic = 0x37435352 ("RSC7")
            const uint MagicRsc7 = 0x37435352;
            Span<byte> buffer = stackalloc byte[4];

            using var fs = new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite,
                4096,
                FileOptions.SequentialScan
            );
            
            int read = fs.Read(buffer);
            if (read < 4)
                return true;

            uint magic = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(buffer);
            return magic != MagicRsc7;
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
