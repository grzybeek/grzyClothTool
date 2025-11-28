using CodeWalker;
using grzyClothTool.Helpers;
using grzyClothTool.Models.Drawable;
using grzyClothTool.Models.Texture;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Forms;

namespace grzyClothTool.Controls
{
    /// <summary>
    /// Interaction logic for PreviewWindowHost.xaml
    /// </summary>
    public partial class PreviewWindowHost : System.Windows.Controls.UserControl
    {
        private CustomPedsForm _customPedsForm;
        private bool _isInitialized = false;

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
                PreviewHost.Child = _customPedsForm;
                PlaceholderText.Visibility = Visibility.Collapsed;
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

        public void InitializePreview()
        {
            if (_isInitialized && _customPedsForm != null && !_customPedsForm.IsDisposed)
            {
                PlaceholderText.Visibility = Visibility.Collapsed;
                return;
            }

            try
            {
                _customPedsForm = new CustomPedsForm
                {
                    TopLevel = false,
                    FormBorderStyle = FormBorderStyle.None,
                    Dock = DockStyle.Fill
                };

                PreviewHost.Child = _customPedsForm;
                _customPedsForm.Show();

                PlaceholderText.Visibility = Visibility.Collapsed;
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Error initializing 3D preview: {ex.Message}", Views.LogType.Error);
                PlaceholderText.Text = "Error loading 3D preview";
            }
        }

        public void InitializePreviewInBackground()
        {
            if (_isInitialized)
            {
                return;
            }

            try
            {
                _customPedsForm = new CustomPedsForm
                {
                    TopLevel = false,
                    FormBorderStyle = FormBorderStyle.None,
                    Dock = DockStyle.Fill
                };

                PreviewHost.Child = _customPedsForm;
                _customPedsForm.Show();

                _isInitialized = true;
            }
            catch (Exception ex)
            {
                LogHelper.Log($"Error initializing 3D preview in background: {ex.Message}", Views.LogType.Error);
            }
        }

        public void ClosePreview()
        {
            if (_customPedsForm != null && !_customPedsForm.IsDisposed)
            {
                PreviewHost.Child = null;
                _customPedsForm.Close();
                _customPedsForm = null;
                _isInitialized = false;
                PlaceholderText.Visibility = Visibility.Visible;
            }
        }

        public void SetPedModel(string pedModel)
        {
            if (_customPedsForm != null && !_customPedsForm.IsDisposed && _customPedsForm.formopen)
            {
                _customPedsForm.PedModel = pedModel;
            }
        }

        public void UpdateDrawables(ObservableCollection<GDrawable> selectedDrawables, GTexture selectedTexture, Dictionary<string, string> updateDict)
        {
            if (_customPedsForm == null || _customPedsForm.IsDisposed || !_customPedsForm.formopen || _customPedsForm.isLoading)
            {
                return;
            }

            var selectedNames = selectedDrawables.Select(d => d.Name).ToHashSet();
            var removedDrawables = _customPedsForm.LoadedDrawables.Keys.Where(name => !selectedNames.Contains(name)).ToList();
            foreach (var removed in removedDrawables)
            {
                if (_customPedsForm.LoadedDrawables.TryGetValue(removed, out var removedDrawable))
                {
                    _customPedsForm.LoadedTextures.Remove(removedDrawable);
                }
                _customPedsForm.LoadedDrawables.Remove(removed);
            }

            foreach (var drawable in selectedDrawables)
            {
                if (drawable.IsEncrypted)
                {
                    continue;
                }

                var ydd = CWHelper.CreateYddFile(drawable);
                if (ydd == null || ydd.Drawables.Length == 0) continue;

                var firstDrawable = ydd.Drawables.First();
                _customPedsForm.LoadedDrawables[drawable.Name] = firstDrawable;

                CodeWalker.GameFiles.YtdFile ytd = null;
                if (selectedTexture != null)
                {
                    ytd = CWHelper.CreateYtdFile(selectedTexture, selectedTexture.DisplayName);
                    _customPedsForm.LoadedTextures[firstDrawable] = ytd.TextureDict;
                }

                if (selectedTexture == null && selectedDrawables.Count > 1)
                {
                    var firstTexture = drawable.Textures.FirstOrDefault();
                    if (firstTexture != null)
                    {
                        ytd = CWHelper.CreateYtdFile(firstTexture, firstTexture.DisplayName);
                        _customPedsForm.LoadedTextures[firstDrawable] = ytd.TextureDict;
                    }
                }

                _customPedsForm.UpdateSelectedDrawable(
                    firstDrawable,
                    ytd?.TextureDict,
                    updateDict
                );
            }

            _customPedsForm.Refresh();
        }
    }
}
