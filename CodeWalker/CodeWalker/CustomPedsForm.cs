using CodeWalker.GameFiles;
using CodeWalker.Properties;
using CodeWalker.Rendering;
using CodeWalker.Utils;
using CodeWalker.World;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using Color = SharpDX.Color;

namespace CodeWalker
{
    public partial class CustomPedsForm : Form, DXForm
    {
        public Form Form { get { return this; } } //for DXForm/DXManager use

        public Renderer Renderer = null;
        public object RenderSyncRoot { get { return Renderer.RenderSyncRoot; } }

        public volatile bool formopen = false;

        public bool isLoading = true;
        volatile bool running = false;
        volatile bool pauserendering = false;
        //volatile bool initialised = false;

        Stopwatch frametimer = new Stopwatch();
        Camera camera;
        Timecycle timecycle;
        Weather weather;
        Clouds clouds;

        Entity camEntity = new Entity();


        bool MouseLButtonDown = false;
        bool MouseRButtonDown = false;
        int MouseX;
        int MouseY;
        System.Drawing.Point MouseDownPoint;
        System.Drawing.Point MouseLastPoint;

        public GameFileCache GameFileCache { get; } = new GameFileCache(Settings.Default.CacheSize, Settings.Default.CacheTime, GTAFolder.CurrentGTAFolder, Settings.Default.DLC, false, "levels;anim;audio;data;");


        InputManager Input = new InputManager();


        bool initedOk = false;



        bool toolsPanelResizing = false;
        int toolsPanelResizeStartX = 0;
        int toolsPanelResizeStartLeft = 0;
        int toolsPanelResizeStartRight = 0;


        public string PedModel = "mp_f_freemode_01";
        Ped SelectedPed = new Ped();
        public Dictionary<string, Drawable> SavedDrawables = new Dictionary<string, Drawable>();
        public Dictionary<string, Drawable> LoadedDrawables = new Dictionary<string, Drawable>();
        public Dictionary<Drawable, TextureDictionary> LoadedTextures = new Dictionary<Drawable, TextureDictionary>();
        public Dictionary<Drawable, TextureDictionary> SavedTextures = new Dictionary<Drawable, TextureDictionary>();

        string liveTexturePath = null;
        DateTime liveTextureLastWriteTime;
        Texture LiveTexture = new Texture();

        List<List<ComponentComboItem>> ComponentComboBoxes;
        private Dictionary<string, List<VertexTypePC>> floorVerticesDict = new Dictionary<string, List<VertexTypePC>>();
        private readonly Vector3[] floorVertices = new Vector3[]
        {
            new Vector3(-1.0f, 1.0f, -1.0f),
            new Vector3(1.0f, -1.0f, -1.0f),
            new Vector3(1.0f, 1.0f,  -1.0f),

            new Vector3(-1.0f, 1.0f, -1.0f),
            new Vector3(-1.0f, -1.0f, -1.0f),
            new Vector3(1.0f, -1.0f, -1.0f),

            new Vector3(-1.0f, 1.0f, -1.0f),
            new Vector3(1.0f, 1.0f,  -1.0f),
            new Vector3(1.0f, -1.0f, -1.0f),

            new Vector3(-1.0f, 1.0f, -1.0f),
            new Vector3(1.0f, -1.0f, -1.0f),
            new Vector3(-1.0f, -1.0f, -1.0f)
        };

        private bool highheelvaluechanged = false;
        private bool renderOnlySelected = false;

        private System.Windows.Forms.Timer autoRotateTimer;
        private float autoRotateAngle = 0f;
        private const float AutoRotateSpeed = 1.0f;

        public class ComponentComboItem
        {
            public MCPVDrawblData DrawableData { get; set; }
            public int AlternativeIndex { get; set; }
            public int TextureIndex { get; set; }
            public ComponentComboItem(MCPVDrawblData drawableData, int altIndex = 0, int textureIndex = -1)
            {
                DrawableData = drawableData;
                AlternativeIndex = altIndex;
                TextureIndex = textureIndex;
            }
            public override string ToString()
            {
                if (DrawableData == null) return TextureIndex.ToString();
                var itemname = DrawableData.GetDrawableName(AlternativeIndex);
                if (DrawableData.TexData?.Length > 0) return itemname + " + " + DrawableData.GetTextureSuffix(TextureIndex);
                return itemname;
            }
            public string DrawableName
            {
                get
                {
                    return DrawableData?.GetDrawableName(AlternativeIndex) ?? "error";
                }
            }
            public string TextureName
            {
                get
                {
                    return DrawableData?.GetTextureName(TextureIndex);
                }
            }
        }

        public CustomPedsForm()
        {

            InitializeComponent();

            ComponentComboBoxes = new List<List<ComponentComboItem>>
            {
                new List<ComponentComboItem>(),
                new List<ComponentComboItem>(),
                new List<ComponentComboItem>(),
                new List<ComponentComboItem>(),
                new List<ComponentComboItem>(),
                new List<ComponentComboItem>(),
                new List<ComponentComboItem>(),
                new List<ComponentComboItem>(),
                new List<ComponentComboItem>(),
                new List<ComponentComboItem>(),
                new List<ComponentComboItem>(),
                new List<ComponentComboItem>(),
            };

            Renderer = new Renderer(this, GameFileCache);
            camera = Renderer.camera;
            timecycle = Renderer.timecycle;
            weather = Renderer.weather;
            clouds = Renderer.clouds;

            initedOk = Renderer.Init();

            Renderer.controllightdir = !Settings.Default.Skydome;
            Renderer.rendercollisionmeshes = false;
            Renderer.renderclouds = false;
            //Renderer.renderclouds = true;
            //Renderer.individualcloudfrag = "Contrails";
            Renderer.rendermoon = true;
            Renderer.renderskeletons = false;
            Renderer.SelectionFlagsTestAll = true;
            Renderer.swaphemisphere = true;


            Renderer.renderskydome = false;
            Renderer.renderhdtextures = true;

            autoRotateTimer = new System.Windows.Forms.Timer();
            autoRotateTimer.Interval = 16; // About 60 FPS
            autoRotateTimer.Tick += AutoRotateTimer_Tick;
        }

        public override void Refresh()
        {
            base.Refresh();
            UpdateModelsUI();
        }

