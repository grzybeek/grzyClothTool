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
                    var files = Directory.GetFiles(fldr, "*.ydd", SearchOption.AllDirectories)
                        .OrderBy(f =>
                        {
                            var number = FileHelper.GetDrawableNumberFromFileName(Path.GetFileName(f));
                            return number ?? int.MaxValue;
                        })
                        .ThenBy(Path.GetFileName)
                        .ToArray();
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

            if (Addon.SelectedDrawable.IsEncrypted)
            {
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
                    var hasYddFiles = CheckForYddFilesInDescriptor(e.Data);
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
                    var extractedFiles = await ExtractVirtualFilesAsync(e.Data);
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

                var accessibleFiles = new List<string>();
                var inaccessibleFiles = new List<string>();
                foreach (var file in yddFiles)
                {
                    if (File.Exists(file))
                    {
                        accessibleFiles.Add(file);
                    }
                    else
                    {
                        inaccessibleFiles.Add(file);
                    }
                }

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

                var maleFiles = new List<string>();
                var femaleFiles = new List<string>();
                var undeterminedFiles = new List<string>();

                foreach (var file in accessibleFiles)
                {
                    var detectedGender = DetermineGenderFromFilename(file);
                    
                    if (detectedGender == Enums.SexType.male)
                    {
                        maleFiles.Add(file);
                    }
                    else if (detectedGender == Enums.SexType.female)
                    {
                        femaleFiles.Add(file);
                    }
                    else
                    {
                        undeterminedFiles.Add(file);
                    }
                }

                ProgressHelper.Start();

                if (maleFiles.Count > 0)
                {
                    await MainWindow.AddonManager.AddDrawables(maleFiles.ToArray(), Enums.SexType.male);
                }

                if (femaleFiles.Count > 0)
                {
                    await MainWindow.AddonManager.AddDrawables(femaleFiles.ToArray(), Enums.SexType.female);
                }

                if (undeterminedFiles.Count > 0)
                {
                    var result = CustomMessageBox.Show(
                        $"{undeterminedFiles.Count} drawable(s) could not have gender automatically determined. Please select the gender for these files:",
                        "Select Gender",
                        CustomMessageBox.CustomMessageBoxButtons.MaleFemaleCancel);

                    if (result == CustomMessageBox.CustomMessageBoxResult.Male)
                    {
                        await MainWindow.AddonManager.AddDrawables(undeterminedFiles.ToArray(), Enums.SexType.male);
                    }
                    else if (result == CustomMessageBox.CustomMessageBoxResult.Female)
                    {
                        await MainWindow.AddonManager.AddDrawables(undeterminedFiles.ToArray(), Enums.SexType.female);
                    }
                }

                ProgressHelper.Stop("Added drawables in {0}", true);
                SaveHelper.SetUnsavedChanges(true);
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

        private async Task<List<string>> ExtractVirtualFilesAsync(System.Windows.IDataObject data)
        {
            var extractedFiles = new List<string>();
            
            try
            {
                var format = "FileGroupDescriptorW";
                if (!data.GetDataPresent(format))
                {
                    format = "FileGroupDescriptor";
                    if (!data.GetDataPresent(format))
                        return extractedFiles;
                }

                var descriptorStream = data.GetData(format) as System.IO.MemoryStream;
                if (descriptorStream == null)
                    return extractedFiles;

                if (!data.GetDataPresent("FileContents"))
                {
                    return extractedFiles;
                }

                var tempDir = Path.Combine(Path.GetTempPath(), "grzyClothTool_dragdrop", Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempDir);

                using var reader = new System.IO.BinaryReader(descriptorStream);
                int fileCount = reader.ReadInt32();

                for (int i = 0; i < fileCount; i++)
                {
                    try
                    {
                        // Parse file descriptor structure
                        descriptorStream.Seek(4 + (i * 592), System.IO.SeekOrigin.Begin);

                        uint flags = reader.ReadUInt32();
                        reader.ReadBytes(16); // CLSID (16 bytes)
                        reader.ReadBytes(16); // SIZEL (8 bytes) + POINTL (8 bytes)
                        uint fileAttributes = reader.ReadUInt32();
                        reader.ReadBytes(32); // File times (3x8 bytes + 8 padding)
                        uint fileSizeHigh = reader.ReadUInt32();
                        uint fileSizeLow = reader.ReadUInt32();

                        // Read filename (520 bytes for Unicode, null-terminated)
                        byte[] filenameBytes = reader.ReadBytes(520);
                        string filename = System.Text.Encoding.Unicode.GetString(filenameBytes).TrimEnd('\0');

                        if (!Path.GetExtension(filename).Equals(".ydd", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        // Try to get file content using COM IDataObject with lindex
                        System.IO.MemoryStream contentStream = null;

                        try
                        {
                            if (data is System.Runtime.InteropServices.ComTypes.IDataObject comData)
                            {
                                System.Runtime.InteropServices.ComTypes.FORMATETC formatEtc = new System.Runtime.InteropServices.ComTypes.FORMATETC
                                {
                                    cfFormat = (short)System.Windows.DataFormats.GetDataFormat("FileContents").Id,
                                    dwAspect = System.Runtime.InteropServices.ComTypes.DVASPECT.DVASPECT_CONTENT,
                                    lindex = i,
                                    ptd = IntPtr.Zero,
                                    tymed = System.Runtime.InteropServices.ComTypes.TYMED.TYMED_ISTREAM | System.Runtime.InteropServices.ComTypes.TYMED.TYMED_HGLOBAL
                                };

                                System.Runtime.InteropServices.ComTypes.STGMEDIUM medium = new System.Runtime.InteropServices.ComTypes.STGMEDIUM();
                                comData.GetData(ref formatEtc, out medium);

                                if (medium.unionmember != IntPtr.Zero)
                                {
                                    if (medium.tymed == System.Runtime.InteropServices.ComTypes.TYMED.TYMED_ISTREAM)
                                    {
                                        if (System.Runtime.InteropServices.Marshal.GetObjectForIUnknown(medium.unionmember) is System.Runtime.InteropServices.ComTypes.IStream iStream)
                                        {
                                            contentStream = new System.IO.MemoryStream();
                                            byte[] buffer = new byte[4096];
                                            IntPtr pcbRead = System.Runtime.InteropServices.Marshal.AllocHGlobal(sizeof(int));
                                            try
                                            {
                                                int bytesRead;
                                                do
                                                {
                                                    iStream.Read(buffer, buffer.Length, pcbRead);
                                                    bytesRead = System.Runtime.InteropServices.Marshal.ReadInt32(pcbRead);
                                                    if (bytesRead > 0)
                                                    {
                                                        contentStream.Write(buffer, 0, bytesRead);
                                                    }
                                                } while (bytesRead > 0);
                                            }
                                            finally
                                            {
                                                System.Runtime.InteropServices.Marshal.FreeHGlobal(pcbRead);
                                            }

                                            contentStream.Seek(0, System.IO.SeekOrigin.Begin);
                                        }
                                    }
                                    else if (medium.tymed == System.Runtime.InteropServices.ComTypes.TYMED.TYMED_HGLOBAL)
                                    {
                                        var ptr = System.Runtime.InteropServices.Marshal.ReadIntPtr(medium.unionmember);
                                        var size = (int)fileSizeLow;
                                        byte[] buffer = new byte[size];
                                        System.Runtime.InteropServices.Marshal.Copy(ptr, buffer, 0, size);
                                        contentStream = new System.IO.MemoryStream(buffer);
                                    }

                                    // Release the medium
                                    if (medium.pUnkForRelease != null)
                                    {
                                        System.Runtime.InteropServices.Marshal.ReleaseComObject(medium.pUnkForRelease);
                                    }
                                }
                            }
                        }
                        catch (Exception comEx)
                        {
                            LogHelper.Log($"Failed to extract file {i + 1} ({filename}): {comEx.Message}", LogType.Error);
                        }

                        if (contentStream != null && contentStream.Length > 0)
                        {
                            var outputPath = Path.Combine(tempDir, filename);

                            using (var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
                            {
                                contentStream.Seek(0, System.IO.SeekOrigin.Begin);
                                await contentStream.CopyToAsync(fileStream);
                            }

                            extractedFiles.Add(outputPath);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Log($"Error extracting file {i + 1}: {ex.Message}", LogType.Error);
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Error in ExtractVirtualFilesAsync: {ex.Message}", LogType.Error);
            }

            return extractedFiles;
        }

        private bool CheckForYddFilesInDescriptor(System.Windows.IDataObject data)
        {
            try
            {
                var format = "FileGroupDescriptorW";
                if (!data.GetDataPresent(format))
                {
                    format = "FileGroupDescriptor";
                    if (!data.GetDataPresent(format))
                        return false;
                }

                if (data.GetData(format) is not System.IO.MemoryStream stream)
                    return false;

                using var reader = new System.IO.BinaryReader(stream);
                int fileCount = reader.ReadInt32();

                // Read file descriptors
                for (int i = 0; i < fileCount; i++)
                {
                    // Skip to filename (offset 76 for descriptor header)
                    stream.Seek(4 + (i * 592) + 72, System.IO.SeekOrigin.Begin);

                    // Read filename (520 bytes, Unicode)
                    byte[] filenameBytes = reader.ReadBytes(520);
                    string filename = System.Text.Encoding.Unicode.GetString(filenameBytes).TrimEnd('\0');

                    if (Path.GetExtension(filename).Equals(".ydd", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Error checking FileGroupDescriptor: {ex.Message}", LogType.Error);
                return false;
            }
        }

        private static Enums.SexType? DetermineGenderFromFilename(string filePath)
        {
            var filename = Path.GetFileName(filePath).ToLowerInvariant();
            if (filename.Contains("mp_m_freemode") || filename.Contains("_m_") || filename.Contains("male"))
            {
                return Enums.SexType.male;
            }

            if (filename.Contains("mp_f_freemode") || filename.Contains("_f_") || filename.Contains("female"))
            {
                return Enums.SexType.female;
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
