using grzyClothTool.Controls;
using grzyClothTool.Extensions;
using grzyClothTool.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace grzyClothTool.Models;

public class Addon : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    public string Name { get; set; }

    public bool HasFemale { get; set; }
    public bool HasMale { get; set; }
    public bool HasProps { get; set; }
    private bool _isPreviewEnabled;
    public bool IsPreviewEnabled
    {
        get { return _isPreviewEnabled; }
        set
        {
            _isPreviewEnabled = value;
            OnPropertyChanged();
        }
    }

    [JsonIgnore]
    public bool TriggerSelectedDrawableUpdatedEvent { get; set; }

    private GDrawable _selectedDrawable;
    [JsonIgnore]
    public GDrawable SelectedDrawable
    {
        get { return _selectedDrawable; }
        set
        {
            TriggerSelectedDrawableUpdatedEvent = false;
            _selectedDrawable = value;
            OnPropertyChanged();
            TriggerSelectedDrawableUpdatedEvent = true;
        }
    }

    private GTexture _selectedTexture;
    [JsonIgnore]
    public GTexture SelectedTexture
    {
        get { return _selectedTexture; }
        set
        {
            _selectedTexture = value;
            OnPropertyChanged();
        }
    }

    private ObservableCollection<GDrawable> _drawables;

    public ObservableCollection<GDrawable> Drawables
    {
        get { return _drawables; }
        set
        {
            if (_drawables != value)
            {
                _drawables = value;
                OnPropertyChanged();
            }
        }
    }

    public Addon(string projectName)
    {
        Name = projectName;

        Drawables = [];

        if (projectName == "design")
        {
            for (int i = 1; i < 50; i++)
            {
                Drawables.Add(
                    new GDrawable("grzyClothTool/Assets/jbib_000_u.ydd", i % 2 == 0, false, 11, i, false,
                    [
                        new("grzyClothTool/Assets/jbib_diff_000_a_uni.ytd", 11, 0, 0, false, false), 
                        new("grzyClothTool/Assets/jbib_diff_000_a_uni.ytd", 11, 0, 1, false, false)
                    ]
                ));
            }

            SelectedDrawable = Drawables.First();
        }
    }
    public async Task AddDrawables(string[] filePaths, bool isMale)
    {

        Regex alternateRegex = new(@"_\w_\d+\.ydd$");
        foreach (var filePath in filePaths)
        {
            var (isProp, drawableType) = FileHelper.ResolveDrawableType(filePath);
            if (drawableType == -1)
            {
                continue;
            }

            if(alternateRegex.IsMatch(filePath))
            {
                continue;
            }

            // Start from the first Addon
            var currentAddonIndex = 0;
            Addon currentAddon = MainWindow.AddonManager.Addons[currentAddonIndex];

            // Calculate countOfType for the current Addon
            var countOfType = currentAddon.Drawables.Count(x => x.TypeNumeric == drawableType && x.IsProp == isProp && x.Sex == isMale);
            var drawable = await Task.Run(() => FileHelper.CreateDrawableAsync(filePath, isMale, isProp, drawableType, countOfType));

            // Check if the number of drawables of this type has reached 128
            while (countOfType >= GlobalConstants.MAX_DRAWABLES_IN_ADDON)
            {
                // Move to the next Addon
                currentAddonIndex++;
                if (currentAddonIndex < MainWindow.AddonManager.Addons.Count)
                {
                    // Get the next Addon
                    currentAddon = MainWindow.AddonManager.Addons[currentAddonIndex];
                }
                else
                {
                    // Create a new Addon
                    currentAddon = new Addon("Addon " + (currentAddonIndex + 1));
                    MainWindow.AddonManager.Addons.Add(currentAddon);
                }

                // Calculate countOfType for the current Addon
                countOfType = currentAddon.Drawables.Count(x => x.TypeNumeric == drawableType && x.IsProp == isProp && x.Sex == isMale);

                // Update name and number
                drawable.Number = countOfType;
                drawable.SetDrawableName();
            }

            // Add the drawable to the current Addon
            currentAddon.Drawables.Add(drawable);

            //set HasMale/HasFemale/HasProps only once adding first drawable
            if (isMale && !currentAddon.HasMale) currentAddon.HasMale = true;
            if (!isMale && !currentAddon.HasFemale) currentAddon.HasFemale = true;
            if (isProp && !currentAddon.HasProps) currentAddon.HasProps = true;
        }

        // Sort the ObservableCollection in place, in all existing addons
        foreach (var addon in MainWindow.AddonManager.Addons)
        {
            addon.Drawables.Sort();
        }
    }

    public int GetNextDrawableNumber(int typeNumeric, bool isProp, bool isMale)
    {
        var nextNumber = 0;
        var currentAddonIndex = 0;

        while (currentAddonIndex < MainWindow.AddonManager.Addons.Count)
        {
            var currentAddon = MainWindow.AddonManager.Addons[currentAddonIndex];
            var countOfType = currentAddon.Drawables.Count(x => x.TypeNumeric == typeNumeric && x.IsProp == isProp && x.Sex == isMale);

            // If the number of drawables of this type has reached 128, move to the next addon
            if (countOfType >= GlobalConstants.MAX_DRAWABLES_IN_ADDON)
            {
                currentAddonIndex++;
                continue;
            }

            nextNumber = countOfType;
            break;
        }

        return nextNumber;
    }

    public async void LoadAddon(string path)
    {
        var dirPath = Path.GetDirectoryName(path);
        var addonName = Path.GetFileNameWithoutExtension(path);

        // find all .ydd files within all folders that contain addonName in name
        var yddFiles = Directory.GetFiles(dirPath, "*.ydd", SearchOption.AllDirectories)
            .Where(x => x.Contains(addonName))
            .ToArray();

        if (yddFiles.Length == 0)
        {
            CustomMessageBox.Show($"No .ydd files found for selected .meta file ({Path.GetFileName(path)})", "Error");
            return;
        }

        await AddDrawables(yddFiles, true);
    }

    protected void OnPropertyChanged([CallerMemberName] string name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public byte[] GenerateAvailComp()
    {
        byte[] genAvailComp = [255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255];
        byte compCount = 0;

        for (int i = 0; i < genAvailComp.Length; i++)
        {
            var compExist = Drawables.Where(x => x.TypeNumeric == i && x.IsProp == false).Any();
            if (compExist)
            {
                genAvailComp[i] = compCount;
                compCount++;
            }

        }

        return genAvailComp;
    }
}