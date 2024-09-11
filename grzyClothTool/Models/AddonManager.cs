using CodeWalker.GameFiles;
using grzyClothTool.Controls;
using grzyClothTool.Extensions;
using grzyClothTool.Helpers;
using grzyClothTool.Models.Drawable;
using grzyClothTool.Models.Texture;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace grzyClothTool.Models
{

    public class AddonManagerDesign : AddonManager
    {
        public AddonManagerDesign()
        {
            Addons = [];

            Addons.Add(new Addon("design"));
            SelectedAddon = Addons.First();
        }
    }

    public class AddonManager : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [JsonProperty]
        private string SavedAt => DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss");

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
        }

        public void ResetDuplicateSearch()
        {
            GDrawable.hashes = Tuple.Create(new Dictionary<string, List<GDrawable>>(), new Dictionary<string, List<GDrawable>>());
            GTexture.hashes = Tuple.Create(new Dictionary<string, Dictionary<string, List<GTexture>>>(), new Dictionary<string, Dictionary<string, List<GTexture>>>());
        }

        public void CreateAddon()
        {
            var name = "Addon " + (Addons.Count + 1);

            Addons.Add(new Addon(name));
            OnPropertyChanged("Addons");
        }

        public async Task LoadAddon(string path)
        {
            var dirPath = Path.GetDirectoryName(path);
            var addonName = Path.GetFileNameWithoutExtension(path);

            var yddFiles = Directory.GetFiles(dirPath, "*.ydd", SearchOption.AllDirectories)
                .Where(x => x.Contains(addonName + "^") || x.Contains(addonName + "_p^p_") || (x.Contains("_p_") && x.Replace("_p_", "_").Contains(addonName + "^p_")))
                .ToArray();

            var ymtFile = Directory.GetFiles(dirPath, "*.ymt", SearchOption.AllDirectories)
                .Where(x => x.Contains(addonName))
                .FirstOrDefault();

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

            var isMale = !addonName.Contains('_') || !(addonName.Split("_")[1] == "f");

            var ymt = new PedFile();
            RpfFile.LoadResourceFile(ymt, File.ReadAllBytes(ymtFile), 2);

            Dictionary<(int, int), MCComponentInfo> compInfoDict = [];
            var hasCompInfos = ymt.VariationInfo.CompInfos != null;
            if (hasCompInfos)
            {
                foreach (var compInfo in ymt.VariationInfo.CompInfos)
                {
                    var key = (compInfo.ComponentType, compInfo.ComponentIndex);
                    compInfoDict[key] = compInfo;
                }
            }

            Dictionary<(int, int), MCPedPropMetaData> pedPropMetaDataDict = [];
            var hasProps = ymt.VariationInfo.PropInfo.PropMetaData != null && ymt.VariationInfo.PropInfo.Data.numAvailProps > 0;
            if (hasProps) 
            {
                foreach (var pedPropMetaData in ymt.VariationInfo.PropInfo.PropMetaData)
                {
                    var key = (pedPropMetaData.Data.anchorId, pedPropMetaData.Data.propId);
                    pedPropMetaDataDict[key] = pedPropMetaData;
                }
            }
            
            var drawablesAdded = await AddDrawables(yddFiles, isMale);

            foreach (var drawable in drawablesAdded)
            {
                var number = drawablesAdded.FindAll(x => x.TypeNumeric == drawable.TypeNumeric && x.IsProp == drawable.IsProp && x.Sex == drawable.Sex).IndexOf(drawable);
                var key = (drawable.TypeNumeric, number);
                    if (compInfoDict.TryGetValue(key, out MCComponentInfo compInfo))
                    {
                        drawable.Audio = compInfo.Data.pedXml_audioID.ToString();

                        var list = EnumHelper.GetFlags((int)compInfo.Data.flags);
                        drawable.SelectedFlags = list.ToObservableCollection();

                        if (compInfo.Data.pedXml_expressionMods.f4 != 0)
                        {
                            drawable.EnableHighHeels = true;
                            drawable.HighHeelsValue = compInfo.Data.pedXml_expressionMods.f4;
                        }
                    }

                    if (drawable.IsProp)
                    {
                    var propKey = (drawable.TypeNumeric, number);
                        if (pedPropMetaDataDict.TryGetValue(propKey, out MCPedPropMetaData pedPropMetaData))
                        {
                            drawable.Audio = pedPropMetaData.Data.audioId.ToString();
                            drawable.RenderFlag = pedPropMetaData.Data.renderFlags.ToString();

                            var list = EnumHelper.GetFlags((int)pedPropMetaData.Data.propFlags);
                            drawable.SelectedFlags = list.ToObservableCollection();

                            if (pedPropMetaData.Data.expressionMods.f0 != 0)
                            {
                                drawable.EnableHairScale = true;

                                // grzyClothTool saves hairScaleValue as positive number, on resource build it makes it negative
                                drawable.HairScaleValue = Math.Abs(pedPropMetaData.Data.expressionMods.f0); 
                            }
                        }
                    }
                }
            }

        public async Task<List<GDrawable>> AddDrawables(string[] filePaths, bool isMale)
        {
            List<GDrawable> drawablesAdded = [];
            Regex alternateRegex = new(@"_\w_\d+\.ydd$");
            foreach (var filePath in filePaths)
            {
                var (isProp, drawableType) = FileHelper.ResolveDrawableType(filePath);
                if (drawableType == -1)
                {
                    continue;
                }

                if (alternateRegex.IsMatch(filePath))
                {
                    continue;
                }

                if(Addons.Count == 0)
                {
                    CreateAddon();
                }

                // Start from the first Addon
                var currentAddonIndex = 0;
                Addon currentAddon = Addons[currentAddonIndex];

                // Calculate countOfType for the current Addon
                var countOfType = currentAddon.Drawables.Count(x => x.TypeNumeric == drawableType && x.IsProp == isProp && x.Sex == isMale);
                var drawable = await Task.Run(() => FileHelper.CreateDrawableAsync(filePath, isMale, isProp, drawableType, countOfType));

                // Check if the number of drawables of this type has reached 128
                while (countOfType >= GlobalConstants.MAX_DRAWABLES_IN_ADDON)
                {
                    // Move to the next Addon
                    currentAddonIndex++;
                    if (currentAddonIndex < Addons.Count)
                    {
                        // Get the next Addon
                        currentAddon = Addons[currentAddonIndex];
                    }
                    else
                    {
                        // Create a new Addon
                        currentAddon = new Addon("Addon " + (currentAddonIndex + 1));
                        Addons.Add(currentAddon);
                    }

                    // Calculate countOfType for the current Addon
                    countOfType = currentAddon.Drawables.Count(x => x.TypeNumeric == drawableType && x.IsProp == isProp && x.Sex == isMale);

                    // Update name and number
                    drawable.Number = countOfType;
                    drawable.SetDrawableName();
                }

                // Add the drawable to the current Addon
                currentAddon.Drawables.Add(drawable);
                drawablesAdded.Add(drawable);

                //set HasMale/HasFemale/HasProps only once adding first drawable
                if (isMale && !currentAddon.HasMale) currentAddon.HasMale = true;
                if (!isMale && !currentAddon.HasFemale) currentAddon.HasFemale = true;
                if (isProp && !currentAddon.HasProps) currentAddon.HasProps = true;
            }

            // Sort the ObservableCollection in place, in all existing addons
            foreach (var addon in Addons)
            {
                addon.Drawables.Sort();
            }

            return drawablesAdded;
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
