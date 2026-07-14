using CodeWalker;
using grzyClothTool.Helpers;
using grzyClothTool.Models.Drawable;
using grzyClothTool.Models.Texture;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;

namespace grzyClothTool.Controls
{
    /// <summary>
    /// Interaction logic for PreviewWindowHost.xaml
    /// </summary>
    public partial class PreviewWindowHost : System.Windows.Controls.UserControl
    {
        private CustomPedsForm _customPedsForm;
        private bool _isInitialized = false;
        private const int MaxPendingPreviewUpdateAttempts = 120;
        private List<GDrawable> _pendingSelectedDrawables;
        private GTexture _pendingSelectedTexture;
        private Dictionary<string, string> _pendingUpdateDict;
        private DispatcherTimer _pendingPreviewUpdateTimer;
        private int _pendingPreviewUpdateAttempts;

        public static event EventHandler Preview3DAvailabilityChanged;

        public PreviewWindowHost()
        {
            InitializeComponent();
            this.Loaded += PreviewWindowHost_Loaded;
            this.Unloaded += PreviewWindowHost_Unloaded;
        }

        private void PreviewWindowHost_Loaded(object sender, RoutedEventArgs e)
        {
            if (_isInitialized && _customPedsForm != null && !_customPedsForm.IsDisposed && PreviewHost.Child == null)
            {
                AttachPreviewForm();
            }
            else if (!_isInitialized)
            {
                InitializePreview();
            }
        }

