using CodeWalker.GameFiles;
using grzyClothTool.Controls;
using grzyClothTool.Helpers;
using grzyClothTool.Models.Texture;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace grzyClothTool.Views
{
    /// <summary>
    /// Interaction logic for OptimizeWindow.xaml
    /// </summary>
    public partial class OptimizeWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public List<dynamic> GTextures { get; set; }
        public GTextureDetails TextureDetails { get; set; }

        private GTextureDetails _outputTextureDetails;
        public GTextureDetails OutputTextureDetails
        {
            get => _outputTextureDetails;
            set
            {
                _outputTextureDetails = value;
                OnPropertyChanged();
            }
        }
        public string TextureName { get; set; }
        public bool MultipleTexturesSelected { get; set; }
        public int SelectedTextureCount { get; set; }

        public string[] AvailableTextureSizes { get; set; }

        private string _selectedTextureSize;
        public string SelectedTextureSize
        {
            get => _selectedTextureSize;
            set
            {
                _selectedTextureSize = value;
                OnPropertyChanged();
                ReloadOutputTexture();
            }
        }

        private bool _isTextureDownsizeEnabled;
        public bool IsTextureDownsizeEnabled
        {
            get => _isTextureDownsizeEnabled;
            set
            {
                if (_isTextureDownsizeEnabled != value)
                {
                    _isTextureDownsizeEnabled = value;

                    if (value && SelectedTextureSize != null)
                    {
                        var size = SelectedTextureSize.Split('x');
                        var width = int.Parse(size[0]);
                        var height = int.Parse(size[1]);
                        OutputTextureDetails.Width = width;
                        OutputTextureDetails.Height = height;
                        OutputTextureDetails.MipMapCount = ImgHelper.GetCorrectMipMapAmount(width, height);
                    }

                    if (!value)
                    {
                        OutputTextureDetails.Width = TextureDetails.Width;
                        OutputTextureDetails.Height = TextureDetails.Height;
                        OutputTextureDetails.MipMapCount = TextureDetails.MipMapCount;
                    }

                    OnPropertyChanged(nameof(IsTextureDownsizeEnabled));
                    OnPropertyChanged(nameof(OutputTextureDetails));
                }
            }
        }

        private TextureFormat? _textureFormat;
        public string SelectedCompression
        {
            get => _textureFormat.ToString();
            set
            {
                switch (value)
                {
                    case "DXT1 (no alpha)":
                        _textureFormat = TextureFormat.D3DFMT_DXT1;
                        break;
                    case "DXT3 (sharp alpha)":
                        _textureFormat = TextureFormat.D3DFMT_DXT3;
                        break;
                    case "DXT5 (gradient alpha)":
                        _textureFormat = TextureFormat.D3DFMT_DXT5;
                        break;
                }

                OnPropertyChanged();
                ReloadOutputTexture();
            }
        }
        
        public string[] AvailableCompression { get; set; } = ["DXT1 (no alpha)", "DXT3 (sharp alpha)", "DXT5 (gradient alpha)"];
        private bool _isTextureCompressionEnabled;
        public bool IsTextureCompressionEnabled
        {
            get => _isTextureCompressionEnabled;
            set
            {
                if (_isTextureCompressionEnabled != value)
                {
                    _isTextureCompressionEnabled = value;

                    if (value && !string.IsNullOrEmpty(SelectedCompression))
                    {
                        OutputTextureDetails.Compression = SelectedCompression;
                        ReloadOutputTexture();
                    }

                    if (!value)
                    {
                        OutputTextureDetails.Compression = TextureDetails.Compression;
                        ReloadOutputTexture();
                    }

                    OnPropertyChanged(nameof(IsTextureCompressionEnabled));
                }
            }
        }

        public OptimizeWindow(List<dynamic> txts, bool multipleTexturesSelected = false)
        {
            InitializeComponent();
            DataContext = this;

            GTextures = txts;
            TextureDetails = GetTextureDetails(GTextures[0]);

            OutputTextureDetails = new GTextureDetails
            {
                MipMapCount = ImgHelper.GetCorrectMipMapAmount(TextureDetails.Width, TextureDetails.Height),
                Compression = TextureDetails.Compression,
                Width = TextureDetails.Width,
                Height = TextureDetails.Height
            };

            var sizes = new List<string>();
            for (int i = 2; i < 6; i += 2)
            {
                var newWidth = TextureDetails.Width / i;
                var newHeight = TextureDetails.Height / i;

                sizes.Add($"{newWidth}x{newHeight}");
            }
            AvailableTextureSizes = [.. sizes];

            MultipleTexturesSelected = multipleTexturesSelected;
            SelectedTextureCount = txts.Count;

            if (!multipleTexturesSelected)
            {
                dynamic first = txts[0];

                TextureName = first switch
                {
                    GTexture gt => gt.DisplayName,
                    GTextureEmbedded ge => ge.Details.Name,
                    _ => string.Empty
                };
            }
        }

        public static GTextureDetails GetTextureDetails(dynamic gtxt)
        {
            GTextureDetails curDetails = null;

            if (gtxt is GTexture gTexture)
            {
                curDetails = gTexture.TxtDetails;
            }
            else if (gtxt is GTextureEmbedded gTextureEmbedded)
            {
                curDetails = gTextureEmbedded.Details;
            }

            var (correctWidth, correctHeight) = ImgHelper.CheckPowerOfTwo(curDetails.Width, curDetails.Height);

            return new GTextureDetails
            {
                MipMapCount = curDetails.MipMapCount,
                Compression = curDetails.Compression,
                Width = correctWidth,
                Height = correctHeight
            };
        }

        public static string CheckTexturesHaveSameSize(List<GTexture> textures)
        {
            if (textures.Count < 2)
                return null;

            var firstTextureDetails = GetTextureDetails(textures[0]);

            for (int i = 1; i < textures.Count; i++)
            {
                var currentTextureDetails = GetTextureDetails(textures[i]);

                if (firstTextureDetails.Width != currentTextureDetails.Width ||
                    firstTextureDetails.Height != currentTextureDetails.Height)
                {
                    return textures[i].DisplayName; // return the name of the texture that doesn't match
                }
            }

            return null;
        }

        private void OptimizeTexture_Click(object sender, RoutedEventArgs e)
        {
            if (!IsTextureDownsizeEnabled && !IsTextureCompressionEnabled)
            {
                Close();
                CustomMessageBox.Show("No optimization options selected");
                return;
            }

            ReloadOutputTexture();

            foreach (var txt in GTextures)
            {
                // We don't want to create it at the time of clicking this button, this should be saved and generated only during build
                txt.IsOptimizedDuringBuild = true;
                txt.OptimizeDetails = new GTextureDetails
                {
                    Width = OutputTextureDetails.Width,
                    Height = OutputTextureDetails.Height,
                    Compression = OutputTextureDetails.Compression,
                    MipMapCount = OutputTextureDetails.MipMapCount,
                    IsOptimizeNeeded = false
                };
            } 

            LogHelper.Log("Textures will be optimized during resource build");
            Close();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ReloadOutputTexture()
        {
            if (IsTextureDownsizeEnabled && SelectedTextureSize != null)
            {
                var size = SelectedTextureSize.Split('x');
                var width = int.Parse(size[0]);
                var height = int.Parse(size[1]);

                OutputTextureDetails.Width = width;
                OutputTextureDetails.Height = height;

                OutputTextureDetails.MipMapCount = ImgHelper.GetCorrectMipMapAmount(width, height);
            }

            if (IsTextureCompressionEnabled)
            {
                OutputTextureDetails.Compression = SelectedCompression;
            }

            OnPropertyChanged("OutputTextureDetails");
        }

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
