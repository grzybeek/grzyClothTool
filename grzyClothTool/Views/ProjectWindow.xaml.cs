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
using WpfDataFormats = System.Windows.DataFormats;
using WpfDragDropEffects = System.Windows.DragDropEffects;

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
            
            Loaded += ProjectWindow_Loaded;
            Unloaded += ProjectWindow_Unloaded;
            
            PreviewWindowHost.Preview3DAvailabilityChanged += OnPreview3DAvailabilityChanged;
        }

        private void ProjectWindow_Loaded(object sender, RoutedEventArgs e)
        {
            UpdatePreviewButtonState();
        }

        private void ProjectWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            PreviewWindowHost.Preview3DAvailabilityChanged -= OnPreview3DAvailabilityChanged;
        }

        private void OnPreview3DAvailabilityChanged(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() => UpdatePreviewButtonState());
        }

        private void UpdatePreviewButtonState()
        {
            if (PreviewButton != null)
            {
                PreviewButton.IsEnabled = SettingsHelper.Preview3DAvailable;
            }
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
                LogHelper.Log($"Scanning files to add...", LogType.Info);

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

                try
                {
                    LogHelper.Log($"Scanning files to add...", LogType.Info);

                    var allFiles = await Task.Run(() =>
                    {
                        var fileList = new List<string>();
                        foreach (var fldr in folder.FolderNames)
                        {
                            var files = Directory.GetFiles(fldr, "*.ydd", SearchOption.AllDirectories);
                            fileList.AddRange(files);
                        }
                        
                        return fileList
                            .OrderBy(f =>
                            {
                                var number = FileHelper.GetDrawableNumberFromFileName(Path.GetFileName(f));
                                return number ?? int.MaxValue;
                            })
                            .ThenBy(Path.GetFileName)
                            .ToArray();
                    });

                    if (allFiles.Length == 0)
                    {
                        ProgressHelper.Stop("No drawable files found", false);
                        return;
                    }

                    LogHelper.Log($"Adding {allFiles.Length} drawable files from {folder.FolderNames.Length} folder(s)...", LogType.Info);

                    await MainWindow.AddonManager.AddDrawables(allFiles, sexBtn);

                    ProgressHelper.Stop($"Added {allFiles.Length} drawables in {{0}}", true);
                    SaveHelper.SetUnsavedChanges(true);
                }
                catch (Exception ex)
                {
                    LogHelper.Log($"Error adding drawables: {ex.Message}", LogType.Error);
                    ProgressHelper.Stop("Failed to add drawables", false);
                }
            }
        }

        private async void Add_DrawableAutoFile(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            await SelectDrawableFilesAsync(null);
        }

        private async void Add_DrawableFemaleFile(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            await SelectDrawableFilesAsync(Enums.SexType.female);
        }

        private async void Add_DrawableMaleFile(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            await SelectDrawableFilesAsync(Enums.SexType.male);
        }

        private async Task SelectDrawableFilesAsync(Enums.SexType? forcedGender)
        {
            OpenFileDialog files = new()
            {
                Title = forcedGender.HasValue
                    ? $"Select drawable files ({GetGenderDisplayName(forcedGender.Value)})"
                    : "Select drawable files",
                Filter = "Drawable files (*.ydd)|*.ydd",
                Multiselect = true
            };

            if (files.ShowDialog() != true)
            {
                return;
            }

            await AddDrawablesByDetectedGenderAsync(files.FileNames, forcedGender);
        }

        private async void Add_DrawableAutoFolder(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            await SelectDrawableFoldersAsync(null);
        }

        private async void Add_DrawableFemaleFolder(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            await SelectDrawableFoldersAsync(Enums.SexType.female);
        }

        private async void Add_DrawableMaleFolder(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            await SelectDrawableFoldersAsync(Enums.SexType.male);
        }

        private async Task SelectDrawableFoldersAsync(Enums.SexType? forcedGender)
        {
            OpenFolderDialog folder = new()
            {
                Title = forcedGender.HasValue
                    ? $"Select folder(s) containing drawable files ({GetGenderDisplayName(forcedGender.Value)})"
                    : "Select folder(s) containing drawable files",
                Multiselect = true
            };

            if (folder.ShowDialog() != true)
            {
                return;
            }

            ProgressHelper.Start();

            try
            {
                LogHelper.Log("Scanning files to add...", LogType.Info);

                var allFiles = await Task.Run(() =>
                {
                    var fileList = new List<string>();
                    foreach (var fldr in folder.FolderNames)
                    {
                        var files = Directory.GetFiles(fldr, "*.ydd", SearchOption.AllDirectories);
                        fileList.AddRange(files);
                    }

                    return fileList
                        .OrderBy(f =>
                        {
                            var number = FileHelper.GetDrawableNumberFromFileName(Path.GetFileName(f));
                            return number ?? int.MaxValue;
                        })
                        .ThenBy(Path.GetFileName)
                        .ToArray();
                });

                if (allFiles.Length == 0)
                {
                    ProgressHelper.Stop("No drawable files found", false);
                    return;
                }

                ProgressHelper.Stop($"Found {allFiles.Length} drawable files in {{0}}", true);

                await AddDrawablesByDetectedGenderAsync(allFiles, forcedGender);
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Error adding drawables: {ex.Message}", LogType.Error);
                ProgressHelper.Stop("Failed to add drawables", false);
            }
        }

        private static async Task AddDrawablesByDetectedGenderAsync(IEnumerable<string> filePaths, Enums.SexType? forcedGender = null)
        {
            var files = filePaths.Distinct().ToList();
            if (files.Count == 0)
            {
                return;
            }

            var resolution = ResolveDrawableImport(files, forcedGender);
            if (resolution == null)
            {
                LogHelper.Log("Adding drawables cancelled while resolving import settings.", LogType.Info);
                return;
            }

            var maleFiles = resolution.Genders
                .Where(x => x.Value == Enums.SexType.male)
                .Select(x => x.Key)
                .ToArray();
            var femaleFiles = resolution.Genders
                .Where(x => x.Value == Enums.SexType.female)
                .Select(x => x.Key)
                .ToArray();

            ProgressHelper.Start();
            LogHelper.Log($"Adding {files.Count} drawable file(s): {maleFiles.Length} male, {femaleFiles.Length} female.", LogType.Info);

            if (maleFiles.Length > 0)
            {
                var maleTypes = resolution.DrawableTypes
                    .Where(x => maleFiles.Contains(x.Key))
                    .ToDictionary(x => x.Key, x => x.Value);
                await MainWindow.AddonManager.AddDrawables(maleFiles, Enums.SexType.male, resolvedDrawableTypes: maleTypes);
            }

            if (femaleFiles.Length > 0)
            {
                var femaleTypes = resolution.DrawableTypes
                    .Where(x => femaleFiles.Contains(x.Key))
                    .ToDictionary(x => x.Key, x => x.Value);
                await MainWindow.AddonManager.AddDrawables(femaleFiles, Enums.SexType.female, resolvedDrawableTypes: femaleTypes);
            }

            ProgressHelper.Stop("Added drawables in {0}", true);
            SaveHelper.SetUnsavedChanges(true);
        }

        private sealed class DrawableImportResolution
        {
            public Dictionary<string, Enums.SexType> Genders { get; init; }
            public Dictionary<string, (bool IsProp, int DrawableType)> DrawableTypes { get; init; }
        }

        private static string GetGenderDisplayName(Enums.SexType gender)
        {
            return gender == Enums.SexType.male ? "Male" : "Female";
        }

        private static DrawableImportResolution ResolveDrawableImport(IEnumerable<string> files, Enums.SexType? forcedGender)
        {
            var fileList = files.ToList();
            var detectedGenders = fileList.ToDictionary(
                file => file,
                file => forcedGender ?? DetermineGenderFromFilename(file));
            var detectedDrawableTypes = fileList.ToDictionary(
                file => file,
                file => FileHelper.TryResolveDrawableTypeFromFileName(file));

            var needsGender = !forcedGender.HasValue && detectedGenders.Values.Any(x => !x.HasValue);
            var needsDrawableProperties = detectedDrawableTypes.Values.Any(x => !x.HasValue);

            if (!needsGender && !needsDrawableProperties)
            {
                return new DrawableImportResolution
                {
                    Genders = detectedGenders.ToDictionary(x => x.Key, x => x.Value.GetValueOrDefault()),
                    DrawableTypes = detectedDrawableTypes.ToDictionary(x => x.Key, x => x.Value.GetValueOrDefault())
                };
            }

            var window = new DrawableImportResolveWindow(
                fileList,
                detectedGenders,
                detectedDrawableTypes,
                needsGender,
                needsDrawableProperties)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            if (window.ShowDialog() != true)
            {
                return null;
            }

            return new DrawableImportResolution
            {
                Genders = needsGender
                    ? window.SelectedGenders
                    : detectedGenders.ToDictionary(x => x.Key, x => x.Value.GetValueOrDefault()),
                DrawableTypes = needsDrawableProperties
                    ? window.SelectedDrawableTypes
                    : detectedDrawableTypes.ToDictionary(x => x.Key, x => x.Value.GetValueOrDefault())
            };
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
            if (string.IsNullOrEmpty(MainWindow.AddonManager.ProjectName))
            {
                CustomMessageBox.Show("No project is currently loaded. Please create or open a project first.", 
                    "No Project", 
                    CustomMessageBox.CustomMessageBoxButtons.OKOnly, 
                    CustomMessageBox.CustomMessageBoxIcon.Warning);
                return;
            }

            BuildWindow buildWindow = new()
            {
                Owner = Window.GetWindow(this)
            };
            buildWindow.ShowDialog();
        }

        private void Preview_Btn(object sender, RoutedEventArgs e)
        {
            var mainWindow = MainWindow.Instance;
            if (mainWindow == null) return;

            if (mainWindow.PreviewAnchorable != null)
            {
                mainWindow.PreviewAnchorable.Show();
                mainWindow.PreviewHost?.InitializePreview();

                if (Addon.SelectedDrawable != null && !Addon.SelectedDrawable.IsEncrypted)
                {
                    CWHelper.SendDrawableUpdateToPreview(e);
                }

                MainWindow.AddonManager.IsPreviewEnabled = true;
                
                UpdatePreviewButtonState();
            }
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
            
            var mainWindow = MainWindow.Instance;
            if (mainWindow?.PreviewAnchorable?.IsVisible != true) return;
            
            CWHelper.SendDrawableUpdateToPreview(e);
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

            var mainWindow = MainWindow.Instance;
            if (mainWindow?.PreviewAnchorable?.IsVisible != true) return;

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

            var mainWindow = MainWindow.Instance;
            if (mainWindow?.PreviewAnchorable?.IsVisible != true) return;

            CWHelper.SendDrawableUpdateToPreview(e);
        }

        #region Drag and Drop for Drawables

        private void DrawablesGroupBox_DragEnter(object sender, System.Windows.DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(WpfDataFormats.FileDrop))
                {
                    var files = (string[])e.Data.GetData(WpfDataFormats.FileDrop);
                    var yddFiles = files.Where(f => Path.GetExtension(f).Equals(".ydd", StringComparison.OrdinalIgnoreCase)).ToArray();
                    
                    e.Effects = yddFiles.Length > 0 ? WpfDragDropEffects.Copy : WpfDragDropEffects.None;
                }
                else if (e.Data.GetDataPresent("FileGroupDescriptor") || e.Data.GetDataPresent("FileGroupDescriptorW"))
                {
                    var filter = DragDropHelper.CreateExtensionFilter(".ydd");
                    var hasYddFiles = DragDropHelper.CheckForFilesInDescriptor(e.Data, filter);
                    e.Effects = hasYddFiles ? WpfDragDropEffects.Copy : WpfDragDropEffects.None;
                }
                else
                {
                    e.Effects = WpfDragDropEffects.None;
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Error in DragEnter: {ex.Message}", LogType.Error);
                e.Effects = WpfDragDropEffects.None;
            }
            
            e.Handled = true;
        }

        private void DrawablesGroupBox_DragOver(object sender, System.Windows.DragEventArgs e)
        {
            try
            {
                if (e.Data.GetDataPresent(WpfDataFormats.FileDrop))
                {
                    var files = (string[])e.Data.GetData(WpfDataFormats.FileDrop);
                    e.Effects = files.Any(f => Path.GetExtension(f).Equals(".ydd", StringComparison.OrdinalIgnoreCase)) 
                        ? WpfDragDropEffects.Copy 
                        : WpfDragDropEffects.None;
                }
                else if (e.Data.GetDataPresent("FileGroupDescriptor") || e.Data.GetDataPresent("FileGroupDescriptorW"))
                {
                    e.Effects = WpfDragDropEffects.Copy;
                }
                else
                {
                    e.Effects = WpfDragDropEffects.None;
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Error in DragOver: {ex.Message}", LogType.Error);
                e.Effects = WpfDragDropEffects.None;
            }
            
            e.Handled = true;
        }

        private void DrawablesGroupBox_DragLeave(object sender, System.Windows.DragEventArgs e)
        {
            e.Handled = true;
        }

        private async void DrawablesGroupBox_Drop(object sender, System.Windows.DragEventArgs e)
        {
            try
            {
                List<string> filesToProcess = [];
                
                if (e.Data.GetDataPresent(WpfDataFormats.FileDrop))
                {
                    var files = (string[])e.Data.GetData(WpfDataFormats.FileDrop);
                    filesToProcess.AddRange(files);
                }
                else if (e.Data.GetDataPresent("FileGroupDescriptor") || e.Data.GetDataPresent("FileGroupDescriptorW"))
                {
                    var filter = DragDropHelper.CreateExtensionFilter(".ydd");
                    var extractedFiles = await DragDropHelper.ExtractVirtualFilesAsync(e.Data, filter);
                    if (extractedFiles.Count > 0)
                    {
                        filesToProcess.AddRange(extractedFiles);
                    }
                    else
                    {
                        LogHelper.Log($"Could not extract files", LogType.Error);
                        e.Handled = true;
                        return;
                    }
                }
                else
                {
                    e.Handled = true;
                    return;
                }

                var yddFiles = filesToProcess.Where(f => Path.GetExtension(f).Equals(".ydd", StringComparison.OrdinalIgnoreCase)).ToArray();
                if (yddFiles.Length == 0)
                {
                    e.Handled = true;
                    return;
                }

                var (accessibleFiles, inaccessibleFiles) = DragDropHelper.ValidateFileAccess(yddFiles);

                if (inaccessibleFiles.Count > 0)
                {
                    var message = $"The following file(s) could not be accessed:\n\n" +
                                  string.Join("\n", inaccessibleFiles.Select(Path.GetFileName)) +
                                  "\n\nThey may be virtual paths. Please extract them to a folder first and drag from there.";
                    
                    CustomMessageBox.Show(message, "Files Not Accessible", 
                        CustomMessageBox.CustomMessageBoxButtons.OKOnly, 
                        CustomMessageBox.CustomMessageBoxIcon.Warning);
                }

                if (accessibleFiles.Count == 0)
                {
                    e.Handled = true;
                    return;
                }

                await AddDrawablesByDetectedGenderAsync(accessibleFiles);
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Error in Drop event: {ex.Message}", LogType.Error);
                
                CustomMessageBox.Show(
                    $"An error occurred while processing dropped files:\n\n{ex.Message}",
                    "Drag & Drop Error",
                    CustomMessageBox.CustomMessageBoxButtons.OKOnly,
                    CustomMessageBox.CustomMessageBoxIcon.Error);
            }
            
            e.Handled = true;
        }

        private static Enums.SexType? DetermineGenderFromFilename(string filePath)
        {
            var filename = Path.GetFileNameWithoutExtension(filePath).ToLowerInvariant();

            if (filename.StartsWith("mp_f") ||
                filename.Contains("mp_f_freemode") ||
                filename.Contains("_f_") ||
                filename.Contains("female"))
            {
                return Enums.SexType.female;
            }

            if (filename.StartsWith("mp_m") ||
                filename.Contains("mp_m_freemode") ||
                filename.Contains("_m_") ||
                filename.Contains("male"))
            {
                return Enums.SexType.male;
            }

            return null;
        }

        #endregion

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
