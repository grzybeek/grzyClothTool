using grzyClothTool.Models.Drawable;
using grzyClothTool.Models.Texture;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace grzyClothTool.Models;

public class Addon : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    public string Name { get; set; }

    public bool HasFemale { get; set; }
    public bool HasMale { get; set; }
    public bool HasProps { get; set; }

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
        InitAfterLoading();

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

    [OnDeserialized]
    internal void OnDeserialized(StreamingContext context)
    {
        InitAfterLoading();
    }

    private void InitAfterLoading()
    {
        Drawables.CollectionChanged += async (s, e) =>
        {
            if (e.Action == NotifyCollectionChangedAction.Remove && e.OldItems?.Count > 0)
            {
                foreach (var oldItem in e.OldItems)
                {
                    if (oldItem is GDrawable drawable)
                    {
                        await drawable.RemoveDuplicate();
                    }
                }
            }
            if (e.Action == NotifyCollectionChangedAction.Replace && e.NewItems?.Count > 0 && e.NewItems?.Count == e.OldItems?.Count)
            {
                for (int i = 0; i < e.NewItems.Count; ++i)
                {
                    if (e.NewItems[i] is GDrawableReserved newItem && e.OldItems[i] is GDrawable oldItem && oldItem is not GDrawableReserved)
                    {
                        await oldItem.RemoveDuplicate();
                    }
                }
            }
        };
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