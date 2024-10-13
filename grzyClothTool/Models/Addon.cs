﻿using grzyClothTool.Models.Drawable;
using grzyClothTool.Models.Other;
using grzyClothTool.Models.Texture;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

    private ObservableCollection<GDrawable> _selectedDrawables;
    [JsonIgnore]
    public ObservableCollection<GDrawable> SelectedDrawables
    {
        get { return _selectedDrawables; }
        set
        {
            TriggerSelectedDrawableUpdatedEvent = false;
            _selectedDrawables = value;
            OnPropertyChanged();
            TriggerSelectedDrawableUpdatedEvent = true;
        }
    }

    public bool IsMultipleDrawablesSelected
    {
        get { return SelectedDrawables.Count > 1; }
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
        SelectedDrawables = [];
        SelectedDrawables.CollectionChanged += (sender, e) =>
        {
            OnPropertyChanged(nameof(IsMultipleDrawablesSelected));
        };

        MainWindow.AddonManager.MoveMenuItems.Add(new MoveMenuItem() { Header = projectName, IsEnabled = true });

        if (projectName == "design")
        {
            for (int i = 1; i < 50; i++)
            {
                var sexEnum = i % 2 == 0 ? Enums.SexType.female : Enums.SexType.male;
                Drawables.Add(
                    new GDrawable("grzyClothTool/Assets/jbib_000_u.ydd", sexEnum, false, 11, i, false,
                    [
                        new("grzyClothTool/Assets/jbib_diff_000_a_uni.ytd", 11, 0, 0, false, false), 
                        new("grzyClothTool/Assets/jbib_diff_000_a_uni.ytd", 11, 0, 1, false, false)
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