        public void InitScene(Device device)
        {
            int width = ClientSize.Width;
            int height = ClientSize.Height;

            try
            {
                Renderer.DeviceCreated(device, width, height);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading shaders!\n" + ex.ToString());
                return;
            }


            SetDefaultCameraPosition();

            Renderer.shaders.deferred = false; //no point using this here yet
            Renderer.shaders.AnisotropicFiltering = true;
            Renderer.shaders.hdr = false;

            LoadSettings();


            formopen = true;
            new Thread(new ThreadStart(ContentThread)).Start();

            frametimer.Start();

            floorVerticesDict = new Dictionary<string, List<VertexTypePC>>
            {
                { "DrawableFloor", CreateFloorVertices(new Color(50, 50, 50, 255)) },
                { "PreviewFloor", CreateFloorVertices(new Color(90, 90, 90, 255)) }
            };
        }

        private List<VertexTypePC> CreateFloorVertices(Color color)
        {
            var verticesList = new List<VertexTypePC>(floorVertices.Length);
            uint colorValue = (uint)color.ToRgba();

            foreach (var vertex in floorVertices)
            {
                verticesList.Add(new VertexTypePC
                {
                    Position = vertex,
                    Colour = colorValue
                });
            }

            return verticesList;
        }

        public void CleanupScene()
        {
            formopen = false;

            Renderer.DeviceDestroyed();

            int count = 0;
            while (running && (count < 5000)) //wait for the content thread to exit gracefully
            {
                Thread.Sleep(1);
                count++;
            }
        }

        public void RenderScene(DeviceContext context)
        {
            float elapsed = (float)frametimer.Elapsed.TotalSeconds;
            frametimer.Restart();

            if (pauserendering) return;

            GameFileCache.BeginFrame();

            if (!Monitor.TryEnter(Renderer.RenderSyncRoot, 50))
            { return; } //couldn't get a lock, try again next time

            UpdateControlInputs(elapsed);

            Renderer.Update(elapsed, MouseLastPoint.X, MouseLastPoint.Y);

            Renderer.BeginRender(context);

            Renderer.RenderSkyAndClouds();

            RenderSelectedItems();

            RenderFloor();

            Renderer.SelectedDrawable = null;

            Renderer.RenderPed(SelectedPed);

            Renderer.RenderQueued();

            Renderer.RenderFinalPass();

            Renderer.EndRender();

            Monitor.Exit(Renderer.RenderSyncRoot);
        }

        private void RenderSelectedItems()
        {
            foreach(var drawable in LoadedDrawables.Values)
            {
                if (LoadedTextures.TryGetValue(drawable, out var texture))
                {
                    RenderSelectedItem(drawable, texture);
                }
            }

            if (SavedDrawables.Count > 0)
            {
                foreach (var drawable in SavedDrawables.Values)
                {
                    if (LoadedDrawables.TryGetValue(drawable.Name, out var loadedDrawable) && loadedDrawable.Name.Equals(drawable.Name))
                    {
                        continue;
                    }
                    if (!SavedTextures.ContainsKey(drawable))
                    {
                        continue;
                    }

                    RenderSelectedItem(drawable, SavedTextures[drawable]);
                }
            }
        }
        private void RenderSelectedItem(Drawable d, TextureDictionary t)
        {
            // dirty hack to render drawable only when all other drawables are rendered, it fixes issue that props sometimes are not rendered attached to the head
            if (Renderer.RenderedDrawablesDict.Count < 4) return;

            var isProp = d.Name.StartsWith("p_");
            d.Owner = SelectedPed;

            if(d.Skeleton == null || d.Skeleton.Bones.Items.Length == 0)
            {
                d.Skeleton = SelectedPed.Skeleton.Clone();
            }

            if(liveTexturePath != null)
            {
                var files = Directory.GetFiles(liveTexturePath, "*.dds");
                if(files.Length > 0)
                {
                    var file = files[0];
                    var fileinfo = new FileInfo(file);
                    var lastwritetime = fileinfo.LastWriteTime;
                    
                    if (lastwritetime > liveTextureLastWriteTime)
                    {
                        liveTextureLastWriteTime = lastwritetime;

                        LiveTexture = DDSIO.GetTexture(File.ReadAllBytes(file));

                    }
                    Renderer.RenderDrawable(d, null, SelectedPed.RenderEntity, 0, null, LiveTexture, SelectedPed.AnimClip, null, null, isProp, true);
                }
                return;
            }

            if(t.Textures.data_items.Count() == 0)
            {
                return;
            }

            Renderer.RenderDrawable(d, null, SelectedPed.RenderEntity, 0, t, t.Textures.data_items[0], SelectedPed.AnimClip, null, null, isProp, true);
        }

        private void RenderFloor()
        {
            if (Renderer.renderfloor)
            {
                if (floorVerticesDict.TryGetValue("DrawableFloor", out var floorVerticesList))
                {
                    if (Renderer.SelDrawable != null && Renderer.SelDrawable.IsHighHeelsEnabled)
                    {
                        List<VertexTypePC> newFloorVerticesList = new List<VertexTypePC>();

                        if (highheelvaluechanged || newFloorVerticesList.Count == 0)
                        {
                            newFloorVerticesList.Clear();

                            foreach (var ver in floorVerticesList)
                            {
                                var newPosition = new Vector3(ver.Position.X, ver.Position.Y, ver.Position.Z - Renderer.SelDrawable.HighHeelsValue);
                                newFloorVerticesList.Add(new VertexTypePC
                                {
                                    Position = newPosition,
                                    Colour = ver.Colour
                                });
                            }

                            highheelvaluechanged = false;
                        }

                        Renderer.RenderTriangles(newFloorVerticesList);
                    }
                    else
                    {
                        Renderer.RenderTriangles(floorVerticesList);
                    }
                }
            }

            if (floorCheckbox.Checked)
            {
                if (floorVerticesDict.TryGetValue("PreviewFloor", out var previewFloorVerticesList))
                {
                    List<VertexTypePC> newPreviewFloorVerticesList = new List<VertexTypePC>();
                    float floorUpDownValue = (float)floorUpDown.Value / 10;

                    foreach (var ver in previewFloorVerticesList)
                    {
                        var newPosition = new Vector3(ver.Position.X, ver.Position.Y, ver.Position.Z - floorUpDownValue);
                        newPreviewFloorVerticesList.Add(new VertexTypePC
                        {
                            Position = newPosition,
                            Colour = ver.Colour
                        });
                    }

                    Renderer.RenderTriangles(newPreviewFloorVerticesList);
                }
            }
        }

