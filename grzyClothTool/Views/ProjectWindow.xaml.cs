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
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using System.Windows.Input;
using grzyClothTool.Models.Drawable;
using grzyClothTool.Models.Texture;
using System.Threading.Tasks;

namespace grzyClothTool.Views
{
    /// <summary>
    /// Interaction logic for Project.xaml
    /// </summary>
    public partial class ProjectWindow : UserControl, INotifyPropertyChanged
    {
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

            DataContext = MainWindow.AddonManager;
        }

        private async void Add_DrawableFile(object sender, RoutedEventArgs e)
        {
            var btn = sender as CustomButton;
            var sexBtn = btn.Label.ToString().Equals("male", StringComparison.CurrentCultureIgnoreCase) ? Enums.SexType.male : Enums.SexType.female;
            e.Handled = true;

            OpenFileDialog files = new()
            {
                Title = $"Select drawable files ({btn.Label})",
                Filter = "Drawable files (*.ydd)|*.ydd",
                Multiselect = true
            };

            if (files.ShowDialog() == true)
            {
                ProgressHelper.Start();

                await MainWindow.AddonManager.AddDrawables(files.FileNames, sexBtn);

                ProgressHelper.Stop("Added drawables in {0}", true);
                SaveHelper.SetUnsavedChanges(true);
            }
        }

        private async void Add_DrawableFolder(object sender, RoutedEventArgs e)
        {
            var btn = sender as CustomButton;
            var sexBtn = btn.Tag.ToString().Equals("male", StringComparison.CurrentCultureIgnoreCase) ? Enums.SexType.male : Enums.SexType.female;
            e.Handled = true;

            OpenFolderDialog folder = new()
            {
                Title = $"Select a folder containing drawable files ({btn.Tag})",
                Multiselect = true
            };

            if (folder.ShowDialog() == true)
            {
                ProgressHelper.Start();

                foreach (var fldr in folder.FolderNames)
                {
                    var files = Directory.GetFiles(fldr, "*.ydd", SearchOption.AllDirectories).OrderBy(f => Path.GetFileName(f)).ToArray();
                    await MainWindow.AddonManager.AddDrawables(files, sexBtn);
                }

                ProgressHelper.Stop("Added drawables in {0}", true);
                SaveHelper.SetUnsavedChanges(true);
            }
        }

