using grzyClothTool.Controls;
using grzyClothTool.Extensions;
using grzyClothTool.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace grzyClothTool.Models
{

    public delegate void DrawableChangedHandler(GDrawable drawable);

    public class AddonManager : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public string Name;

        public bool HasFemale { get; set; }
        public bool HasMale { get; set; }
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


        public bool TriggerSelectedDrawableUpdatedEvent { get; set; }

        private GDrawable _selectedDrawable;
        public GDrawable SelectedDrawable 
        { 
            get { return _selectedDrawable; } 
            set {
                TriggerSelectedDrawableUpdatedEvent = false;
                _selectedDrawable = value;
                OnPropertyChanged();
                TriggerSelectedDrawableUpdatedEvent = true;
            } 
        }

        private GTexture _selectedTexture;
        public GTexture SelectedTexture
        {
            get { return _selectedTexture; }
            set
            {
                _selectedTexture = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<GDrawable> Drawables { get; set; }

        private readonly FileHelper _fileHelper;

        public AddonManager(string projectName)
        {
            Name = projectName;

            Drawables = [];

            if(projectName == "design")
            {
                for (int i = 1; i < 50; i++)
                {
                    Drawables.Add(new GDrawable(new FileInfo("grzyClothTool/Assets/jbib_000_u.ydd"), i % 2 == 0, false, 11, i, false, [new ("grzyClothTool/Assets/jbib_diff_000_a_uni.ytd", 11, 0, 0, false, false)]));
                    SelectedDrawable = Drawables.First();
                }
            }


            _fileHelper = new FileHelper(Name);
        }

        public async Task AddDrawables(string[] filePaths, bool isMale)
        {
            foreach(var filePath in filePaths)
            {
                var isProp = false;
                var (isValidComp, compType) = FileHelper.IsValidComponent(filePath);
                if (!isValidComp)
                {
                    if (compType == -1) return; //not component not prop
                    isProp = true;
                }

                var countOfType = Drawables.Count(x => x.TypeNumeric == compType && x.IsProp == isProp && x.Sex == isMale);
                var drawable = await Task.Run(() => _fileHelper.CreateDrawableAsync(filePath, isMale, isProp, compType, countOfType));
                Drawables.Add(drawable);
            }

            // Sort the ObservableCollection in place
            Drawables.Sort();

            //set HasMale/HasFemale only once adding first drawable of this gender
            if (isMale && !HasMale) HasMale = true;
            if (!isMale && !HasFemale) HasFemale = true;
        }

        public async void LoadAddon(string path)
        {
            var dirPath = Path.GetDirectoryName(path);
            var addonName = Path.GetFileNameWithoutExtension(path);

            // find all .ydd files within all folders that contain addonName in name
            var yddFiles = Directory.GetFiles(dirPath, "*.ydd", SearchOption.AllDirectories)
                .Where(x => x.Contains(addonName))
                .ToArray();

            if(yddFiles.Length == 0)
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
}
