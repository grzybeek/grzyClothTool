using CodeWalker;
using CodeWalker.GameFiles;
using grzyClothTool.Controls;
using grzyClothTool.Extensions;
using grzyClothTool.Helpers;
using grzyClothTool.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Path = System.IO.Path;
using UserControl = System.Windows.Controls.UserControl;

namespace grzyClothTool.Views
{
    /// <summary>
    /// Interaction logic for Project.xaml
    /// </summary>
    public partial class ProjectWindow : UserControl, INotifyPropertyChanged
    {
        private bool PrevDrawableSex;
        public event PropertyChangedEventHandler PropertyChanged;

        private Addon _addon;
        public Addon Addon
        {
            get { return _addon; }
            set
            {
                if (_addon != value)
                {
                    _addon = value;
                    OnPropertyChanged();
                }
            }
        }

        public ProjectWindow()
        {
            InitializeComponent();

            if(DesignerProperties.GetIsInDesignMode(this))
            {
                Addon = new Addon("design");
                DataContext = this;
                return;
            }

            Addon = MainWindow.AddonManager.Addons.First();
            DataContext = MainWindow.AddonManager;
        }

        private async void Add_DrawableFile(object sender, RoutedEventArgs e)
        {
            var btn = sender as CustomButton;
            var isMaleBtn = btn.Label.ToString().Equals("male", StringComparison.CurrentCultureIgnoreCase);
            e.Handled = true;

            OpenFileDialog files = new()
            {
                Title = $"Select drawable files ({btn.Label})",
                Filter = "Drawable files (*.ydd)|*.ydd",
                Multiselect = true
            };

            if (files.ShowDialog() == true)
            {
                var timer = new Stopwatch();
                timer.Start();

                await Addon.AddDrawables(files.FileNames, isMaleBtn);

                timer.Stop();
                LogHelper.Log($"Added drawables in {timer.Elapsed}");
            }
        }

        private async void Add_DrawableFolder(object sender, RoutedEventArgs e)
        {
            var btn = sender as CustomButton;
            var isMaleBtn = btn.Tag.ToString().Equals("male", StringComparison.CurrentCultureIgnoreCase);
            e.Handled = true;

            OpenFolderDialog folder = new()
            {
                Title = $"Select a folder containing drawable files ({btn.Tag})",
                Multiselect = true
            };

            if (folder.ShowDialog() == true)
            {
                var timer = new Stopwatch();
                timer.Start();
                foreach (var fldr in folder.FolderNames)
                {
                    var files = Directory.GetFiles(fldr, "*.ydd", SearchOption.AllDirectories).OrderBy(f => Path.GetFileName(f)).ToArray();
                    await Addon.AddDrawables(files, isMaleBtn);
                }

                timer.Stop();
                LogHelper.Log($"Added drawables in {timer.Elapsed}");
            }
        }

        private void Delete_SelectedDrawable(object sender, RoutedEventArgs e)
        {
            var drawable = Addon.SelectedDrawable;
            if (drawable == null)
            {
                CustomMessageBox.Show("No drawable selected", "Delete drawable", CustomMessageBox.CustomMessageBoxButtons.OKOnly);
                return;
            }

            var result = CustomMessageBox.Show($"Are you sure you want to delete this drawable? ({drawable.Name})\nThis will CHANGE NUMBERS of everything after this drawable!\n\nDo you want to replace with reserved slot instead?", "Delete drawable", CustomMessageBox.CustomMessageBoxButtons.DeleteReplaceCancel);
            if(result == CustomMessageBox.CustomMessageBoxResult.Delete)
            {
                Addon.Drawables.Remove(drawable);
                Addon.Drawables.Sort(true);
            }
            else if(result == CustomMessageBox.CustomMessageBoxResult.Replace)
            {
                var reserved = new GReservedDrawable(drawable.Sex, drawable.IsProp, drawable.TypeNumeric, drawable.Number);

                //replace drawable with reserved in the same place
                Addon.Drawables[Addon.Drawables.IndexOf(drawable)] = reserved;
            }
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var addon = e.AddedItems[0] as Addon;
                int index = int.Parse(addon.Name.ToString().Split(' ')[1]) - 1;
                Addon = MainWindow.AddonManager.Addons.ElementAt(index);

                MainWindow.AddonManager.SelectedAddon = Addon;
            }
        }

        private void BuildResource_Btn(object sender, RoutedEventArgs e)
        {
            BuildWindow buildWindow = new()
            {
                Owner = Window.GetWindow(this)
            };
            buildWindow.ShowDialog();
        }