        public void SelectedDrawable_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Delete || Addon.SelectedDrawables.Count == 0)
            {
                return;
            }

            switch (Keyboard.Modifiers)
            {
                case ModifierKeys.Shift:
                    // Shift+Delete was pressed, delete the drawable instantly
                    MainWindow.AddonManager.DeleteDrawables([.. Addon.SelectedDrawables]);
                    break;
                case ModifierKeys.Control:
                    // Ctrl+Delete was pressed, replace the drawable instantly
                    ReplaceDrawables([.. Addon.SelectedDrawables]);
                    break;
                default:
                    // Only Delete was pressed, show the message box
                    Delete_SelectedDrawable(sender, new RoutedEventArgs());
                    break;
            }
        }

        private void Delete_SelectedDrawable(object sender, RoutedEventArgs e)
        {
            var count = Addon.SelectedDrawables.Count;

            if (count == 0)
            {
                CustomMessageBox.Show("No drawable(s) selected", "Delete drawable", CustomMessageBox.CustomMessageBoxButtons.OKOnly);
                return;
            }

            var message = count == 1
                ? $"Are you sure you want to delete this drawable? ({Addon.SelectedDrawable.Name})"
                : $"Are you sure you want to delete these {count} selected drawables?";

            message += "\nThis will CHANGE NUMBERS of everything after this drawable!\n\nDo you want to replace with reserved slot instead?";

            var result = CustomMessageBox.Show(message, "Delete drawable", CustomMessageBox.CustomMessageBoxButtons.DeleteReplaceCancel);
            if (result == CustomMessageBox.CustomMessageBoxResult.Delete)
            {
                MainWindow.AddonManager.DeleteDrawables([.. Addon.SelectedDrawables]);
            }
            else if (result == CustomMessageBox.CustomMessageBoxResult.Replace)
            {
                ReplaceDrawables([.. Addon.SelectedDrawables]);
            }
        }

        private void ReplaceDrawables(List<GDrawable> drawables)
        {
            foreach(var drawable in drawables)
            {
                var reserved = new GDrawableReserved(drawable.Sex, drawable.IsProp, drawable.TypeNumeric, drawable.Number);

                //replace drawable with reserved in the same place
                Addon.Drawables[Addon.Drawables.IndexOf(drawable)] = reserved;
            }
            SaveHelper.SetUnsavedChanges(true);
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var addon = e.AddedItems[0] as Addon;
                int index = int.Parse(addon.Name.ToString().Split(' ')[1]) - 1;

                // as we are modyfing the collection, we need to use try-catch
                try
                {
                    Addon = MainWindow.AddonManager.Addons.ElementAt(index);
                    MainWindow.AddonManager.SelectedAddon = Addon;

                    foreach (var menuItem in MainWindow.AddonManager.MoveMenuItems)
                    {
                        menuItem.IsEnabled = menuItem.Header != addon.Name;
                    }
                } catch (Exception)  { }
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

            var ydd = CWHelper.CreateYddFile(Addon.SelectedDrawable);
            CWHelper.CWForm.LoadedDrawables.Add(Addon.SelectedDrawable.Name, ydd.Drawables.First());

            if (Addon.SelectedTexture != null)
            {
                var ytd = CWHelper.CreateYtdFile(Addon.SelectedTexture, Addon.SelectedTexture.DisplayName);
                CWHelper.CWForm.LoadedTextures.Add(ydd.Drawables.First(), ytd.TextureDict);
            }

            CWHelper.SetPedModel(Addon.SelectedDrawable.Sex);

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

            foreach (GDrawable drawable in args.RemovedItems)
            {
                Addon.SelectedDrawables.Remove(drawable);
            }

            foreach (GDrawable drawable in args.AddedItems)
            {
                Addon.SelectedDrawables.Add(drawable);
                drawable.IsNew = false;
            }

            // Handle the case when a single item is selected
            if (Addon.SelectedDrawables.Count == 1)
            {
                Addon.SelectedDrawable = Addon.SelectedDrawables.First();
                if (Addon.SelectedDrawable.Textures.Count > 0)
                {
                    Addon.SelectedTexture = Addon.SelectedDrawable.Textures.First();
                    SelDrawable.SelectedIndex = 0;
                    SelDrawable.SelectedTextures = [Addon.SelectedTexture];
                }
            }
            else
            {
                Addon.SelectedDrawable = null;
                Addon.SelectedTexture = null;
            }

            if (!MainWindow.AddonManager.IsPreviewEnabled || (Addon.SelectedDrawable == null && Addon.SelectedDrawables.Count == 0)) return;
            CWHelper.SendDrawableUpdateToPreview(e);
            CWHelper.CWForm.Refresh();
        }

        private void SelectedDrawable_Updated(object sender, DrawableUpdatedArgs e)
        {
            if (!Addon.TriggerSelectedDrawableUpdatedEvent ||
                !MainWindow.AddonManager.IsPreviewEnabled ||
                (Addon.SelectedDrawable is null && Addon.SelectedDrawables.Count == 0) ||
                Addon.SelectedDrawables.All(d => d.Textures.Count == 0))
            {
                return;
            }

            CWHelper.SendDrawableUpdateToPreview(e);
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

            var ytd = CWHelper.CreateYtdFile(Addon.SelectedTexture, Addon.SelectedTexture.DisplayName);
            var cwydd = CWHelper.CWForm.LoadedDrawables[Addon.SelectedDrawable.Name];
            CWHelper.CWForm.LoadedTextures[cwydd] = ytd.TextureDict;
            CWHelper.CWForm.Refresh();
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
