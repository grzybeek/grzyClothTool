using grzyClothTool.Collections;
using grzyClothTool.Constants;
using grzyClothTool.Models.Drawable;
using grzyClothTool.Models.Other;
using grzyClothTool.Models.Texture;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace grzyClothTool.Models;

public class Addon : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    private string _name;
    public string Name
    {
        get { return _name; }
        set 
        {
            _name = value; 
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

            _selectedDrawable?.LoadTexturesThumbnail();
        }
    }

    private ObservableCollection<GDrawable> _selectedDrawables;
    [JsonIgnore]
    public ObservableCollection<GDrawable> SelectedDrawables
    {
        get => _selectedDrawables;
        set
        {
            if (_selectedDrawables == value) return;

            TriggerSelectedDrawableUpdatedEvent = false;

            if (_selectedDrawables != null)
                _selectedDrawables.CollectionChanged -= SelectedDrawables_CollectionChanged;

            _selectedDrawables = value;

            if (_selectedDrawables != null)
                _selectedDrawables.CollectionChanged += SelectedDrawables_CollectionChanged;

            OnPropertyChanged();
            TriggerSelectedDrawableUpdatedEvent = true;
        }
    }

    private void SelectedDrawables_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (GDrawable drawable in e.OldItems)
            {
                drawable.PropertyChanged -= SelectedDrawable_PropertyChanged;
            }
        }

        if (e.NewItems != null)
        {
            foreach (GDrawable drawable in e.NewItems)
            {
                drawable.PropertyChanged += SelectedDrawable_PropertyChanged;
            }
        }

        AllowOverrideDrawables = false; //reset to false
        OnPropertyChanged(nameof(IsMultipleDrawablesSelected));
        OnPropertyChanged(nameof(IsMultipleDrawablesSameType));
        OnPropertyChanged(nameof(IsMultipleDrawablesExactlyTheSame));
        OnPropertyChanged(nameof(CanEditMultipleDrawables));
        OnPropertyChanged(nameof(MultiSelectGroupDisplay));
        OnPropertyChanged(nameof(HasMultipleDifferentGroups));
        OnPropertyChanged(nameof(MultiSelectCommonTags));
    }

    private void SelectedDrawable_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GDrawable.Group))
        {
            OnPropertyChanged(nameof(MultiSelectGroupDisplay));
            OnPropertyChanged(nameof(HasMultipleDifferentGroups));
        }
        else if (e.PropertyName == nameof(GDrawable.Tags))
        {
            OnPropertyChanged(nameof(MultiSelectCommonTags));
        }
    }

    [JsonIgnore]
    public string MultiSelectGroupDisplay
    {
        get
        {
            if (!IsMultipleDrawablesSelected || SelectedDrawables.Count == 0)
                return SelectedDrawable?.Group;

            var groups = SelectedDrawables.Select(d => d.Group).Distinct().ToList();
            
            if (groups.Count == 1)
            {
                return groups[0];
            }
            else
            {
                return string.Empty;
            }
        }
    }

    [JsonIgnore]
    public bool HasMultipleDifferentGroups
    {
        get
        {
            if (!IsMultipleDrawablesSelected || SelectedDrawables.Count == 0)
                return false;

            var groups = SelectedDrawables.Select(d => d.Group).Distinct().ToList();
            return groups.Count > 1;
        }
    }

    [JsonIgnore]
    public ObservableCollection<string> MultiSelectCommonTags
    {
        get
        {
            if (!IsMultipleDrawablesSelected || SelectedDrawables.Count == 0)
                return SelectedDrawable?.Tags ?? [];

            var allUniqueTags = SelectedDrawables
                .SelectMany(d => d.Tags)
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            return new ObservableCollection<string>(allUniqueTags);
        }
    }

    [JsonIgnore]
    public bool IsMultipleDrawablesSelected
    {
        get { return SelectedDrawables.Count > 1; }
    }

    [JsonIgnore]
    public bool IsMultipleDrawablesSameType
    {
        get
        {
            if (IsMultipleDrawablesSelected)
            {
                var first = SelectedDrawables.First();
                var isSameType = SelectedDrawables.All(x =>
                    x.IsReserved == false && //allow only not-reserved drawables
                    x.IsProp == first.IsProp &&
                    x.TypeNumeric == first.TypeNumeric);

                return isSameType;
            }
            return true;
        }
    }

    [JsonIgnore]
    public bool IsMultipleDrawablesExactlyTheSame
    {
        get
        {
            if(IsMultipleDrawablesSelected && IsMultipleDrawablesSameType)
            {
                var first = SelectedDrawables.First();
                var isSameFields = SelectedDrawables.All(x =>
                    x.Audio == first.Audio &&
                    x.EnableHairScale == first.EnableHairScale &&
                    x.EnableHighHeels == first.EnableHighHeels &&
                    x.Flags == first.Flags &&
                    x.HasSkin == first.HasSkin &&
                    x.RenderFlag == first.RenderFlag);
                return isSameFields;
            }

            return false;
        }
    }

    private bool _allowOverrideDrawables;
    [JsonIgnore]
    public bool AllowOverrideDrawables
    {
        get { return _allowOverrideDrawables; }
        set
        {
            _allowOverrideDrawables = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanEditMultipleDrawables));
        }
    }

    [JsonIgnore]
    public bool CanEditMultipleDrawables
    {
        // allow to edit multiple drawables only if they are the same type and button clicked
        // or if they are exactly the same
        get
        {
            return (IsMultipleDrawablesSelected && IsMultipleDrawablesSameType && AllowOverrideDrawables)
                   || (IsMultipleDrawablesSelected && IsMultipleDrawablesExactlyTheSame);
        }
    }

    [JsonIgnore]
    public GDrawable FirstDrawable
    {
        get
        {
            return SelectedDrawables.FirstOrDefault() ?? SelectedDrawable;
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

    public Addon(string name)
    {
        Name = name;

        Drawables = [];
        SelectedDrawables = [];
        SelectedDrawables.CollectionChanged += (sender, e) =>
        {
            OnPropertyChanged(nameof(IsMultipleDrawablesSelected));
        };

        MainWindow.AddonManager.MoveMenuItems.Add(new MoveMenuItem() { Header = name, IsEnabled = true });

        if (name == "design")
        {
            for (int i = 1; i < 50; i++)
            {
                var sexEnum = i % 2 == 0 ? Enums.SexType.female : Enums.SexType.male;
                Drawables.Add(
                    new GDrawable(Guid.Empty, "grzyClothTool/Assets/jbib_000_u.ydd", sexEnum, false, 11, i, false,
                    [
                        new(Guid.Empty, "grzyClothTool/Assets/jbib_diff_000_a_uni.ytd", 11, 0, 0, false, false),
                        new(Guid.Empty, "grzyClothTool/Assets/jbib_diff_000_a_uni.ytd", 11, 0, 1, false, false)
                    ]
                ));
            }

            SelectedDrawable = Drawables.First();
        }
    }

    public int GetNextDrawableNumber(int typeNumeric, bool isProp, Enums.SexType sex)
    {
        var nextNumber = 0;
        var currentAddonIndex = 0;

        while (currentAddonIndex < MainWindow.AddonManager.Addons.Count)
        {
            var currentAddon = MainWindow.AddonManager.Addons[currentAddonIndex];
            var countOfType = currentAddon.Drawables.Count(x => x.TypeNumeric == typeNumeric && x.IsProp == isProp && x.Sex == sex);

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

    public bool CanFitDrawables(List<GDrawable> drawables)
    {
        var groupedDrawables = Drawables
            .GroupBy(d => new { d.TypeNumeric, d.Sex, d.IsProp })
            .ToDictionary(g => g.Key, g => g.Count());

        var newGroupedDrawables = drawables
            .GroupBy(d => new { d.TypeNumeric, d.Sex, d.IsProp })
            .ToDictionary(g => g.Key, g => g.Count());

        // Check if adding the new drawables would exceed the maximum count
        foreach (var newGroup in newGroupedDrawables)
        {
            var key = newGroup.Key;
            var newCount = newGroup.Value;

            if (groupedDrawables.ContainsKey(key))
            {
                groupedDrawables[key] += newCount;
            }
            else
            {
                groupedDrawables[key] = newCount;
            }

            if (groupedDrawables[key] > GlobalConstants.MAX_DRAWABLES_IN_ADDON)
            {
                return false;
            }
        }

        return true;
    }

    public bool HasSex(Enums.SexType sex)
    {
        return Drawables.Any(x => x.Sex == sex);
    }

    public bool HasProps()
    {
        return Drawables.Any(x => x.IsProp);
    }

    public int GetTotalDrawableAndTextureCount()
    {
        return Drawables.Count + Drawables.SelectMany(drawable => drawable.Textures).Count();
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