        private void Preview_Btn(object sender, RoutedEventArgs e)
        {
            if (CWHelper.CWForm == null || CWHelper.CWForm.IsDisposed)
            {
                CWHelper.CWForm = new CustomPedsForm();
                CWHelper.CWForm.FormClosed += CWForm_FormClosed;
            }

            if (Addon.SelectedDrawable == null)
            {
                CWHelper.CWForm.Show();
                MainWindow.AddonManager.IsPreviewEnabled = true;
                return;
            }

            var ydd = CreateYddFile(Addon.SelectedDrawable);
            CWHelper.CWForm.LoadedDrawable = ydd.Drawables.First();

            if (Addon.SelectedTexture != null)
            {
                var ytd = CWHelper.CreateYtdFile(Addon.SelectedTexture.FilePath, Addon.SelectedTexture.DisplayName);
                CWHelper.CWForm.LoadedTexture = ytd.TextureDict;
            }

            CWHelper.CWForm.Show();
            MainWindow.AddonManager.IsPreviewEnabled = true;
        }

        private void CWForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            MainWindow.AddonManager.IsPreviewEnabled = false;
        }

        private void SelectedDrawable_Changed(object sender, EventArgs e)
        {
            if (e is not SelectionChangedEventArgs args) return;
            args.Handled = true;

            if (args.AddedItems.Count == 0)
            {
                Addon.SelectedDrawable = null;
                return;
            }

            Addon.SelectedDrawable = (GDrawable)args.AddedItems[0];
            if (Addon.SelectedDrawable.Textures.Count > 0)
            {
                Addon.SelectedTexture = Addon.SelectedDrawable.Textures.First();
                SelDrawable.SelectedIndex = 0;
            }

            if (!MainWindow.AddonManager.IsPreviewEnabled) return;
            SendDrawableUpdateToCodewalkerPreview(e);
        }

        private void SendDrawableUpdateToCodewalkerPreview(EventArgs args)
        {
            var ydd = CreateYddFile(Addon.SelectedDrawable);
            YtdFile ytd = null;
            if (Addon.SelectedTexture != null)
            {
                ytd = CWHelper.CreateYtdFile(Addon.SelectedTexture.FilePath, Addon.SelectedTexture.DisplayName);
                CWHelper.CWForm.LoadedTexture = ytd.TextureDict;
            }

            var firstDrawable = ydd.Drawables.First();
            CWHelper.CWForm.LoadedDrawable = firstDrawable;
            CWHelper.CWForm.Refresh();

            Dictionary<string, string> updateDict = [];
            string updateName, value;

            if (args is DrawableUpdatedArgs dargs)
            {
                updateName = dargs.UpdatedName;
                value = dargs.Value.ToString();
                updateDict.Add(updateName, value);
            }

            if (PrevDrawableSex != Addon.SelectedDrawable.Sex)
            {
                PrevDrawableSex = Addon.SelectedDrawable.Sex;

                updateName = "GenderChanged";
                value = Addon.SelectedDrawable.Sex ? "mp_m_freemode_01" : "mp_f_freemode_01";
                updateDict.Add(updateName, value);
            }

            CWHelper.CWForm.UpdateSelectedDrawable(firstDrawable, ytd?.TextureDict, updateDict);
        }

        private void SelectedDrawable_Updated(object sender, DrawableUpdatedArgs e)
        {
            if (!Addon.TriggerSelectedDrawableUpdatedEvent || !MainWindow.AddonManager.IsPreviewEnabled || Addon.SelectedDrawable is null || Addon.SelectedDrawable.Textures.Count == 0)
            {
                return;
            }

            Addon.SelectedTexture = Addon.SelectedDrawable.Textures.First();
            SelDrawable.SelectedIndex = 0;

            SendDrawableUpdateToCodewalkerPreview(e);
        }

        private void SelectedDrawable_TextureChanged(object sender, EventArgs e)
        {
            if (e is not SelectionChangedEventArgs args || args.AddedItems.Count == 0)
            {
                Addon.SelectedTexture = null;
                return;
            }

            args.Handled = true;
            Addon.SelectedTexture = (GTexture)args.AddedItems[0];

            if (!MainWindow.AddonManager.IsPreviewEnabled) return;

            var ytd = CWHelper.CreateYtdFile(Addon.SelectedTexture.FilePath, Addon.SelectedTexture.DisplayName);
            CWHelper.CWForm.LoadedTexture = ytd.TextureDict;
            CWHelper.CWForm.Refresh();
        }

        private static YddFile CreateYddFile(GDrawable d)
        {
            byte[] data = File.ReadAllBytes(d.FilePath);

            RpfFileEntry rpf = RpfFile.CreateResourceFileEntry(ref data, 0);
            var decompressedData = ResourceBuilder.Decompress(data);
            YddFile ydd = RpfFile.GetFile<YddFile>(rpf, decompressedData);
            var drawable = ydd.Drawables.First();
            drawable.Name = Path.GetFileNameWithoutExtension(d.Name);
            drawable.IsHairScaleEnabled = d.EnableHairScale;
            drawable.IsHighHeelsEnabled = d.EnableHighHeels;


            return ydd;
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