        private void PreviewWindowHost_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_customPedsForm != null && !_customPedsForm.IsDisposed && PreviewHost.Child != null)
            {
                PreviewHost.Child = null;
                PlaceholderText.Visibility = Visibility.Visible;
            }
        }


        private bool BlockPreviewIfGtaFolderInvalid()
        {
            if (CWHelper.IsGTAFolderValid())
            {
                return false;
            }

            LogHelper.Log("3D Preview unavailable: no valid GTA V folder is set. Set it in Settings to enable the preview.", Views.LogType.Warning);
            if (PlaceholderText != null)
            {
                PlaceholderText.Text = "3D Preview unavailable - set a valid GTA V path in Settings";
                PlaceholderText.Visibility = Visibility.Visible;
            }

            SettingsHelper.Preview3DAvailable = false;
            Preview3DAvailabilityChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }

        public void InitializePreview()
        {
            if (_isInitialized && _customPedsForm != null && !_customPedsForm.IsDisposed)
            {
                AttachPreviewForm();
                return;
            }

            if (BlockPreviewIfGtaFolderInvalid())
            {
                return;
            }

            try
            {
                CreatePreviewForm();
                AttachPreviewForm();

                _isInitialized = true;
                SettingsHelper.Preview3DAvailable = true;
                Preview3DAvailabilityChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                var errorMsg = $"3D Preview initialization failed: {ex.Message}";
                LogHelper.Log(errorMsg, Views.LogType.Error);
                ErrorLogHelper.LogError("3D Preview initialization failed", ex);
                
                bool isGtaError = ex.Message.Contains("GTA") || ex.Message.Contains("DLC") || ex.Message.Contains("corrupted");
                PlaceholderText.Text = isGtaError 
                    ? "3D Preview unavailable (GTA V installation issue - see log)" 
                    : "3D Preview unavailable (GPU/Graphics error - see log)";
                
                SettingsHelper.Preview3DAvailable = false;
                Preview3DAvailabilityChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void InitializePreviewInBackground()
        {
            if (_isInitialized)
            {
                AttachPreviewForm();
                return;
            }

            if (BlockPreviewIfGtaFolderInvalid())
            {
                return;
            }

            try
            {
                CreatePreviewForm();
                AttachPreviewForm();

                _isInitialized = true;
                SettingsHelper.Preview3DAvailable = true;
                Preview3DAvailabilityChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                var errorMsg = $"3D Preview background initialization failed: {ex.Message}";
                LogHelper.Log(errorMsg, Views.LogType.Error);
                ErrorLogHelper.LogError("3D Preview background initialization failed", ex);
                SettingsHelper.Preview3DAvailable = false;
                Preview3DAvailabilityChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void CreatePreviewForm()
        {
            _customPedsForm = new CustomPedsForm
            {
                TopLevel = false,
                FormBorderStyle = FormBorderStyle.None,
                Dock = DockStyle.Fill
            };
        }

        private void AttachPreviewForm()
        {
            if (_customPedsForm == null || _customPedsForm.IsDisposed)
            {
                _isInitialized = false;
                return;
            }

            if (PreviewHost.Child != _customPedsForm)
            {
                PreviewHost.Child = _customPedsForm;
            }

            if (!_customPedsForm.Visible)
            {
                _customPedsForm.Show();
            }

            PlaceholderText.Visibility = Visibility.Collapsed;
        }

        public void ClosePreview()
        {
            if (_customPedsForm != null)
            {
                try
                {
                    _customPedsForm = null;
                    _isInitialized = false;
                    Environment.Exit(0);
                }
                catch (Exception ex)
                {
                    LogHelper.Log($"Error closing preview: {ex.Message}", Views.LogType.Error);

                    _customPedsForm = null;
                    _isInitialized = false;
                }
            }
        }

        public void SetPedModel(string pedModel)
        {
            try
            {
                if (_customPedsForm != null && !_customPedsForm.IsDisposed)
                {
                    _customPedsForm.PedModel = pedModel;
                }
            }
            catch (Exception ex)
            {
                HandlePreviewError("Failed to set ped model", ex);
            }
        }

        public void UpdateDrawables(ObservableCollection<GDrawable> selectedDrawables, GTexture selectedTexture, Dictionary<string, string> updateDict)
        {
            try
            {
                var selectedDrawableList = selectedDrawables?.ToList() ?? [];
                var updates = updateDict != null
                    ? new Dictionary<string, string>(updateDict)
                    : [];

                if (!IsPreviewReady())
                {
                    QueuePendingDrawableUpdate(selectedDrawableList, selectedTexture, updates);
                    return;
                }

                UpdateDrawablesNow(selectedDrawableList, selectedTexture, updates);
            }
            catch (Exception ex)
            {
                HandlePreviewError("Failed to update drawables in 3D preview", ex);
            }
        }

        private bool IsPreviewReady()
        {
            return _customPedsForm != null &&
                   !_customPedsForm.IsDisposed &&
                   _customPedsForm.formopen &&
                   !_customPedsForm.isLoading;
        }

        private void QueuePendingDrawableUpdate(List<GDrawable> selectedDrawables, GTexture selectedTexture, Dictionary<string, string> updateDict)
        {
            _pendingSelectedDrawables = selectedDrawables;
            _pendingSelectedTexture = selectedTexture;
            _pendingUpdateDict = updateDict;
            _pendingPreviewUpdateAttempts = 0;

            if (_pendingPreviewUpdateTimer == null)
            {
                _pendingPreviewUpdateTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(250)
                };
                _pendingPreviewUpdateTimer.Tick += PendingPreviewUpdateTimer_Tick;
            }

            if (!_pendingPreviewUpdateTimer.IsEnabled)
            {
                _pendingPreviewUpdateTimer.Start();
            }
        }

        private void PendingPreviewUpdateTimer_Tick(object sender, EventArgs e)
        {
            if (_pendingSelectedDrawables == null)
            {
                _pendingPreviewUpdateTimer.Stop();
                return;
            }

            if (!IsPreviewReady())
            {
                _pendingPreviewUpdateAttempts++;
                if (_pendingPreviewUpdateAttempts >= MaxPendingPreviewUpdateAttempts)
                {
                    _pendingPreviewUpdateTimer.Stop();
                    LogHelper.Log("3D Preview did not finish loading in time; selected drawable update was skipped.", Views.LogType.Warning);
                }
                return;
            }

            var selectedDrawables = _pendingSelectedDrawables;
            var selectedTexture = _pendingSelectedTexture;
            var updateDict = _pendingUpdateDict ?? [];

            _pendingSelectedDrawables = null;
            _pendingSelectedTexture = null;
            _pendingUpdateDict = null;
            _pendingPreviewUpdateTimer.Stop();

            try
            {
                UpdateDrawablesNow(selectedDrawables, selectedTexture, updateDict);
            }
            catch (Exception ex)
            {
                HandlePreviewError("Failed to apply pending drawable update in 3D preview", ex);
            }
        }

        private void UpdateDrawablesNow(List<GDrawable> selectedDrawables, GTexture selectedTexture, Dictionary<string, string> updateDict)
        {
            if (_customPedsForm?.Renderer == null)
            {
                QueuePendingDrawableUpdate(selectedDrawables, selectedTexture, updateDict);
                return;
            }

            lock (_customPedsForm.Renderer.RenderSyncRoot)
            {
                var selectedNames = selectedDrawables.Select(d => d.Name).ToHashSet();
                var removedDrawables = _customPedsForm.LoadedDrawables.Keys.Where(name => !selectedNames.Contains(name)).ToList();
                foreach (var removed in removedDrawables)
                {
                    if (_customPedsForm.LoadedDrawables.TryGetValue(removed, out var removedDrawable))
                    {
                        _customPedsForm.LoadedTextures.Remove(removedDrawable);
                        if (_customPedsForm.Renderer?.SelDrawable == removedDrawable)
                        {
                            _customPedsForm.Renderer.SelDrawable = null;
                            _customPedsForm.Renderer.SelectedDrawable = null;
                            _customPedsForm.Renderer.renderfloor = false;
                            _customPedsForm.Renderer.SelectedDrawableChanged = false;
                        }
                    }
                    _customPedsForm.LoadedDrawables.Remove(removed);
                }

                if (selectedDrawables.Count == 0 && _customPedsForm.Renderer != null)
                {
                    _customPedsForm.Renderer.SelDrawable = null;
                    _customPedsForm.Renderer.SelectedDrawable = null;
                    _customPedsForm.Renderer.renderfloor = false;
                    _customPedsForm.Renderer.SelectedDrawableChanged = false;
                }

                foreach (var drawable in selectedDrawables)
                {
                    if (drawable.IsEncrypted)
                    {
                        continue;
                    }

                    CodeWalker.GameFiles.Drawable firstDrawable;
                    CodeWalker.GameFiles.YtdFile ytd = null;

                    // Load the geometry (YDD). If this fails there is nothing to render, so skip it.
                    try
                    {
                        var ydd = CWHelper.CreateYddFile(drawable);
                        if (ydd == null || ydd.Drawables.Length == 0)
                        {
                            RemoveLoadedDrawable(drawable.Name);
                            continue;
                        }

                        firstDrawable = ydd.Drawables.First();
                    }
                    catch (Exception ex)
                    {
                        RemoveLoadedDrawable(drawable.Name);
                        LogHelper.Log($"Skipped drawable '{drawable.Name}' in 3D preview: {ex.Message}", Views.LogType.Warning);
                        continue;
                    }

                    // Load the texture separately. A missing/broken texture (e.g. the source image
                    // was moved or deleted in an external project) must NOT drop the whole drawable -
                    // it should still render, just untextured, with a clear warning.
                    try
                    {
                        var textureForDrawable = selectedTexture != null && drawable.Textures.Contains(selectedTexture)
                            ? selectedTexture
                            : drawable.Textures.FirstOrDefault();

                        if (textureForDrawable != null)
                        {
                            ytd = CWHelper.CreateYtdFile(textureForDrawable, textureForDrawable.DisplayName);
                        }
                    }
                    catch (Exception ex)
                    {
                        ytd = null;
                        LogHelper.Log($"Could not load texture for '{drawable.Name}' in 3D preview; it will show untextured: {ex.Message}", Views.LogType.Warning);
                    }

                   
                    try
                    {
                        if (_customPedsForm.LoadedDrawables.TryGetValue(drawable.Name, out var existingDrawable))
                        {
                            _customPedsForm.LoadedTextures.Remove(existingDrawable);
                        }

                        _customPedsForm.LoadedDrawables[drawable.Name] = firstDrawable;
                        if (ytd?.TextureDict != null)
                        {
                            _customPedsForm.LoadedTextures[firstDrawable] = ytd.TextureDict;
                        }

                        _customPedsForm.UpdateSelectedDrawable(
                            firstDrawable,
                            ytd?.TextureDict,
                            updateDict
                        );
                    }
                    catch (Exception ex)
                    {
                        RemoveLoadedDrawable(drawable.Name);
                        LogHelper.Log($"Skipped drawable '{drawable.Name}' in 3D preview: {ex.Message}", Views.LogType.Warning);
                        continue;
                    }
                }

                _customPedsForm.Refresh();
            }
        }

        private void RemoveLoadedDrawable(string drawableName)
        {
            if (_customPedsForm?.Renderer == null)
            {
                return;
            }

            if (_customPedsForm.LoadedDrawables.TryGetValue(drawableName, out var loadedDrawable))
            {
                _customPedsForm.LoadedTextures.Remove(loadedDrawable);
                if (_customPedsForm.Renderer.SelDrawable == loadedDrawable)
                {
                    _customPedsForm.Renderer.SelDrawable = null;
                    _customPedsForm.Renderer.SelectedDrawable = null;
                    _customPedsForm.Renderer.renderfloor = false;
                    _customPedsForm.Renderer.SelectedDrawableChanged = false;
                }
            }

            _customPedsForm.LoadedDrawables.Remove(drawableName);
        }

        private void HandlePreviewError(string context, Exception ex)
        {
            ErrorLogHelper.LogError($"3D Preview error - {context}: {ex.Message}", ex);
            LogHelper.Log($"3D Preview error: {context}", Views.LogType.Warning);
            
            // Disable the preview if it's having issues
            try
            {
                if (_customPedsForm != null && !_customPedsForm.IsDisposed)
                {
                    _customPedsForm.Dispose();
                }
                _customPedsForm = null;
                _isInitialized = false;
                SettingsHelper.Preview3DAvailable = false;
                
                if (PlaceholderText != null)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        PlaceholderText.Text = "3D Preview disabled due to errors (see log)";
                        PlaceholderText.Visibility = Visibility.Visible;
                    });
                }
                
                Preview3DAvailabilityChanged?.Invoke(this, EventArgs.Empty);
            }
            catch
            {
                // Silently fail if we can't clean up
            }
        }
    }
}