        public void BuffersResized(int w, int h)
        {
            Renderer.BuffersResized(w, h);
        }
        public bool ConfirmQuit()
        {
            return true;
        }

        private void Init()
        {
            //called from PedForm_Load

            if (!initedOk)
            {
                Close();
                return;
            }

            MouseWheel += PedsForm_MouseWheel;

            if (!GTAFolder.UpdateGTAFolder(true))
            {
                Close();
                return;
            }

            ShaderParamNames[] texsamplers = RenderableGeometry.GetTextureSamplerList();
            foreach (var texsampler in texsamplers)
            {
                TextureSamplerComboBox.Items.Add(texsampler);
            }

            Input.Init();

            Renderer.Start();
        }

        private void ContentThread()
        {
            //main content loading thread.
            running = true;

            UpdateStatus("Scanning...");

            try
            {
                GTA5Keys.LoadFromPath(GTAFolder.CurrentGTAFolder, Settings.Default.Key);
            }
            catch
            {
                MessageBox.Show("Keys not found! This shouldn't happen.");
                Close();
                return;
            }

            GameFileCache.EnableDlc = false;
            GameFileCache.EnableMods = false;
            GameFileCache.LoadPeds = true;
            GameFileCache.LoadVehicles = false;
            GameFileCache.LoadArchetypes = false;//to speed things up a little
            GameFileCache.BuildExtendedJenkIndex = false;//to speed things up a little
            GameFileCache.DoFullStringIndex = true;//to get all global text from DLC...
            GameFileCache.Init(UpdateStatus, LogError);

            UpdateGlobalPedsUI();


            LoadWorld();
            isLoading = false;
            Task.Run(() => {
                while (formopen && !IsDisposed) //renderer content loop
                {
                    bool rcItemsPending = Renderer.ContentThreadProc();
                    if (!rcItemsPending)
                    {
                        Thread.Sleep(1); //sleep if there's nothing to do
                    }
                }
            });

            while (formopen && !IsDisposed) //main asset loop
            {
                bool fcItemsPending = GameFileCache.ContentThreadProc();
                if (!fcItemsPending)
                {
                    Thread.Sleep(1); //sleep if there's nothing to do
                }
            }

            GameFileCache.Clear();
            running = false;
        }

        private void LoadSettings()
        {
            var s = Settings.Default;
            WireframeCheckBox.Checked = false;
            HDRRenderingCheckBox.Checked = false;
            ShadowsCheckBox.Checked = true;

            EnableAnimationCheckBox.Checked = false;
            ClipComboBox.Enabled = false;
            ClipDictComboBox.Enabled = false;
            EnableRootMotionCheckBox.Enabled = false;
            PlaybackSpeedTrackBar.Enabled = false;

            RenderModeComboBox.SelectedIndex = Math.Max(RenderModeComboBox.FindString(s.RenderMode), 0);
            TextureSamplerComboBox.SelectedIndex = Math.Max(TextureSamplerComboBox.FindString(s.RenderTextureSampler), 0);
            TextureCoordsComboBox.SelectedIndex = Math.Max(TextureCoordsComboBox.FindString(s.RenderTextureSamplerCoord), 0);
        }
        private void LoadWorld()
        {
            UpdateStatus("Loading timecycles...");
            timecycle.Init(GameFileCache, UpdateStatus);
            timecycle.SetTime(Renderer.timeofday);

            UpdateStatus("Loading materials...");
            BoundsMaterialTypes.Init(GameFileCache);

            UpdateStatus("Loading weather...");
            weather.Init(GameFileCache, UpdateStatus, timecycle);

            UpdateStatus("Loading clouds...");
            clouds.Init(GameFileCache, UpdateStatus, weather);

        }

        private void UpdateStatus(string text)
        {
            try
            {
                if (InvokeRequired)
                {
                    BeginInvoke(new Action(() => { UpdateStatus(text); }));
                }
                else
                {
                    StatusLabel.Text = text;
                }
            }
            catch { }
        }
        private void LogError(string text)
        {
            try
            {
                if (InvokeRequired)
                {
                    Invoke(new Action(() => { LogError(text); }));
                }
                else
                {
                    //TODO: error logging..
                    ConsoleTextBox.AppendText(text + "\r\n");
                    //StatusLabel.Text = text;
                    //MessageBox.Show(text);
                }
            }
            catch { }
        }

        private void UpdateMousePosition(MouseEventArgs e)
        {
            MouseX = e.X;
            MouseY = e.Y;
            MouseLastPoint = e.Location;
        }

        private void RotateCam(int dx, int dy)
        {
            camera.MouseRotate(dx, dy);
        }

        private void MoveCameraToView(Vector3 pos, float rad)
        {
            //move the camera to a default place where the given sphere is fully visible.

            rad = Math.Max(0.01f, rad*0.1f);

            camera.FollowEntity.Position = pos;
            camera.TargetDistance = rad * 1.2f;
            camera.CurrentDistance = rad * 1.2f;

            camera.UpdateProj = true;

        }
        private void AddDrawableTreeNode(DrawableBase drawable, string name, bool check)
        {
            var tnode = TexturesTreeView.Nodes.Add(name);
            var dnode = ModelsTreeView.Nodes.Add(name);
            dnode.Tag = drawable;
            dnode.Checked = check;

            if (name.Contains("Selected"))
            {
                string drawableTypeName;
                string[] nameParts = name.Split(' ')[1].Trim('(', ')').Split('_');

                if (nameParts.Length <= 1 || nameParts[0] != "p")
                {
                    drawableTypeName = nameParts[0];
                    var sameName = ModelsTreeView.Nodes.Cast<TreeNode>().Where(n =>
                        n.Text.Contains(drawableTypeName) &&
                        !n.Text.Contains("Selected") &&
                        !n.Text.Contains("Saved")
                    ).ToList();
                    if (sameName.Count > 0)
                    {
                        foreach (var node in sameName)
                        {
                            node.Checked = !node.Checked;
                        }
                    }
                }
            }

            AddDrawableModelsTreeNodes(drawable.DrawableModels?.High, "High Detail", true, dnode, tnode);
            AddDrawableModelsTreeNodes(drawable.DrawableModels?.Med, "Medium Detail", false, dnode, tnode);
            AddDrawableModelsTreeNodes(drawable.DrawableModels?.Low, "Low Detail", false, dnode, tnode);
            AddDrawableModelsTreeNodes(drawable.DrawableModels?.VLow, "Very Low Detail", false, dnode, tnode);

        }
        private void AddDrawableModelsTreeNodes(DrawableModel[] models, string prefix, bool check, TreeNode parentDrawableNode = null, TreeNode parentTextureNode = null)
        {
            if (models == null) return;

            for (int mi = 0; mi < models.Length; mi++)
            {
                var tnc = (parentDrawableNode != null) ? parentDrawableNode.Nodes : ModelsTreeView.Nodes;

                var model = models[mi];
                string mprefix = prefix + " " + (mi + 1).ToString();
                var mnode = tnc.Add(mprefix + " " + model.ToString());
                mnode.Tag = model;
                mnode.Checked = check;

                var ttnc = (parentTextureNode != null) ? parentTextureNode.Nodes : TexturesTreeView.Nodes;
                var tmnode = ttnc.Add(mprefix + " " + model.ToString());
                tmnode.Tag = model;

                if (!check)
                {
                    Renderer.SelectionModelDrawFlags[model] = false;
                }

                if (model.Geometries == null) continue;

                foreach (var geom in model.Geometries)
                {
                    var gname = geom.ToString();
                    var gnode = mnode.Nodes.Add(gname);
                    gnode.Tag = geom;
                    gnode.Checked = true;// check;

                    var tgnode = tmnode.Nodes.Add(gname);
                    tgnode.Tag = geom;

                    if ((geom.Shader != null) && (geom.Shader.ParametersList != null) && (geom.Shader.ParametersList.Hashes != null))
                    {
                        var pl = geom.Shader.ParametersList;
                        var h = pl.Hashes;
                        var p = pl.Parameters;
                        for (int ip = 0; ip < h.Length; ip++)
                        {
                            var hash = pl.Hashes[ip];
                            var parm = pl.Parameters[ip];
                            var tex = parm.Data as TextureBase;
                            if (tex != null)
                            {
                                var t = tex as Texture;
                                var tstr = tex.Name.Trim();
                                if (t != null)
                                {
                                    tstr = string.Format("{0} ({1}x{2}, embedded)", tex.Name, t.Width, t.Height);
                                }
                                var tnode = tgnode.Nodes.Add(hash.ToString().Trim() + ": " + tstr);
                                tnode.Tag = tex;
                            }
                        }
                        tgnode.Expand();
                    }

                }

                mnode.Expand();
                tmnode.Expand();
            }
        }
        private void UpdateSelectionDrawFlags(TreeNode node)
        {
            //update the selection draw flags depending on tag and checked/unchecked
            var drwbl = node.Tag as DrawableBase;
            var model = node.Tag as DrawableModel;
            var geom = node.Tag as DrawableGeometry;
            bool rem = node.Checked;
            lock (Renderer.RenderSyncRoot)
            {
                if (drwbl != null)
                {
                    if (rem)
                    {
                        if (Renderer.SelectionDrawableDrawFlags.ContainsKey(drwbl))
                        {
                            Renderer.SelectionDrawableDrawFlags.Remove(drwbl);
                        }
                    }
                    else
                    {
                        Renderer.SelectionDrawableDrawFlags[drwbl] = false;
                    }
                }
                if (model != null)
                {
                    if (rem)
                    {
                        if (Renderer.SelectionModelDrawFlags.ContainsKey(model))
                        {
                            Renderer.SelectionModelDrawFlags.Remove(model);
                        }
                    }
                    else
                    {
                        Renderer.SelectionModelDrawFlags[model] = false;
                    }
                }
                if (geom != null)
                {
                    if (rem)
                    {
                        if (Renderer.SelectionGeometryDrawFlags.ContainsKey(geom))
                        {
                            Renderer.SelectionGeometryDrawFlags.Remove(geom);
                        }
                    }
                    else
                    {
                        Renderer.SelectionGeometryDrawFlags[geom] = false;
                    }
                }
                //updateArchetypeStatus = true;
            }
        }

        private void UpdateGlobalPedsUI()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => { UpdateGlobalPedsUI(); }));
            }
            else
            {

                var index = PedModel == "mp_f_freemode_01" ? 0 : 1;

                PedNameComboBox.Items.Add("mp_f_freemode_01");
                PedNameComboBox.Items.Add("mp_m_freemode_01");
                PedNameComboBox.SelectedIndex = index;
            }

        }

        private void UpdateModelsUI()
        {
            Renderer.SelectionDrawableDrawFlags.Clear();
            Renderer.SelectionModelDrawFlags.Clear();
            Renderer.SelectionGeometryDrawFlags.Clear();
            ModelsTreeView.Nodes.Clear();
            ModelsTreeView.ShowRootLines = true;
            TexturesTreeView.Nodes.Clear();
            TexturesTreeView.ShowRootLines = true;

            if (SelectedPed == null) return;

            // The following could be combined into a single for loop with a conditional check inside
            if (!renderOnlySelected)
            {
                for (int i = 0; i < 12; i++)
                {
                    var drawable = SelectedPed.Drawables[i];
                    var drawablename = SelectedPed.DrawableNames[i];

                    if (drawable != null)
                    {
                        AddDrawableTreeNode(drawable, drawablename, true);
                    }
                }
            }
            else
            {
                // Disabling rendering for all drawables.
                // Code below this for loop will override this flag for the drawable(s) we want to render.
                for (int i = 0; i < 12; i++)
                {
                    var drawable = SelectedPed.Drawables[i];

                    if (drawable != null)
                    {
                        Renderer.SelectionDrawableDrawFlags[drawable] = false;
                    }
                }
            }

            foreach (var drawable in LoadedDrawables.Values)
            {
                AddDrawableTreeNode(drawable, $"Selected ({drawable.Name})", true);
            }

            if (SavedDrawables.Count > 0)
            {
                foreach (var drawable in SavedDrawables.Values)
                {
                    if (LoadedDrawables.Values.Contains(drawable)) continue;
                    AddDrawableTreeNode(drawable, $"Saved ({drawable.Name})", true);
                }
            }
        }

        public void LoadAnimsForModel(string pedname)
        {
            if(pedname == "mp_m_freemode_01")
            {
                ClipDictComboBox.Text = "move_m@generic";
            } 
            else if(pedname == "mp_f_freemode_01")
            {
                ClipDictComboBox.Text = "move_f@generic";
            }

            ClipComboBox.Text = "idle";
            LoadClipDict(ClipDictComboBox.Text);
            SelectClip("idle");
        }

        public void LoadPed(string pedname = "")
        {
            if (string.IsNullOrEmpty(pedname))
            {
                pedname = PedNameComboBox.Text;
            }
            PedNameComboBox.Text = pedname;

            var pedhash = JenkHash.GenHash(pedname.ToLowerInvariant());
            var pedchange = SelectedPed.NameHash != pedhash;

            SelectedPed.Init(pedname, GameFileCache);

            LoadModel(SelectedPed.Yft, pedchange);
            if(EnableAnimationCheckBox.Checked)
            {
                LoadAnimsForModel(pedname);
            }
            
            var vi = SelectedPed.Ymt?.VariationInfo;
            if (vi != null)
            {
                for (int i = 0; i < 12; i++)
                {
                    PopulateCompCombo(ComponentComboBoxes.ElementAt(i), vi.GetComponentData(i));
                }
            }

            head_updown.Maximum = ComponentComboBoxes[0].Count - 1;
            berd_updown.Maximum = ComponentComboBoxes[1].Count - 1;
            hair_updown.Maximum = ComponentComboBoxes[2].Count - 1;
            uppr_updown.Maximum = ComponentComboBoxes[3].Count - 1;
            lowr_updown.Maximum = ComponentComboBoxes[4].Count - 1;
            feet_updown.Maximum = ComponentComboBoxes[6].Count - 1;

            var index = PedNameComboBox.SelectedIndex;
            head_updown.Value = GetCompSettingsValue("HeadComp", index);
            berd_updown.Value = GetCompSettingsValue("BerdComp", index);
            hair_updown.Value = GetCompSettingsValue("HairComp", index);
            uppr_updown.Value = GetCompSettingsValue("UpprComp", index);
            lowr_updown.Value = GetCompSettingsValue("LowrComp", index);
            feet_updown.Value = GetCompSettingsValue("FeetComp", index);

            SetComponentDrawable(0, (int)head_updown.Value);
            SetComponentDrawable(1, (int)berd_updown.Value);
            SetComponentDrawable(2, (int)hair_updown.Value);
            SetComponentDrawable(3, (int)uppr_updown.Value);
            SetComponentDrawable(4, (int)lowr_updown.Value);
            SetComponentDrawable(6, (int)feet_updown.Value);


            UpdateModelsUI();
        }
       
        public int GetCompSettingsValue(string name, int index)
        {

            string[] settings = (Settings.Default[name] as string).Split(';');
            if (settings != null)
            {
                return Convert.ToInt32(settings[index]);
            }
            return 0;
        }

        public void UpdateSelectedDrawable(Drawable d, TextureDictionary t, Dictionary<string, string> updates)
        {
            Renderer.SelDrawable = d;
            Renderer.SelectedDrawableChanged = true;

            foreach (var update in updates)
            {
                var value = update.Value.ToString().ToLower();
                switch (update.Key)
                {
                    case "EnableKeepPreview":
                        var v = bool.Parse(value);
                        //if loadeddrawables already contains drawable, remove it
                        if (v == true && !SavedDrawables.ContainsKey(d.Name))
                        {
                            SavedDrawables.Add(d.Name, d);
                            SavedTextures.Add(d, t);
                            UpdateModelsUI();
                        }
                        else if (v == false && SavedDrawables.ContainsKey(d.Name))
                        {
                            SavedDrawables.Remove(d.Name);
                            SavedTextures.Remove(d);
                            UpdateModelsUI();
                        }
                        break;
                    case "EnableHairScale":
                        Renderer.SelDrawable.IsHairScaleEnabled = bool.Parse(value);
                        break;
                    case "HairScaleValue":
                        Renderer.SelDrawable.HairScaleValue = Convert.ToSingle(value);
                        break;
                    case "EnableHighHeels":
                        Renderer.SelDrawable.IsHighHeelsEnabled = bool.Parse(value);
                        break;
                    case "HighHeelsValue":
                        Renderer.SelDrawable.HighHeelsValue = Convert.ToSingle(value) / 10;
                        highheelvaluechanged = true;
                        break;
                    case "GenderChanged":
                        LoadPed(PedModel);
                        break;
                    default:
                        break;
                }
            }
        }

        public void LoadModel(YftFile yft, bool movecamera = true)
        {
            if (yft == null) return;

            //FileName = yft.Name;
            //Yft = yft;

            var dr = yft.Fragment?.Drawable;
            if (movecamera && (dr != null))
            {
                MoveCameraToView(dr.BoundingCenter, dr.BoundingSphereRadius);
            }

            //UpdateModelsUI(yft.Fragment.Drawable);
        }

        private void PopulateCompCombo(List<ComponentComboItem> c, MCPVComponentData compData)
        {
            if (compData?.DrawblData3 == null) return;
            foreach (var item in compData.DrawblData3)
            {
                c.Add(new ComponentComboItem(item));
            }
        }

        private void SetComponentDrawable(int compIndex, int itemIndex)
        {
            var s = ComponentComboBoxes[compIndex][itemIndex];

            SelectedPed.SetComponentDrawable(compIndex, s.DrawableName, s.TextureName, GameFileCache);

            UpdateModelsUI();
        }

        private void LoadClipDict(string name)
        {
            var ycdhash = JenkHash.GenHash(name.ToLowerInvariant());
            var ycd = GameFileCache.GetYcd(ycdhash);
            while ((ycd != null) && (!ycd.Loaded))
            {
                Thread.Sleep(1);//kinda hacky
                ycd = GameFileCache.GetYcd(ycdhash);
            }

            SelectedPed.Ycd = ycd;

            ClipComboBox.Items.Clear();
            ClipComboBox.Items.Add("");

            if (ycd?.ClipMapEntries == null)
            {
                ClipComboBox.SelectedIndex = 0;
                SelectedPed.AnimClip = null;
                return;
            }

            List<string> items = new List<string>();
            foreach (var cme in ycd.ClipMapEntries)
            {
                if (cme.Clip != null)
                {
                    items.Add(cme.Clip.ShortName);
                }
            }

            items.Sort();
            foreach (var item in items)
            {
                ClipComboBox.Items.Add(item);
            }
        }

        private void SelectClip(string name)
        {
            MetaHash cliphash = JenkHash.GenHash(name);
            ClipMapEntry cme = null;
            SelectedPed.Ycd?.ClipMap?.TryGetValue(cliphash, out cme);
            SelectedPed.AnimClip = cme;

            PlaybackSpeedTrackBar.Value = 60;
            UpdatePlaybackSpeedLabel();
        }
        private void UpdateTimeOfDayLabel()
        {
            int v = TimeOfDayTrackBar.Value;
            float fh = v / 60.0f;
            int ih = (int)fh;
            int im = v - (ih * 60);
            if (ih == 24) ih = 0;
            TimeOfDayLabel.Text = string.Format("{0:00}:{1:00}", ih, im);
        }

        private void UpdateControlInputs(float elapsed)
        {
            if (elapsed > 0.1f) elapsed = 0.1f;

            var s = Settings.Default;

            float moveSpeed = 2.0f;


            Input.Update();

            if (Input.ShiftPressed)
            {
                moveSpeed *= 5.0f;
            }
            if (Input.CtrlPressed)
            {
                moveSpeed *= 0.2f;
            }

            Vector3 movevec = Input.KeyboardMoveVec(false);

            if (Input.xbenable)
            {
                movevec.X += Input.xblx;
                movevec.Z -= Input.xbly;
                moveSpeed *= (1.0f + (Math.Min(Math.Max(Input.xblt, 0.0f), 1.0f) * 15.0f)); //boost with left trigger
                if (Input.ControllerButtonPressed(GamepadButtonFlags.A | GamepadButtonFlags.RightShoulder | GamepadButtonFlags.LeftShoulder))
                {
                    moveSpeed *= 5.0f;
                }
            }
            {
                //normal movement
                movevec *= elapsed * moveSpeed * Math.Min(camera.TargetDistance, 50.0f);
            }


            Vector3 movewvec = camera.ViewInvQuaternion.Multiply(movevec);
            camEntity.Position += movewvec;

            if (Input.xbenable)
            {
                camera.ControllerRotate(Input.xbrx, Input.xbry, elapsed);

                float zoom = 0.0f;
                float zoomspd = s.XInputZoomSpeed;
                float zoomamt = zoomspd * elapsed;
                if (Input.ControllerButtonPressed(GamepadButtonFlags.DPadUp)) zoom += zoomamt;
                if (Input.ControllerButtonPressed(GamepadButtonFlags.DPadDown)) zoom -= zoomamt;

                camera.ControllerZoom(zoom);
            }
        }

        private void PedsForm_Load(object sender, EventArgs e)
        {
            Init();
        }

        private void PedsForm_MouseDown(object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.Left: MouseLButtonDown = true; break;
                case MouseButtons.Right: MouseRButtonDown = true; break;
            }

            if (!ToolsPanelShowButton.Focused)
            {
                ToolsPanelShowButton.Focus(); //make sure no textboxes etc are focused!
            }

            MouseDownPoint = e.Location;
            MouseLastPoint = MouseDownPoint;

            if (MouseLButtonDown)
            {
            }

            if (MouseRButtonDown)
            {
                //SelectMousedItem();
            }

            MouseX = e.X; //to stop jumps happening on mousedown, sometimes the last MouseMove event was somewhere else... (eg after clicked a menu)
            MouseY = e.Y;
        }

        private void PedsForm_MouseUp(object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {
                case MouseButtons.Left: MouseLButtonDown = false; break;
                case MouseButtons.Right: MouseRButtonDown = false; break;
            }

        }

        private void PedsForm_MouseMove(object sender, MouseEventArgs e)
        {
            int dx = e.X - MouseX;
            int dy = e.Y - MouseY;

            {
                if (MouseLButtonDown)
                {
                    RotateCam(dx, dy);
                }
                if (MouseRButtonDown)
                {
                    if (Renderer.controllightdir)
                    {
                        Renderer.lightdirx += (dx * camera.Sensitivity);
                        Renderer.lightdiry += (dy * camera.Sensitivity);
                    }
                    else if (Renderer.controltimeofday)
                    {
                        float tod = Renderer.timeofday;
                        tod += (dx - dy) / 30.0f;
                        while (tod >= 24.0f) tod -= 24.0f;
                        while (tod < 0.0f) tod += 24.0f;
                        timecycle.SetTime(tod);
                        Renderer.timeofday = tod;

                        float fv = tod * 60.0f;
                        TimeOfDayTrackBar.Value = (int)fv;
                        UpdateTimeOfDayLabel();
                    }
                }

                UpdateMousePosition(e);
            }
        }

        private void PedsForm_MouseWheel(object sender, MouseEventArgs e)
        {
            if (e.Delta != 0)
            {
                camera.MouseZoom(e.Delta);
            }
        }

        private void PedsForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (ActiveControl is TextBox)
            {
                var tb = ActiveControl as TextBox;
                if (!tb.ReadOnly) return; //don't move the camera when typing!
            }
            if (ActiveControl is ComboBox)
            {
                var cb = ActiveControl as ComboBox;
                if (cb.DropDownStyle != ComboBoxStyle.DropDownList) return; //nontypable combobox
            }

            bool enablemove = true;// (!iseditmode) || (MouseLButtonDown && (GrabbedMarker == null) && (GrabbedWidget == null));

            Input.KeyDown(e, enablemove);

            var k = e.KeyCode;
            var kb = Input.keyBindings;
            bool ctrl = Input.CtrlPressed;
            bool shift = Input.ShiftPressed;


            if (!ctrl)
            {
                if (k == kb.MoveSlowerZoomIn)
                {
                    camera.MouseZoom(1);
                }
                if (k == kb.MoveFasterZoomOut)
                {
                    camera.MouseZoom(-1);
                }
            }
        }

        private void PedsForm_KeyUp(object sender, KeyEventArgs e)
        {
            Input.KeyUp(e);

            if (ActiveControl is TextBox)
            {
                var tb = ActiveControl as TextBox;
                if (!tb.ReadOnly) return; //don't move the camera when typing!
            }
            if (ActiveControl is ComboBox)
            {
                var cb = ActiveControl as ComboBox;
                if (cb.DropDownStyle != ComboBoxStyle.DropDownList) return; //non-typable combobox
            }
        }

        private void PedsForm_Deactivate(object sender, EventArgs e)
        {
            //try not to lock keyboard movement if the form loses focus.
            Input.KeyboardStop();
        }

        private void StatsUpdateTimer_Tick(object sender, EventArgs e)
        {
            StatsLabel.Text = Renderer.GetStatusText();

            if (Renderer.timerunning)
            {
                float fv = Renderer.timeofday * 60.0f;
                //TimeOfDayTrackBar.Value = (int)fv;
                UpdateTimeOfDayLabel();
            }
        }

        private void ToolsPanelShowButton_Click(object sender, EventArgs e)
        {
            ToolsPanel.Visible = true;
        }

        private void ToolsPanelHideButton_Click(object sender, EventArgs e)
        {
            ToolsPanel.Visible = false;
        }

        private void ToolsDragPanel_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                toolsPanelResizing = true;
                toolsPanelResizeStartX = e.X + ToolsPanel.Left + ToolsDragPanel.Left;
                toolsPanelResizeStartLeft = ToolsPanel.Left;
                toolsPanelResizeStartRight = ToolsPanel.Right;
            }
        }

        private void ToolsDragPanel_MouseUp(object sender, MouseEventArgs e)
        {
            toolsPanelResizing = false;
        }

        private void ToolsDragPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (toolsPanelResizing)
            {
                int rx = e.X + ToolsPanel.Left + ToolsDragPanel.Left;
                int dx = rx - toolsPanelResizeStartX;
                ToolsPanel.Width = toolsPanelResizeStartRight - toolsPanelResizeStartLeft + dx;
            }
        }

        private void ModelsTreeView_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (e.Node != null)
            {
                UpdateSelectionDrawFlags(e.Node);
            }
        }

        private void ModelsTreeView_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node != null)
            {
                e.Node.Checked = !e.Node.Checked;
                //UpdateSelectionDrawFlags(e.Node);
            }
        }

        private void ModelsTreeView_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = true; //stops annoying ding sound...
        }

        private void ShadowsCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            lock (Renderer.RenderSyncRoot)
            {
                Renderer.shaders.shadows = ShadowsCheckBox.Checked;
            }
        }

        private void ControlLightDirCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            Renderer.controllightdir = ControlLightDirCheckBox.Checked;
        }

        private void TimeOfDayTrackBar_Scroll(object sender, EventArgs e)
        {
            int v = TimeOfDayTrackBar.Value;
            float fh = v / 60.0f;
            UpdateTimeOfDayLabel();
            lock (Renderer.RenderSyncRoot)
            {
                Renderer.timeofday = fh;
                timecycle.SetTime(Renderer.timeofday);
            }
        }

        private void WireframeCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            Renderer.shaders.wireframe = WireframeCheckBox.Checked;
        }

        private void RenderModeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            TextureSamplerComboBox.Enabled = false;
            TextureCoordsComboBox.Enabled = false;
            switch (RenderModeComboBox.Text)
            {
                default:
                case "Default":
                    Renderer.shaders.RenderMode = WorldRenderMode.Default;
                    break;
                case "Single texture":
                    Renderer.shaders.RenderMode = WorldRenderMode.SingleTexture;
                    TextureSamplerComboBox.Enabled = true;
                    TextureCoordsComboBox.Enabled = true;
                    break;
                case "Vertex normals":
                    Renderer.shaders.RenderMode = WorldRenderMode.VertexNormals;
                    break;
                case "Vertex tangents":
                    Renderer.shaders.RenderMode = WorldRenderMode.VertexTangents;
                    break;
                case "Vertex colour 1":
                    Renderer.shaders.RenderMode = WorldRenderMode.VertexColour;
                    Renderer.shaders.RenderVertexColourIndex = 1;
                    break;
                case "Vertex colour 2":
                    Renderer.shaders.RenderMode = WorldRenderMode.VertexColour;
                    Renderer.shaders.RenderVertexColourIndex = 2;
                    break;
                case "Vertex colour 3":
                    Renderer.shaders.RenderMode = WorldRenderMode.VertexColour;
                    Renderer.shaders.RenderVertexColourIndex = 3;
                    break;
                case "Texture coord 1":
                    Renderer.shaders.RenderMode = WorldRenderMode.TextureCoord;
                    Renderer.shaders.RenderTextureCoordIndex = 1;
                    break;
                case "Texture coord 2":
                    Renderer.shaders.RenderMode = WorldRenderMode.TextureCoord;
                    Renderer.shaders.RenderTextureCoordIndex = 2;
                    break;
                case "Texture coord 3":
                    Renderer.shaders.RenderMode = WorldRenderMode.TextureCoord;
                    Renderer.shaders.RenderTextureCoordIndex = 3;
                    break;
            }
        }

        private void TextureSamplerComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (TextureSamplerComboBox.SelectedItem is ShaderParamNames)
            {
                Renderer.shaders.RenderTextureSampler = (ShaderParamNames)TextureSamplerComboBox.SelectedItem;
            }
        }

        private void TextureCoordsComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (TextureCoordsComboBox.Text)
            {
                default:
                case "Texture coord 1":
                    Renderer.shaders.RenderTextureSamplerCoord = 1;
                    break;
                case "Texture coord 2":
                    Renderer.shaders.RenderTextureSamplerCoord = 2;
                    break;
                case "Texture coord 3":
                    Renderer.shaders.RenderTextureSamplerCoord = 3;
                    break;
            }
        }

        private void SkeletonsCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            Renderer.renderskeletons = SkeletonsCheckBox.Checked;
        }

        private void StatusBarCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            StatusStrip.Visible = StatusBarCheckBox.Checked;
        }

        private void ErrorConsoleCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            ConsolePanel.Visible = ErrorConsoleCheckBox.Checked;
        }

        private void PedNameComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!GameFileCache.IsInited) return;

            LoadPed();
        }

        private void ClipDictComboBox_TextChanged(object sender, EventArgs e)
        {
            LoadClipDict(ClipDictComboBox.Text);
        }

        private void ClipComboBox_TextChanged(object sender, EventArgs e)
        {
            SelectClip(ClipComboBox.Text);
        }

        private void EnableRootMotionCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            SelectedPed.EnableRootMotion = EnableRootMotionCheckBox.Checked;
        }

        private void HDRRenderingCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            lock (Renderer.RenderSyncRoot)
            {
                Renderer.shaders.hdr = HDRRenderingCheckBox.Checked;
            }
        }

        private void EnableAnimationCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            ClipComboBox.Enabled = EnableAnimationCheckBox.Checked;
            ClipDictComboBox.Enabled = EnableAnimationCheckBox.Checked;
            EnableRootMotionCheckBox.Enabled = EnableAnimationCheckBox.Checked;
            PlaybackSpeedTrackBar.Enabled = EnableAnimationCheckBox.Checked;

            if (EnableAnimationCheckBox.Checked)
            {
                LoadAnimsForModel(SelectedPed.Name);
            } 
            else
            {
                ClipDictComboBox.Text = "";
                ClipComboBox.Text = "";
            }
        }

        private void PlaybackSpeedTrackBar_Scroll(object sender, EventArgs e)
        {
            int v = PlaybackSpeedTrackBar.Value;
            float fh = v / 60.0f;
            UpdatePlaybackSpeedLabel();

            lock (Renderer.RenderSyncRoot)
            {
                SelectedPed.AnimClip.PlaybackSpeed = fh;
            }
        }

        private void UpdatePlaybackSpeedLabel()
        {
            int v = PlaybackSpeedTrackBar.Value;
            float fh = v / 60.0f;
            PlaybackSpeedLabel.Text = string.Format("{0:0.00}", fh);
        }

        private void OptionsComponent_UpDown_ValueChanged(object sender, EventArgs e)
        {
            var compId = Convert.ToInt32(((NumericUpDown)sender).Tag);
            var value = Convert.ToInt32(((NumericUpDown)sender).Value);

            SetComponentDrawable(compId, value);
        }

        private void Save_defaultComp_Click(object sender, EventArgs e)
        {
            //ugly as shit but yeah it works

            int index = PedNameComboBox.SelectedIndex;

            var head = Settings.Default.HeadComp.Split(';');
            head[index] = head_updown.Value.ToString();
            Settings.Default.HeadComp = string.Join(";", head);

            var berd = Settings.Default.BerdComp.Split(';');
            berd[index] = berd_updown.Value.ToString();
            Settings.Default.BerdComp = string.Join(";", berd);

            var hair = Settings.Default.HairComp.Split(';');
            hair[index] = hair_updown.Value.ToString();
            Settings.Default.HairComp = string.Join(";", hair);

            var uppr = Settings.Default.UpprComp.Split(';');
            uppr[index] = uppr_updown.Value.ToString();
            Settings.Default.UpprComp = string.Join(";", uppr);

            var lowr = Settings.Default.LowrComp.Split(';');
            lowr[index] = lowr_updown.Value.ToString();
            Settings.Default.LowrComp = string.Join(";", lowr);

            var feet = Settings.Default.FeetComp.Split(';');
            feet[index] = feet_updown.Value.ToString();
            Settings.Default.FeetComp = string.Join(";", feet);



            Settings.Default.Save();
        }

        private void LiveTexturePreview_Click(object sender, EventArgs e)
        {
            if (liveTexturePath != null)
            {
                liveTexturePath = null;
                Renderer.LiveTextureEnabled = false;
            }
            else
            {
                using (var fbd = new FolderBrowserDialog())
                {
                    if (fbd.ShowDialog() == DialogResult.OK)
                    {
                        liveTexturePath = fbd.SelectedPath;
                        Renderer.LiveTextureEnabled = true;
                    }
                }
            }

            liveTxtButton.Text = Renderer.LiveTextureEnabled ? "Disable" : "Enable";
            diffuseRadio.Enabled = !Renderer.LiveTextureEnabled;
            normalRadio.Enabled = !Renderer.LiveTextureEnabled;
            specularRadio.Enabled = !Renderer.LiveTextureEnabled;
        }

        private void liveTexture_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton radioButton = sender as RadioButton;
            if (radioButton.Checked == true)
            {
                switch (radioButton.Text)
                {
                    case "Diffuse":
                        Renderer.LiveTextureSelectedMode = LiveTextureMode.Diffuse;
                        break;
                    case "Normal":
                        Renderer.LiveTextureSelectedMode = LiveTextureMode.Normal;
                        break;
                    case "Specular":
                        Renderer.LiveTextureSelectedMode = LiveTextureMode.Specular;
                        break;
                }
            }
        }

        private void FloorCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            floorUpDown.Enabled = floorCheckbox.Checked;
        }

        private void FloorUpDown_ValueChanged(object sender, EventArgs e)
        {
            var value = Convert.ToInt32(((NumericUpDown)sender).Value);

        }

        private void AutoRotateTimer_Tick(object sender, EventArgs e)
        {
            if (SelectedPed != null)
            {
                autoRotateAngle += AutoRotateSpeed * (float)Math.PI / 180f;
                if (autoRotateAngle > 2 * Math.PI) // Keep the autoRotateAngle within the 0 to 360
                {
                    autoRotateAngle -= 2 * (float)Math.PI;
                }

                Quaternion newRotation = Quaternion.RotationYawPitchRoll(0, 0, autoRotateAngle);
                SelectedPed.Rotation = newRotation;
                SelectedPed.UpdateEntity();
            }
        }

        private void AutoRotatePedCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (AutoRotatePedCheckBox.Checked)
            {
                autoRotateTimer.Start();
            }
            else
            {
                autoRotateTimer.Stop();
            }
        }

        private void OnlySelectedCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (OnlySelectedCheckBox.Checked == renderOnlySelected) { return; }
            renderOnlySelected = OnlySelectedCheckBox.Checked;
            
            UpdateModelsUI();
        }

        private void SetDefaultCameraPosition()
        {
            camera.FollowEntity = camEntity;
            camera.FollowEntity.Position = Vector3.Zero;// prevworldpos;

            // used to be Vector3.ForwardLH, but default animations rotates ped, so changed it to Vector3.ForwardRH 
            camera.FollowEntity.Orientation = Quaternion.LookAtLH(Vector3.Zero, Vector3.Up, Vector3.ForwardRH); 

            camera.TargetDistance = 2.0f;
            camera.CurrentDistance = 2.0f;
            camera.TargetRotation.Y = 0.2f;
            camera.CurrentRotation.Y = 0.2f;
            camera.TargetRotation.X = 1.0f * (float)Math.PI;
            camera.CurrentRotation.X = 1.0f * (float)Math.PI;

            if (SelectedPed != null)
            {
                // restart rotation angle, so ped faces camera
                autoRotateAngle = 0f;
                SelectedPed.Rotation = Quaternion.Identity;
                SelectedPed.UpdateEntity();
            }
        }

        private void RestartCamera_Click(object sender, EventArgs e)
        {
            SetDefaultCameraPosition();
        }
    }
}
