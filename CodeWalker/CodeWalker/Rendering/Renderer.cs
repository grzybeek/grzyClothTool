using CodeWalker.GameFiles;
using CodeWalker.Properties;
using CodeWalker.World;
using SharpDX;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeWalker.Rendering
{
    public class Renderer
    {
        private DXForm Form;
        private GameFileCache gameFileCache;
        private RenderableCache renderableCache;
        public RenderableCache RenderableCache { get { return renderableCache; } }

        private DXManager dxman = new DXManager();
        public DXManager DXMan { get { return dxman; } }
        private Device currentdevice;
        public Device Device { get { return currentdevice; } }
        private object rendersyncroot = new object();
        public object RenderSyncRoot { get { return rendersyncroot; } }

        public ShaderManager shaders;

        public Camera camera;

        private double currentRealTime = 0;
        private float currentElapsedTime = 0;
        private int framecount = 0;
        private float fcelapsed = 0.0f;
        private int fps = 0;

        private DeviceContext context;


        public float timeofday = 12.0f;
        public bool controltimeofday = true;
        public bool timerunning = false;
        public float timespeed = 0.5f;//min/sec
        public string weathertype = "";
        public string individualcloudfrag = "contrails";

        private Vector4 currentWindVec = Vector4.Zero;
        private float currentWindTime = 0.0f;


        public bool usedynamiclod = Settings.Default.DynamicLOD; //for ymap view
        public float lodthreshold = 50.0f / (0.1f + (float)Settings.Default.DetailDist); //to match formula for the DetailTrackBar value
        public bool waitforchildrentoload = true;


        public bool controllightdir = false; //if not, use timecycle
        public float lightdirx = 2.25f;//radians // approx. light dir on map satellite view
        public float lightdiry = 0.65f;//radians  - used for manual light placement
        public bool renderskydome = Settings.Default.Skydome;
        public bool renderclouds = true;
        public bool rendermoon = true;


        public Timecycle timecycle = new Timecycle();
        public Weather weather = new Weather();
        public Clouds clouds = new Clouds();



        private ShaderGlobalLights globalLights = new ShaderGlobalLights();
        public bool rendernaturalambientlight = true;
        public bool renderartificialambientlight = true;

        public bool renderfloor = false;
        public bool SelectedDrawableChanged = true;

        public bool usehighheels = false;
        public bool usehighheelschanged = false;

        public LiveTextureMode LiveTextureSelectedMode = LiveTextureMode.Diffuse;
        public bool LiveTextureEnabled = false;
        private Texture savedTexture = null;

        public bool MapViewEnabled = false;
        public float MapViewDetail = 1.0f;


        private UnitQuad markerquad = null;
        public bool markerdepthclip = Settings.Default.MarkerDepthClip;

        private List<YmapEntityDef> renderworldentities = new List<YmapEntityDef>(); //used when rendering world view.

        public Dictionary<uint, YmapEntityDef> HideEntities = new Dictionary<uint, YmapEntityDef>();//dictionary of entities to hide, for cutscenes to use 

        public bool ShowScriptedYmaps = true;
        public List<YmapFile> VisibleYmaps = new List<YmapFile>();
        public List<YmapEntityDef> VisibleMlos = new List<YmapEntityDef>();

        public rage__eLodType renderworldMaxLOD = rage__eLodType.LODTYPES_DEPTH_ORPHANHD;
        public float renderworldLodDistMult = 1.0f;
        public float renderworldDetailDistMult = 1.0f;

        public bool rendertimedents = Settings.Default.ShowTimedEntities;
        public bool rendertimedentsalways = false;
        public bool renderinteriors = true;
        public bool renderproxies = false;
        public bool renderchildents = false;//when rendering single ymap, render root only or not...
        public bool renderentities = true;
        public bool rendergrass = true;
        public bool renderlights = true; //render individual drawable lights
        public bool renderlodlights = true; //render LOD lights from ymaps
        public bool renderdistlodlights = true; //render distant lod lights (coronas)
        public bool rendercars = false;
        public bool renderfragwindows = false; //render selection geometry for window glass data in fragments 

        public bool rendercollisionmeshes = Settings.Default.ShowCollisionMeshes;
        public bool rendercollisionmeshlayerdrawable = true;

        public bool renderskeletons = false;
        private List<RenderSkeletonItem> renderskeletonlist = new List<RenderSkeletonItem>();
        private List<VertexTypePC> skeletonLineVerts = new List<VertexTypePC>();

        public bool renderhdtextures = true;

        public bool swaphemisphere = false;//can be used to get better lighting in model viewers


        //public MapSelectionMode SelectionMode = MapSelectionMode.Entity; //to assist in rendering embedded collisions properly...


        public BoundsShaderMode boundsmode = BoundsShaderMode.None;
        public bool renderboundsclip = Settings.Default.BoundsDepthClip;
        public float renderboundsmaxrad = 20000.0f;
        public float renderboundsmaxdist = 10000.0f;
        public List<MapBox> BoundingBoxes = new List<MapBox>();
        public List<MapSphere> BoundingSpheres = new List<MapSphere>();
        public List<MapSphere> HilightSpheres = new List<MapSphere>();
        public List<MapBox> HilightBoxes = new List<MapBox>();
        public List<MapBox> SelectionBoxes = new List<MapBox>();
        public List<MapBox> WhiteBoxes = new List<MapBox>();
        public List<MapSphere> SelectionSpheres = new List<MapSphere>();
        public List<MapSphere> WhiteSpheres = new List<MapSphere>();
        public List<VertexTypePC> SelectionLineVerts = new List<VertexTypePC>();
        public List<VertexTypePC> SelectionTriVerts = new List<VertexTypePC>();

        public DrawableBase SelectedDrawable = null;
        public Drawable SelDrawable = null;
        public Drawable PreviousSelDrawable = null;
        public Dictionary<DrawableBase, bool> SelectionDrawableDrawFlags = new Dictionary<DrawableBase, bool>();
        public Dictionary<DrawableModel, bool> SelectionModelDrawFlags = new Dictionary<DrawableModel, bool>();
        public Dictionary<DrawableGeometry, bool> SelectionGeometryDrawFlags = new Dictionary<DrawableGeometry, bool>();
        public bool SelectionFlagsTestAll = false; //to test all renderables for draw flags; for model form


        public List<RenderedDrawable> RenderedDrawables = new List<RenderedDrawable>(); //queued here for later hit tests...
        public Dictionary<DrawableBase, RenderedDrawable> RenderedDrawablesDict = new Dictionary<DrawableBase, RenderedDrawable>();
        public List<RenderedBoundComposite> RenderedBoundComps = new List<RenderedBoundComposite>();
        public bool RenderedDrawablesListEnable = false; //whether or not to add rendered drawables to the list
        public bool RenderedBoundCompsListEnable = false; //whether or not to add rendered bound comps to the list


        private List<YtdFile> tryGetRenderableSDtxds = new List<YtdFile>();
        private List<YtdFile> tryGetRenderableHDtxds = new List<YtdFile>();



        public Renderer(DXForm form, GameFileCache cache)
        {
            var s = Settings.Default;
            Form = form;
            gameFileCache = cache;
            if (gameFileCache == null)
            {
                gameFileCache = new GameFileCache(s.CacheSize, s.CacheTime, GTAFolder.CurrentGTAFolder, s.DLC, s.EnableMods, s.ExcludeFolders);
            }
            renderableCache = new RenderableCache();

            
            camera = new Camera(s.CameraSmoothing, s.CameraSensitivity, s.CameraFieldOfView);
        }


        public bool Init()
        {
            return dxman.Init(Form, false);
        }

        public void Start()
        {
            dxman.Start();
        }

        public void DeviceCreated(Device device, int width, int height)
        {
            currentdevice = device;

            shaders = new ShaderManager(device, dxman);
            shaders.OnWindowResize(width, height); //init the buffers

            renderableCache.OnDeviceCreated(device);

            camera.OnWindowResize(width, height); //init the projection stuff


            markerquad = new UnitQuad(device);

        }

        public void DeviceDestroyed()
        {
            renderableCache.OnDeviceDestroyed();

            markerquad.Dispose();

            shaders.Dispose();

            currentdevice = null;
        }

        public void BuffersResized(int width, int height)
        {
            lock (rendersyncroot)
            {
                camera.OnWindowResize(width, height);
                shaders?.OnWindowResize(width, height);
            }
        }

        public void ReloadShaders()
        {
            if (shaders != null)
            {
                shaders.Dispose();
            }
            shaders = new ShaderManager(currentdevice, dxman);
        }


        public void Update(float elapsed, int mouseX, int mouseY)
        {
            framecount++;
            fcelapsed += elapsed;
            if (fcelapsed >= 0.5f)
            {
                fps = framecount * 2;
                framecount = 0;
                fcelapsed -= 0.5f;
            }
            if (elapsed > 0.1f) elapsed = 0.1f;
            currentRealTime += elapsed;
            currentElapsedTime = elapsed;



            UpdateTimeOfDay(elapsed);


            weather.Update(elapsed);

            clouds.Update(elapsed);


            UpdateWindVector(elapsed);

            UpdateGlobalLights();


            camera.SetMousePosition(mouseX, mouseY);

            camera.Update(elapsed);
        }


        public void BeginRender(DeviceContext ctx)
        {
            context = ctx;

            dxman.ClearRenderTarget(context);

            if (shaders == null) return;

            shaders.BeginFrame(context, currentRealTime, currentElapsedTime);

            shaders.EnsureShaderTextures(gameFileCache, renderableCache);




            SelectionLineVerts.Clear();
            SelectionTriVerts.Clear();
            WhiteBoxes.Clear();
            WhiteSpheres.Clear();
            SelectionBoxes.Clear();
            SelectionSpheres.Clear();
            HilightBoxes.Clear();
            HilightSpheres.Clear();
            BoundingBoxes.Clear();
            BoundingSpheres.Clear();

            RenderedDrawables.Clear();
            RenderedBoundComps.Clear();

            renderskeletonlist.Clear();

            HideEntities.Clear();
        }

        public void RenderSkyAndClouds()
        {
            RenderSky();

            RenderClouds();

            shaders.ClearDepth(context);
        }

        public void RenderQueued()
        {
            if (shaders == null) return;

            shaders.RenderQueued(context, camera, currentWindVec);

            RenderSkeletons();
            RenderSkeletons();
        }

        public void RenderFinalPass()
        {
            if (shaders == null) return;

            shaders.RenderFinalPass(context);
        }

        public void EndRender()
        {
            renderableCache.RenderThreadSync();
        }

        public bool ContentThreadProc()
        {
            bool rcItemsPending = renderableCache.ContentThreadProc();

            return rcItemsPending;
        }
        public string GetStatusText()
        {
            int rgc = (shaders != null) ? shaders.RenderedGeometries : 0;
            int crc = renderableCache.LoadedRenderableCount;
            int ctc = renderableCache.LoadedTextureCount;
            int tcrc = renderableCache.MemCachedRenderableCount;
            int tctc = renderableCache.MemCachedTextureCount;
            long vr = renderableCache.TotalGraphicsMemoryUse + (shaders != null ? shaders.TotalGraphicsMemoryUse : 0);
            string vram = TextUtil.GetBytesReadable(vr);
            //StatsLabel.Text = string.Format("Drawn: {0} geom, Loaded: {1}/{5} dr, {2}/{6} tx, Vram: {3}, Fps: {4}", rgc, crc, ctc, vram, fps, tcrc, tctc);
            return string.Format("Drawn: {0} geom, Loaded: {1} dr, {2} tx, Vram: {3}, Fps: {4}", rgc, crc, ctc, vram, fps);
        }

        private void UpdateTimeOfDay(float elapsed)
        {
            if (timerunning)
            {
                float helapsed = elapsed * timespeed / 60.0f;
                timeofday += helapsed;
                while (timeofday >= 24.0f) timeofday -= 24.0f;
                while (timeofday < 0.0f) timeofday += 24.0f;
                timecycle.SetTime(timeofday);
            }
        }

        private void UpdateGlobalLights()
        {
            Vector3 lightdir = Vector3.Zero;//will be updated before each frame from X and Y vars
            Color4 lightdircolour = Color4.White;
            Color4 lightdirambcolour = new Color4(0.5f, 0.5f, 0.5f, 1.0f);
            Color4 lightnaturalupcolour = new Color4(0.0f);
            Color4 lightnaturaldowncolour = new Color4(0.0f);
            Color4 lightartificialupcolour = new Color4(0.0f);
            Color4 lightartificialdowncolour = new Color4(0.0f);
            bool hdr = (shaders != null) ? shaders.hdr : false;
            float hdrint = 1.0f;
            Vector3 sundir = Vector3.Up;
            Vector3 moondir = Vector3.Down;
            Vector3 moonax = Vector3.UnitZ;

            if (controllightdir)
            {
                float cryd = (float)Math.Cos(lightdiry);
                lightdir.X = -(float)Math.Sin(-lightdirx) * cryd;
                lightdir.Y = -(float)Math.Cos(-lightdirx) * cryd;
                lightdir.Z = (float)Math.Sin(lightdiry);
                lightdircolour = Color4.White;
                lightdirambcolour = new Color4(0.5f, 0.5f, 0.5f, 1.0f);
                if (hdr && (weather != null) && (weather.Inited))
                {
                    lightdircolour *= weather.CurrentValues.skyHdr;
                    lightdircolour.Alpha = 1.0f;
                    lightdirambcolour *= weather.CurrentValues.skyHdr * 0.35f;
                    lightdirambcolour.Alpha = 1.0f;
                    hdrint = weather.CurrentValues.skyHdr;
                }
                sundir = lightdir;
                moondir = -lightdir;
            }
            else
            {
                float sunroll = timecycle.sun_roll * (float)Math.PI / 180.0f;  //122
                float moonroll = timecycle.moon_roll * (float)Math.PI / 180.0f;  //-122
                float moonwobamp = timecycle.moon_wobble_amp; //0.2
                float moonwobfreq = timecycle.moon_wobble_freq; //2
                float moonwoboffs = timecycle.moon_wobble_offset; //0.375
                float dayval = (0.5f + (timeofday - 6.0f) / 14.0f);
                float nightval = (((timeofday > 12.0f) ? (timeofday - 7.0f) : (timeofday + 17.0f)) / 9.0f);
                float daycyc = (float)Math.PI * dayval;
                float nightcyc = (float)Math.PI * nightval;
                Vector3 sdir = new Vector3((float)Math.Sin(daycyc), -(float)Math.Cos(daycyc), 0.0f);
                Vector3 mdir = new Vector3(-(float)Math.Sin(nightcyc), 0.0f, -(float)Math.Cos(nightcyc));
                Quaternion saxis = Quaternion.RotationYawPitchRoll(0.0f, sunroll, 0.0f);
                Quaternion maxis = Quaternion.RotationYawPitchRoll(0.0f, -moonroll, 0.0f);
                sundir = Vector3.Normalize(saxis.Multiply(sdir));
                moondir = Vector3.Normalize(maxis.Multiply(mdir));
                moonax = Vector3.Normalize(maxis.Multiply(Vector3.UnitY));
                //bool usemoon = false;

                if (swaphemisphere)
                {
                    sundir.Y = -sundir.Y;
                }

                lightdir = sundir;

                //if (lightdir.Z < -0.5f) lightdir.Z = -lightdir.Z; //make sure the lightsource is always above the horizon...

                if ((timeofday < 5.0f) || (timeofday > 21.0f))
                {
                    lightdir = moondir;
                    //usemoon = true;
                }

                if (lightdir.Z < 0)
                {
                    lightdir.Z = 0; //don't let the light source go below the horizon...
                }

                //lightdir = Vector3.Normalize(weather.CurrentValues.sunDirection);

                if ((weather != null) && weather.Inited)
                {
                    lightdircolour = (Color4)weather.CurrentValues.lightDirCol;
                    lightdirambcolour = (Color4)weather.CurrentValues.lightDirAmbCol;
                    lightnaturalupcolour = (Color4)weather.CurrentValues.lightNaturalAmbUp;
                    lightnaturaldowncolour = (Color4)weather.CurrentValues.lightNaturalAmbDown;
                    lightartificialupcolour = (Color4)weather.CurrentValues.lightArtificialExtUp;
                    lightartificialdowncolour = (Color4)weather.CurrentValues.lightArtificialExtDown;
                    float lamult = weather.CurrentValues.lightDirAmbIntensityMult;
                    float abounce = weather.CurrentValues.lightDirAmbBounce;
                    float minmult = hdr ? 0.0f : 0.5f;
                    lightdircolour *= Math.Max(lightdircolour.Alpha, minmult);
                    lightdirambcolour *= lightdirambcolour.Alpha * lamult; // 0.1f * lamult;

                    //if (usemoon)
                    //{
                    //    lightdircolour *= weather.CurrentValues.skyMoonIten;
                    //}


                    lightnaturalupcolour *= lightnaturalupcolour.Alpha * weather.CurrentValues.lightNaturalAmbUpIntensityMult;
                    lightnaturaldowncolour *= lightnaturaldowncolour.Alpha;
                    lightartificialupcolour *= lightartificialupcolour.Alpha;
                    lightartificialdowncolour *= lightartificialdowncolour.Alpha;

                    if (!hdr)
                    {
                        Color4 maxdirc = new Color4(1.0f);
                        Color4 maxambc = new Color4(0.5f);
                        lightdircolour = Color4.Min(lightdircolour, maxdirc);
                        lightdirambcolour = Color4.Min(lightdirambcolour, maxambc);
                        lightnaturalupcolour = Color4.Min(lightnaturalupcolour, maxambc);
                        lightnaturaldowncolour = Color4.Min(lightnaturaldowncolour, maxambc);
                        lightartificialupcolour = Color4.Min(lightartificialupcolour, maxambc);
                        lightartificialdowncolour = Color4.Min(lightartificialdowncolour, maxambc);
                    }
                    else
                    {
                        hdrint = weather.CurrentValues.skyHdr;//.lightDirCol.W;
                    }
                }


            }

            globalLights.Weather = weather;
            globalLights.HdrEnabled = hdr;
            globalLights.SpecularEnabled = !MapViewEnabled;//disable specular for map view.
            globalLights.HdrIntensity = Math.Max(hdrint, 1.0f);
            globalLights.CurrentSunDir = sundir;
            globalLights.CurrentMoonDir = moondir;
            globalLights.MoonAxis = moonax;
            globalLights.Params.LightDir = lightdir;
            globalLights.Params.LightDirColour = lightdircolour;
            globalLights.Params.LightDirAmbColour = lightdirambcolour;
            globalLights.Params.LightNaturalAmbUp = rendernaturalambientlight ? lightnaturalupcolour : Color4.Black;
            globalLights.Params.LightNaturalAmbDown = rendernaturalambientlight ? lightnaturaldowncolour : Color4.Black;
            globalLights.Params.LightArtificialAmbUp = renderartificialambientlight ? lightartificialupcolour : Color4.Black;
            globalLights.Params.LightArtificialAmbDown = renderartificialambientlight ? lightartificialdowncolour : Color4.Black;


            if (shaders != null)
            {
                shaders.SetGlobalLightParams(globalLights);
            }

        }

        private void UpdateWindVector(float elapsed)
        {
            //wind still needs a lot of work.
            //currently just feed the wind vector with small oscillations...
            currentWindTime += elapsed;
            if (currentWindTime >= 200.0f) currentWindTime -= 200.0f;

            float dirval = (float)(currentWindTime * 0.01 * Math.PI);
            float dirval1 = (float)Math.Sin(currentWindTime * 0.100 * Math.PI) * 0.3f;
            float dirval2 = (float)(currentWindTime * 0.333 * Math.PI);
            float dirval3 = (float)(currentWindTime * 0.5 * Math.PI);
            float dirval4 = (float)Math.Sin(currentWindTime * 0.223 * Math.PI) * 0.4f;
            float dirval5 = (float)Math.Sin(currentWindTime * 0.4 * Math.PI) * 5.5f;

            currentWindVec.Z = (float)Math.Sin(dirval) * dirval1 + (float)Math.Cos(dirval2) * dirval4;
            currentWindVec.W = (float)Math.Cos(dirval) * dirval5 + (float)Math.Sin(dirval3) * dirval4;

            float strval = (float)(currentWindTime * 0.1 * Math.PI);
            float strval2 = (float)(currentWindTime * 0.825 * Math.PI);
            float strval3 = (float)(currentWindTime * 0.333 * Math.PI);
            float strval4 = (float)(currentWindTime * 0.666 * Math.PI);
            float strbase = 0.1f * ((float)Math.Sin(strval * 0.5));
            float strbase2 = 0.02f * ((float)Math.Sin(strval2 * 0.1));

            currentWindVec.X = (float)Math.Sin(strval) * strbase + ((float)Math.Cos(strval3) * strbase2);
            currentWindVec.Y = (float)Math.Cos(strval2) * strbase + ((float)Math.Sin(strval4 - strval3) * strbase2);
        }


        public void RenderSelectionLine(Vector3 p1, Vector3 p2, uint col)
        {
            SelectionLineVerts.Add(new VertexTypePC() { Position = p1, Colour = col });
            SelectionLineVerts.Add(new VertexTypePC() { Position = p2, Colour = col });
        }

        public void RenderSelectionQuad(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, uint col)
        {
            var v1 = new VertexTypePC() { Position = p1, Colour = col };
            var v2 = new VertexTypePC() { Position = p2, Colour = col };
            var v3 = new VertexTypePC() { Position = p3, Colour = col };
            var v4 = new VertexTypePC() { Position = p4, Colour = col };
            SelectionTriVerts.Add(v1);
            SelectionTriVerts.Add(v2);
            SelectionTriVerts.Add(v3);
            SelectionTriVerts.Add(v3);
            SelectionTriVerts.Add(v4);
            SelectionTriVerts.Add(v1);
        }


       

        private void RenderSkeleton(Renderable renderable, YmapEntityDef entity)
        {
            RenderSkeletonItem item = new RenderSkeletonItem();
            item.Renderable = renderable;
            item.Entity = entity;
            renderskeletonlist.Add(item);
        }

        private void RenderSkeletons()
        {

            skeletonLineVerts.Clear();

            const uint cred = 4278190335;// (uint)new Color4(1.0f, 0.0f, 0.0f, 1.0f).ToRgba();
            const uint cgrn = 4278255360;// (uint)new Color4(0.0f, 1.0f, 0.0f, 1.0f).ToRgba();
            const uint cblu = 4294901760;// (uint)new Color4(0.0f, 0.0f, 1.0f, 1.0f).ToRgba();
            VertexTypePC vr = new VertexTypePC();
            VertexTypePC vg = new VertexTypePC();
            VertexTypePC vb = new VertexTypePC();
            vr.Colour = cred;
            vg.Colour = cgrn;
            vb.Colour = cblu;

            foreach (var item in renderskeletonlist)
            {
                YmapEntityDef entity = item.Entity;
                DrawableBase drawable = item.Renderable.Key;
                Skeleton skeleton = drawable?.Skeleton;
                if (skeleton == null) continue;

                Vector3 campos = camera.Position - (entity?.Position ?? Vector3.Zero);

                var pinds = skeleton.ParentIndices;
                var bones = skeleton.Bones?.Items;
                if ((pinds == null) || (bones == null)) continue;
                var xforms = skeleton.Transformations;

                int cnt = Math.Min(pinds.Length, bones.Length);
                for (int i = 0; i < cnt; i++)
                {
                    var pind = pinds[i];
                    var bone = bones[i];
                    var pbone = bone.Parent;

                    if (xforms != null)//how to use xforms? bind pose?
                    {
                        var xform = (i < xforms.Length) ? xforms[i] : Matrix.Identity;
                        var pxform = ((pind >= 0) && (pind < xforms.Length)) ? xforms[pind] : Matrix.Identity;
                    }
                    else
                    {
                    }


                    //draw line from bone's position to parent position...
                    Vector3 lbeg = Vector3.Zero;
                    Vector3 lend = bone.AnimTranslation;// bone.Rotation.Multiply();

                    float starsize = (bone.AnimTransform.TranslationVector-campos).Length() * 0.011f;
                    Vector3[] starverts0 = { Vector3.UnitX * starsize, Vector3.UnitY * starsize, Vector3.UnitZ * starsize };
                    Vector3[] starverts1 = { Vector3.UnitX * -starsize, Vector3.UnitY * -starsize, Vector3.UnitZ * -starsize };
                    for (int j = 0; j < 3; j++) starverts0[j] = bone.AnimTransform.MultiplyW(starverts0[j]);
                    for (int j = 0; j < 3; j++) starverts1[j] = bone.AnimTransform.MultiplyW(starverts1[j]);

                    if (pbone != null)
                    {
                        lbeg = pbone.AnimTransform.MultiplyW(lbeg);
                        lend = pbone.AnimTransform.MultiplyW(lend);
                    }

                    if (entity != null)
                    {
                        lbeg = entity.Position + entity.Orientation.Multiply(lbeg * entity.Scale);
                        lend = entity.Position + entity.Orientation.Multiply(lend * entity.Scale);

                        for (int j = 0; j < 3; j++) starverts0[j] = entity.Position + entity.Orientation.Multiply(starverts0[j] * entity.Scale);
                        for (int j = 0; j < 3; j++) starverts1[j] = entity.Position + entity.Orientation.Multiply(starverts1[j] * entity.Scale);
                    }

                    vr.Position = starverts0[0]; skeletonLineVerts.Add(vr);
                    vr.Position = starverts1[0]; skeletonLineVerts.Add(vr);
                    vg.Position = starverts0[1]; skeletonLineVerts.Add(vg);
                    vg.Position = starverts1[1]; skeletonLineVerts.Add(vg);
                    vb.Position = starverts0[2]; skeletonLineVerts.Add(vb);
                    vb.Position = starverts1[2]; skeletonLineVerts.Add(vb);


                    if (pbone != null) //don't draw the origin to root bone line
                    {
                        vg.Position = lbeg;
                        vb.Position = lend;
                        skeletonLineVerts.Add(vg);
                        skeletonLineVerts.Add(vb);
                    }
                }
            }

            if (skeletonLineVerts.Count > 0)
            {
                RenderLines(skeletonLineVerts, DepthStencilMode.DisableAll);
            }

        }



        public void RenderLines(List<VertexTypePC> linelist, DepthStencilMode dsmode = DepthStencilMode.Enabled)
        {
            if (shaders == null) return;

            shaders.SetDepthStencilMode(context, dsmode);
            shaders.Paths.RenderLines(context, linelist, camera, shaders.GlobalLights);
        }

        public void RenderTriangles(List<VertexTypePC> linelist, DepthStencilMode dsmode = DepthStencilMode.Enabled)
        {
            if (shaders == null) return;

            shaders.SetDepthStencilMode(context, dsmode);
            shaders.Paths.RenderTriangles(context, linelist, camera, shaders.GlobalLights);
        }

        private void RenderSky()
        {
            if (MapViewEnabled) return;
            if (!renderskydome) return;
            if (!weather.Inited) return;
            if (shaders == null) return;

            var shader = shaders.Skydome;
            shader.UpdateSkyLocals(weather, globalLights);

            DrawableBase skydomeydr = null;
            YddFile skydomeydd = gameFileCache.GetYdd(2640562617); //skydome hash
            if ((skydomeydd != null) && (skydomeydd.Loaded) && (skydomeydd.Dict != null))
            {
                skydomeydr = skydomeydd.Dict.Values.FirstOrDefault();
            }

            Texture starfield = null;
            Texture moon = null;
            YtdFile skydomeytd = gameFileCache.GetYtd(2640562617); //skydome hash
            if ((skydomeytd != null) && (skydomeytd.Loaded) && (skydomeytd.TextureDict != null) && (skydomeytd.TextureDict.Dict != null))
            {
                skydomeytd.TextureDict.Dict.TryGetValue(1064311147, out starfield); //starfield hash

                if (rendermoon)
                {
                    skydomeytd.TextureDict.Dict.TryGetValue(234339206, out moon); //moon-new hash
                }
            }

            Renderable sdrnd = null;
            if (skydomeydr != null)
            {
                sdrnd = renderableCache.GetRenderable(skydomeydr);
            }

            RenderableTexture sftex = null;
            if (starfield != null)
            {
                sftex = renderableCache.GetRenderableTexture(starfield);
            }

            RenderableTexture moontex = null;
            if (moon != null)
            {
                moontex = renderableCache.GetRenderableTexture(moon);
            }

            if ((sdrnd != null) && (sdrnd.IsLoaded) && (sftex != null) && (sftex.IsLoaded))
            {
                shaders.SetDepthStencilMode(context, DepthStencilMode.DisableAll);
                shaders.SetRasterizerMode(context, RasterizerMode.Solid);

                RenderableInst rinst = new RenderableInst();
                rinst.Position = Vector3.Zero;
                rinst.CamRel = Vector3.Zero;
                rinst.Distance = 0.0f;
                rinst.BBMin = skydomeydr.BoundingBoxMin;
                rinst.BBMax = skydomeydr.BoundingBoxMax;
                rinst.BSCenter = Vector3.Zero;
                rinst.Radius = skydomeydr.BoundingSphereRadius;
                rinst.Orientation = Quaternion.Identity;
                rinst.Scale = Vector3.One;
                rinst.TintPaletteIndex = 0;
                rinst.CastShadow = false;
                rinst.Renderable = sdrnd;
                shader.SetShader(context);
                shader.SetInputLayout(context, VertexType.PTT);
                shader.SetSceneVars(context, camera, null, globalLights);
                shader.SetEntityVars(context, ref rinst);

                RenderableModel rmod = ((sdrnd.HDModels != null) && (sdrnd.HDModels.Length > 0)) ? sdrnd.HDModels[0] : null;
                RenderableGeometry rgeom = ((rmod != null) && (rmod.Geometries != null) && (rmod.Geometries.Length > 0)) ? rmod.Geometries[0] : null;

                if ((rgeom != null) && (rgeom.VertexType == VertexType.PTT))
                {
                    shader.SetModelVars(context, rmod);
                    shader.SetTextures(context, sftex);

                    rgeom.Render(context);
                }

                //shaders.SetRasterizerMode(context, RasterizerMode.SolidDblSided);
                //shaders.SetDepthStencilMode(context, DepthStencilMode.Enabled);
                shader.RenderSun(context, camera, weather, globalLights);


                if ((rendermoon) && (moontex != null) && (moontex.IsLoaded))
                {
                    shader.RenderMoon(context, camera, weather, globalLights, moontex);
                }

                shader.UnbindResources(context);
            }
        }

        private void RenderClouds()
        {
            if (MapViewEnabled) return;
            if (!renderclouds) return;
            if (!renderskydome) return;
            if (!weather.Inited) return;
            if (!clouds.Inited) return;
            if (shaders == null) return;


            var shader = shaders.Clouds;

            shaders.SetDepthStencilMode(context, DepthStencilMode.DisableAll);
            shaders.SetRasterizerMode(context, RasterizerMode.Solid);
            shaders.SetDefaultBlendState(context);
            //shaders.SetAlphaBlendState(context);

            shader.SetShader(context);
            shader.UpdateCloudsLocals(clouds, globalLights);
            shader.SetSceneVars(context, camera, null, globalLights);

            var vtype = (VertexType)0;

            if (!string.IsNullOrEmpty(individualcloudfrag))
            {
                //render one cloud fragment.

                CloudHatFrag frag = clouds.HatManager.FindFrag(individualcloudfrag);
                if (frag == null) return;

                for (int i = 0; i < frag.Layers.Length; i++)
                {
                    CloudHatFragLayer layer = frag.Layers[i];
                    uint dhash = JenkHash.GenHash(layer.Filename.ToLowerInvariant());
                    Archetype arch = gameFileCache.GetArchetype(dhash);
                    if (arch == null)
                    { continue; }

                    if (Math.Max(camera.Position.Z, 0.0f) < layer.HeightTigger) continue;

                    var drw = gameFileCache.TryGetDrawable(arch);
                    var rnd = TryGetRenderable(arch, drw);

                    if ((rnd == null) || (rnd.IsLoaded == false) || (rnd.AllTexturesLoaded == false))
                    { continue; }


                    RenderableInst rinst = new RenderableInst();
                    rinst.Position = frag.Position;// Vector3.Zero;
                    rinst.CamRel = Vector3.Zero;// - camera.Position;
                    rinst.Distance = rinst.CamRel.Length();
                    rinst.BBMin = arch.BBMin;
                    rinst.BBMax = arch.BBMax;
                    rinst.BSCenter = frag.Position;
                    rinst.Radius = arch.BSRadius;
                    rinst.Orientation = Quaternion.Identity;
                    rinst.Scale = frag.Scale;// Vector3.One;
                    rinst.TintPaletteIndex = 0;
                    rinst.CastShadow = false;
                    rinst.Renderable = rnd;

                    shader.SetEntityVars(context, ref rinst);


                    for (int mi = 0; mi < rnd.HDModels.Length; mi++)
                    {
                        var model = rnd.HDModels[mi];

                        for (int gi = 0; gi < model.Geometries.Length; gi++)
                        {
                            var geom = model.Geometries[gi];

                            if (geom.VertexType != vtype)
                            {
                                vtype = geom.VertexType;
                                shader.SetInputLayout(context, vtype);
                            }

                            shader.SetGeomVars(context, geom);

                            geom.Render(context);
                        }
                    }

                }


            }
        }

        private void RenderWorldCalcEntityVisibility(YmapEntityDef ent)
        {
            float dist = (ent.Position - camera.Position).Length();

            if (MapViewEnabled)
            {
                dist = camera.OrthographicSize / MapViewDetail;
            }


            var loddist = ent._CEntityDef.lodDist;
            var cloddist = ent._CEntityDef.childLodDist;

            if (loddist <= 0.0f)//usually -1 or -2
            {
                if (ent.Archetype != null)
                {
                    loddist = ent.Archetype.LodDist * renderworldLodDistMult;
                }
            }
            else if (ent._CEntityDef.lodLevel == rage__eLodType.LODTYPES_DEPTH_ORPHANHD)
            {
                loddist *= renderworldDetailDistMult * 1.5f; //orphan view dist adjustment...
            }
            else
            {
                loddist *= renderworldLodDistMult;
            }


            if (cloddist <= 0)
            {
                if (ent.Archetype != null)
                {
                    cloddist = ent.Archetype.LodDist * renderworldLodDistMult;
                }
            }
            else
            {
                cloddist *= renderworldLodDistMult;
            }


            ent.Distance = dist;
            ent.IsVisible = (dist <= loddist);
            ent.ChildrenVisible = (dist <= cloddist) && (ent._CEntityDef.numChildren > 0);



            if (renderworldMaxLOD != rage__eLodType.LODTYPES_DEPTH_ORPHANHD)
            {
                if ((ent._CEntityDef.lodLevel == rage__eLodType.LODTYPES_DEPTH_ORPHANHD) ||
                    (ent._CEntityDef.lodLevel < renderworldMaxLOD))
                {
                    ent.IsVisible = false;
                    ent.ChildrenVisible = false;
                }
                if (ent._CEntityDef.lodLevel == renderworldMaxLOD)
                {
                    ent.ChildrenVisible = false;
                }
            }
        }
        private void RenderWorldRecurseCalcEntityVisibility(YmapEntityDef ent)
        {
            RenderWorldCalcEntityVisibility(ent);
            if (ent.ChildrenVisible)
            {
                if (ent.Children != null)
                {
                    for (int i = 0; i < ent.Children.Length; i++)
                    {
                        var child = ent.Children[i];
                        if (child.Ymap == ent.Ymap)
                        {
                            RenderWorldRecurseCalcEntityVisibility(child);
                        }
                    }
                }
            }
        }
        private void RenderWorldRecurseAddEntities(YmapEntityDef ent)
        {
            bool hide = ent.ChildrenVisible;
            bool force = (ent.Parent != null) && ent.Parent.ChildrenVisible && !hide;
            if (force || (ent.IsVisible && !hide))
            {
                if (ent.Archetype != null)
                {
                    if (!RenderIsEntityFinalRender(ent)) return;


                    if (!camera.ViewFrustum.ContainsAABBNoClip(ref ent.BBCenter, ref ent.BBExtent))
                    {
                        return;
                    }


                    renderworldentities.Add(ent);


                    if (renderinteriors && ent.IsMlo && (ent.MloInstance != null)) //render Mlo child entities...
                    {
                        RenderWorldAddInteriorEntities(ent);
                    }

                }
            }
            if (ent.IsVisible && ent.ChildrenVisible && (ent.Children != null))
            {
                for (int i = 0; i < ent.Children.Length; i++)
                {
                    var child = ent.Children[i];
                    if (child.Ymap == ent.Ymap)
                    {
                        RenderWorldRecurseAddEntities(ent.Children[i]);
                    }
                }
            }
        }

        
        private void RenderWorldAddInteriorEntities(YmapEntityDef ent)
        {
            if (ent?.MloInstance?.Entities != null)
            {
                for (int j = 0; j < ent.MloInstance.Entities.Length; j++)
                {
                    var intent = ent.MloInstance.Entities[j];
                    if (intent?.Archetype == null) continue; //missing archetype...
                    if (!RenderIsEntityFinalRender(intent)) continue; //proxy or something..

                    intent.IsVisible = true;

                    if (!camera.ViewFrustum.ContainsAABBNoClip(ref intent.BBCenter, ref intent.BBExtent))
                    {
                        continue; //frustum cull interior ents
                    }

                    renderworldentities.Add(intent);
                }
            }
            if (ent?.MloInstance?.EntitySets != null)
            {
                for (int e = 0; e < ent.MloInstance.EntitySets.Length; e++)
                {
                    var entityset = ent.MloInstance.EntitySets[e];
                    if ((entityset == null) || (!entityset.VisibleOrForced)) continue;

                    var entities = entityset.Entities;
                    if (entities == null) continue;
                    for (int i = 0; i < entities.Count; i++)
                    {
                        var intent = entities[i];
                        if (intent?.Archetype == null) continue; //missing archetype...
                        if (!RenderIsEntityFinalRender(intent)) continue; //proxy or something..

                        intent.IsVisible = true;

                        if (!camera.ViewFrustum.ContainsAABBNoClip(ref intent.BBCenter, ref intent.BBExtent))
                        {
                            continue; //frustum cull interior ents
                        }

                        renderworldentities.Add(intent);

                    }
                }
            }
        }
        
        private bool RenderIsEntityFinalRender(YmapEntityDef ent)
        {
            var arch = ent.Archetype;
            if (arch == null) return false;

            bool isshadowproxy = false;
            bool isreflproxy = false;
            uint archflags = arch._BaseArchetypeDef.flags;
            if (arch.Type == MetaName.CTimeArchetypeDef)
            {
                if (!(rendertimedents && (rendertimedentsalways || arch.IsActive(timeofday)))) return false;
                //archflags = arch._BaseArchetypeDef.flags;
            }
            //else if (arch.Type == MetaName.CMloArchetypeDef)
            //{
            //    archflags = arch._BaseArchetypeDef.flags;
            //}
            ////switch (archflags)
            ////{
            ////    //case 8192:  //8192: is YTYP no shadow rendering  - CP
            ////    case 2048:      //000000000000000000100000000000  shadow proxies...
            ////    case 536872960: //100000000000000000100000000000    tunnel refl/shadow prox?
            ////        isshadowproxy = true; break;
            ////}
            if ((archflags & 2048) > 0)
            {
                isshadowproxy = true;
            }

            //if ((ent.CEntityDef.flags & 1572864) == 1572864)
            //{
            //    isreflproxy = true;
            //}

            switch (ent._CEntityDef.flags)
            {
                case 135790592: //001000000110000000000000000000    prewater proxy (golf course)
                case 135790593: //001000000110000000000000000001    water refl proxy? (mike house)
                case 672661504: //101000000110000000000000000000    vb_ca_prop_tree_reflprox_2
                case 536870912: //100000000000000000000000000000    vb_05_emissive_mirroronly
                case 35127296:  //000010000110000000000000000000    tunnel refl proxy?
                case 39321602:  //000010010110000000000000000010    mlo reflection?
                    isreflproxy = true; break;
                    //nonproxy is:  //000000000110000000000000001000   (1572872)
                    //              //000000000110000000000000000000
            }
            if (isshadowproxy || isreflproxy)
            {
                return renderproxies; //filter out proxy entities...
            }
            return true;
        }
        private bool RenderIsModelFinalRender(RenderableModel model)
        {

            if ((model.RenderMaskFlags & 1) == 0) //smallest bit is proxy/"final render" bit? seems to work...
            {
                return renderproxies;
            }
            return true;

            //switch (model.Unk2Ch)
            //{
            //    case 65784:  //0000010000000011111000  //reflection proxy?
            //    case 65788:  //0000010000000011111100
            //    case 131312: //0000100000000011110000  //reflection proxy?
            //    case 131320: //0000100000000011111000  //reflection proxy?
            //    case 131324: //0000100000000011111100  //shadow/reflection proxy?
            //    case 196834: //0000110000000011100010 //shadow proxy? (tree branches)
            //    case 196848: //0000110000000011110000  //reflection proxy?
            //    case 196856: //0000110000000011111000 //reflection proxy? hotel nr golf course
            //    case 262392: //0001000000000011111000  //reflection proxy?
            //    case 327932: //0001010000000011111100  //reflection proxy? (alamo/sandy shores)
            //    case 983268: //0011110000000011100100  //big reflection proxy?
            //    case 2293988://1000110000000011100100  //big reflection proxy?
            //                 //case 1442047://golf course water proxy, but other things also
            //                 //case 1114367://mike house water proxy, but other things also
            //        return renderproxies;
            //}
            //return true;
        }




        private bool RenderYmapLOD(YmapFile ymap, YmapEntityDef entity)
        {
            if (!ymap.Loaded) return false;

            ymap.EnsureChildYmaps(gameFileCache);

            Archetype arch = entity.Archetype;
            if (arch != null)
            {
                bool timed = (arch.Type == MetaName.CTimeArchetypeDef);
                if (!timed || (rendertimedents && (rendertimedentsalways || arch.IsActive(timeofday))))
                {
                    bool usechild = false;
                    Vector3 camrel = entity.Position - camera.Position;
                    float dist = (camrel + entity.BSCenter).Length();
                    entity.Distance = dist;
                    float rad = arch.BSRadius;
                    float loddist = entity._CEntityDef.lodDist;
                    if (loddist < 1.0f)
                    {
                        loddist = 200.0f;
                    }
                    float mindist = Math.Max(dist - rad, 1.0f) * lodthreshold;
                    if (mindist < loddist)
                    {
                        //recurse...
                        var children = entity.ChildrenMerged;
                        if ((children != null))
                        {
                            usechild = true;
                            for (int i = 0; i < children.Length; i++)
                            {
                                var childe = children[i];
                                if (!RenderYmapLOD(childe.Ymap, childe))
                                {
                                    if (waitforchildrentoload)
                                    {
                                        usechild = false; //might cause some overlapping, but should reduce things disappearing
                                    }
                                }
                            }
                        }
                        if (!entity.ChildrenRendered)
                        {
                            entity.ChildrenRendered = usechild;
                        }
                    }
                    else
                    {
                        entity.ChildrenRendered = false;
                    }
                    if (!usechild && !entity.ChildrenRendered)
                    {

                        if (renderinteriors && entity.IsMlo) //render Mlo child entities...
                        {
                            if ((entity.MloInstance != null) && (entity.MloInstance.Entities != null))
                            {
                                for (int j = 0; j < entity.MloInstance.Entities.Length; j++)
                                {
                                    var intent = entity.MloInstance.Entities[j];
                                    var intarch = intent.Archetype;
                                    if (intarch == null) continue; //missing archetype...
                                    if (!RenderIsEntityFinalRender(intent)) continue; //proxy or something..
                                    RenderArchetype(intarch, intent);
                                }
                            }
                        }


                        return RenderArchetype(arch, entity);
                    }
                    return true;
                }

            }
            return false;
        }


        public bool RenderFragment(Archetype arch, YmapEntityDef ent, FragType f, uint txdhash = 0, ClipMapEntry animClip = null)
        {

            RenderDrawable(f.Drawable, arch, ent, txdhash, null, null, animClip);

            if (f.DrawableCloth != null) //cloth
            {
                RenderDrawable(f.DrawableCloth, arch, ent, txdhash, null, null, animClip);
            }

            //vehicle wheels...
            if ((f.PhysicsLODGroup != null) && (f.PhysicsLODGroup.PhysicsLOD1 != null))
            {
                var pl1 = f.PhysicsLODGroup.PhysicsLOD1;
                //var groupnames = pl1?.GroupNames?.data_items;
                var groups = pl1?.Groups?.data_items;

                FragDrawable wheel_f = null;
                FragDrawable wheel_r = null;

                if (pl1.Children?.data_items != null)
                {
                    for (int i = 0; i < pl1.Children.data_items.Length; i++)
                    {
                        var pch = pl1.Children.data_items[i];

                        //var groupname = pch.GroupNameHash;
                        //if ((pl1.Groups?.data_items != null) && (i < pl1.Groups.data_items.Length))
                        //{
                        //    //var group = pl1.Groups.data_items[i];
                        //}

                        if ((pch.Drawable1 != null) && (pch.Drawable1.AllModels.Length != 0))
                        {

                            switch (pch.BoneTag)
                            {
                                case 27922: //wheel_lf
                                case 26418: //wheel_rf
                                    wheel_f = pch.Drawable1;
                                    break;
                                case 29921: //wheel_lm1
                                case 29922: //wheel_lm2
                                case 29923: //wheel_lm3
                                case 27902: //wheel_lr
                                case 5857:  //wheel_rm1
                                case 5858:  //wheel_rm2
                                case 5859:  //wheel_rm3
                                case 26398: //wheel_rr
                                    wheel_r = pch.Drawable1;
                                    break;
                                default:

                                    RenderDrawable(pch.Drawable1, arch, ent, txdhash, null, null, animClip);

                                    break;
                            }

                        }
                        else
                        { }
                        if ((pch.Drawable2 != null) && (pch.Drawable2.AllModels.Length != 0))
                        {
                            RenderDrawable(pch.Drawable2, arch, ent, txdhash, null, null, animClip);
                        }
                        else
                        { }
                    }

                    if ((wheel_f != null) || (wheel_r != null))
                    {
                        for (int i = 0; i < pl1.Children.data_items.Length; i++)
                        {
                            var pch = pl1.Children.data_items[i];
                            FragDrawable dwbl = pch.Drawable1;
                            FragDrawable dwblcopy = null;
                            switch (pch.BoneTag)
                            {
                                case 27922: //wheel_lf
                                case 26418: //wheel_rf
                                    dwblcopy = wheel_f != null ? wheel_f : wheel_r;
                                    break;
                                case 29921: //wheel_lm1
                                case 29922: //wheel_lm2
                                case 29923: //wheel_lm3
                                case 27902: //wheel_lr
                                case 5857:  //wheel_rm1
                                case 5858:  //wheel_rm2
                                case 5859:  //wheel_rm3
                                case 26398: //wheel_rr
                                    dwblcopy = wheel_r != null ? wheel_r : wheel_f;
                                    break;
                                default:
                                    break;
                            }
                            //switch (pch.GroupNameHash)
                            //{
                            //    case 3311608449: //wheel_lf
                            //    case 1705452237: //wheel_lm1
                            //    case 1415282742: //wheel_lm2
                            //    case 3392433122: //wheel_lm3
                            //    case 133671269:  //wheel_rf
                            //    case 2908525601: //wheel_rm1
                            //    case 2835549038: //wheel_rm2
                            //    case 4148013026: //wheel_rm3
                            //        dwblcopy = wheel_f != null ? wheel_f : wheel_r;
                            //        break;
                            //    case 1695736278: //wheel_lr
                            //    case 1670111368: //wheel_rr
                            //        dwblcopy = wheel_r != null ? wheel_r : wheel_f;
                            //        break;
                            //    default:
                            //        break;
                            //}

                            if (dwblcopy != null)
                            {
                                if (dwbl != null)
                                {
                                    if ((dwbl != dwblcopy) && (dwbl.AllModels.Length == 0))
                                    {
                                        dwbl.Owner = dwblcopy;
                                        dwbl.AllModels = dwblcopy.AllModels; //hopefully this is all that's need to render, otherwise drawable is actually getting edited!
                                        //dwbl.DrawableModelsHigh = dwblcopy.DrawableModelsHigh;
                                        //dwbl.DrawableModelsMedium = dwblcopy.DrawableModelsMedium;
                                        //dwbl.DrawableModelsLow = dwblcopy.DrawableModelsLow;
                                        //dwbl.DrawableModelsVeryLow = dwblcopy.DrawableModelsVeryLow;
                                        //dwbl.VertexDecls = dwblcopy.VertexDecls;
                                    }

                                    RenderDrawable(dwbl, arch, ent, txdhash /*, null, null, animClip*/);

                                }
                                else
                                { }
                            }
                            else
                            { }
                        }
                    }

                }
            }


            bool isselected = SelectionFlagsTestAll || (f.Drawable == SelectedDrawable);
            if (isselected)
            {
                var darr = f.DrawableArray?.data_items;
                if (darr != null)
                {
                    for (int i = 0; i < darr.Length; i++)
                    {
                        RenderDrawable(darr[i], arch, ent, txdhash, null, null, animClip);
                    }
                }
            }



            if (renderfragwindows)
            {
                var colblu = (uint)(new Color(0, 0, 255, 255).ToRgba());
                var colred = (uint)(new Color(255, 0, 0, 255).ToRgba());
                var eori = Quaternion.Identity;
                var epos = Vector3.Zero;
                if (ent != null)
                {
                    eori = ent.Orientation;
                    epos = ent.Position;
                }

                if (f.GlassWindows?.data_items != null)
                {
                    for (int i = 0; i < f.GlassWindows.data_items.Length; i++)
                    {
                        var gw = f.GlassWindows.data_items[i];
                        var projt = gw.ProjectionRow1;//row0? or row3? maybe investigate more
                        var proju = gw.ProjectionRow2;//row1 of XYZ>UV projection
                        var projv = gw.ProjectionRow3;//row2 of XYZ>UV projection
                        //var unk01 = new Vector2(gw.UnkFloat13, gw.UnkFloat14);//offset?
                        //var unk02 = new Vector2(gw.UnkFloat15, gw.UnkFloat16);//scale? sum of this and above often gives integers eg 1, 6
                        //var thick = gw.Thickness; //thickness of the glass
                        //var unkuv = new Vector2(gw.UnkFloat18, gw.UnkFloat19); //another scale in UV space..?
                        //var tangt = gw.Tangent;//direction of surface tangent
                        //var bones = f.Drawable?.Skeleton?.Bones?.Items; //todo: use bones instead?
                        var grp = gw.Group;
                        var grplod = gw.GroupLOD;
                        var xforms = grplod?.FragTransforms?.Matrices;
                        var xoffs = Vector3.Zero;
                        if ((grp != null) && (xforms != null) && (grp.ChildIndex < xforms.Length) && (grplod != null))
                        {
                            var xform = xforms[grp.ChildIndex];
                            xoffs = xform.TranslationVector + grplod.PositionOffset;
                        }
                        var m = new Matrix();
                        m.Row1 = new Vector4(projt, 0);
                        m.Row2 = new Vector4(proju, 0);
                        m.Row3 = new Vector4(projv, 0);
                        m.Row4 = new Vector4(xoffs, 1);
                        var v0 = m.Multiply(new Vector3(1, 0, 0));
                        var v1 = m.Multiply(new Vector3(1, 0, 1));
                        var v2 = m.Multiply(new Vector3(1, 1, 1));
                        var v3 = m.Multiply(new Vector3(1, 1, 0));
                        var c0 = eori.Multiply(v0) + epos;
                        var c1 = eori.Multiply(v1) + epos;
                        var c2 = eori.Multiply(v2) + epos;
                        var c3 = eori.Multiply(v3) + epos;
                        RenderSelectionLine(c0, c1, colblu);
                        RenderSelectionLine(c1, c2, colblu);
                        RenderSelectionLine(c2, c3, colblu);
                        RenderSelectionLine(c3, c0, colblu);
                        //RenderSelectionLine(c0, c0 + tangt, colred);
                    }
                }
                if (f.VehicleGlassWindows?.Windows != null)
                {
                    for (int i = 0; i < f.VehicleGlassWindows.Windows.Length; i++)
                    {
                        var vgw = f.VehicleGlassWindows.Windows[i];
                        //var grp = vgw.Group;
                        //var grplod = vgw.GroupLOD;
                        var m = vgw.Projection;
                        m.M44 = 1.0f;
                        m.Transpose();
                        m.Invert();//ouch
                        var min = (new Vector3(0, 0, 0));
                        var max = (new Vector3(vgw.ShatterMapWidth, vgw.ItemDataCount, 1));
                        var v0 = m.MultiplyW(new Vector3(min.X, min.Y, 0));
                        var v1 = m.MultiplyW(new Vector3(min.X, max.Y, 0));
                        var v2 = m.MultiplyW(new Vector3(max.X, max.Y, 0));
                        var v3 = m.MultiplyW(new Vector3(max.X, min.Y, 0));
                        var c0 = eori.Multiply(v0) + epos;
                        var c1 = eori.Multiply(v1) + epos;
                        var c2 = eori.Multiply(v2) + epos;
                        var c3 = eori.Multiply(v3) + epos;
                        RenderSelectionLine(c0, c1, colblu);
                        RenderSelectionLine(c1, c2, colblu);
                        RenderSelectionLine(c2, c3, colblu);
                        RenderSelectionLine(c3, c0, colblu);
                        if (vgw.ShatterMap != null)
                        {
                            var width = vgw.ShatterMapWidth;
                            var height = vgw.ShatterMap.Length;
                            for (int y = 0; y < height; y++)
                            {
                                var smr = vgw.ShatterMap[y];
                                for (int x = 0; x < width; x++)
                                {
                                    var v = smr.GetValue(x);
                                    if ((v < 0) || (v > 255)) continue;
                                    var col = (uint)(new Color(v, v, v, 127).ToRgba());
                                    v0 = m.MultiplyW(new Vector3(x, y, 0));
                                    v1 = m.MultiplyW(new Vector3(x, y+1, 0));
                                    v2 = m.MultiplyW(new Vector3(x+1, y+1, 0));
                                    v3 = m.MultiplyW(new Vector3(x+1, y, 0));
                                    c0 = eori.Multiply(v0) + epos;
                                    c1 = eori.Multiply(v1) + epos;
                                    c2 = eori.Multiply(v2) + epos;
                                    c3 = eori.Multiply(v3) + epos;
                                    RenderSelectionQuad(c0, c1, c2, c3, col);//extra ouch
                                }
                            }
                        }

                    }
                }
            }


            return true;
        }

        public bool RenderArchetype(Archetype arche, YmapEntityDef entity, Renderable rndbl = null, bool cull = true, ClipMapEntry animClip = null)
        {
            //enqueue a single archetype for rendering.

            if (arche == null) return false;

            Vector3 entpos = (entity != null) ? entity.Position : Vector3.Zero;
            Vector3 camrel = entpos - camera.Position;

            Quaternion orientation = Quaternion.Identity;
            Vector3 scale = Vector3.One;
            Vector3 bscent = camrel;
            if (entity != null)
            {
                orientation = entity.Orientation;
                scale = entity.Scale;
                bscent += entity.BSCenter;
            }
            else
            {
                bscent += arche.BSCenter;
            }

            float bsrad = arche.BSRadius;// * scale;
            if (cull)
            {
                if (!camera.ViewFrustum.ContainsSphereNoClipNoOpt(ref bscent, bsrad))
                {
                    return true; //culled - not visible; don't render, but pretend we did for LOD purposes..
                }
            }

            float dist = bscent.Length();

            if (boundsmode == BoundsShaderMode.Sphere)
            {
                if ((bsrad < renderboundsmaxrad) && (dist < renderboundsmaxdist))
                {
                    MapSphere ms = new MapSphere();
                    ms.CamRelPos = bscent;
                    ms.Radius = bsrad;
                    BoundingSpheres.Add(ms);
                }
            }
            if (boundsmode == BoundsShaderMode.Box)
            {
                if ((dist < renderboundsmaxdist))
                {
                    MapBox mb = new MapBox();
                    mb.CamRelPos = camrel;
                    mb.BBMin = arche.BBMin;
                    mb.BBMax = arche.BBMax;
                    mb.Orientation = orientation;
                    mb.Scale = scale;
                    BoundingBoxes.Add(mb);
                }
            }



            bool res = false;
            if (rndbl == null)
            {
                var drawable = gameFileCache.TryGetDrawable(arche);
                rndbl = TryGetRenderable(arche, drawable);
            }

            if (rndbl != null)
            {
                if (animClip != null)
                {
                    rndbl.ClipMapEntry = animClip;
                    rndbl.ClipDict = animClip.Clip?.Ycd;
                    rndbl.HasAnims = true;
                }


                res = RenderRenderable(rndbl, arche, entity);


                //fragments have extra drawables! need to render those too... TODO: handle fragments properly...
                FragDrawable fd = rndbl.Key as FragDrawable;
                if (fd != null)
                {
                    var frag = fd.OwnerFragment;
                    if ((frag != null) && (frag.DrawableCloth != null)) //cloth...
                    {
                        rndbl = TryGetRenderable(arche, frag.DrawableCloth);
                        if (rndbl != null)
                        {
                            bool res2 = RenderRenderable(rndbl, arche, entity);
                            res = res || res2;
                        }
                    }
                }
            }


            return res;
        }

        public bool RenderDrawable(DrawableBase drawable, Archetype arche, YmapEntityDef entity, uint txdHash = 0, TextureDictionary txdExtra = null, Texture diffOverride = null, ClipMapEntry animClip = null, ClothInstance cloth = null, Expression expr = null, bool isProp = false, bool shouldOverride = false)
        {
            //enqueue a single drawable for rendering.

            if (drawable == null)
                return false;

            Renderable rndbl = TryGetRenderable(arche, drawable, txdHash, txdExtra, diffOverride, shouldOverride);
            if (rndbl == null)
                return false;

            if (animClip != null)
            {
                rndbl.ClipMapEntry = animClip;
                rndbl.ClipDict = animClip.Clip?.Ycd;
                rndbl.HasAnims = true;
            }
            else if ((arche == null) && (rndbl.ClipMapEntry != null))
            {
                rndbl.ClipMapEntry = null;
                rndbl.ClipDict = null;
                rndbl.HasAnims = false;
                rndbl.ResetBoneTransforms();
            }

            if (isProp)
            {
                rndbl.IsPedProp = true;
            }

            rndbl.Cloth = cloth;
            rndbl.Expression = expr;

            return RenderRenderable(rndbl, arche, entity);
        }

        private bool RenderRenderable(Renderable rndbl, Archetype arche, YmapEntityDef entity)
        {
            //enqueue a single renderable for rendering.

            if (!rndbl.IsLoaded) return false;


            if (RenderedDrawablesListEnable) //for later hit tests
            {
                var rd = new RenderedDrawable();
                rd.Drawable = rndbl.Key;
                rd.Archetype = arche;
                rd.Entity = entity;
                RenderedDrawables.Add(rd);
            }

            if (!RenderedDrawablesDict.ContainsKey(rndbl.Key))
            {
                var rd = new RenderedDrawable();
                rd.Drawable = rndbl.Key;
                rd.Archetype = arche;
                rd.Entity = entity;
                RenderedDrawablesDict[rndbl.Key] = rd;
            }

            bool isselected = SelectionFlagsTestAll || (rndbl.Key == SelectedDrawable);

            Vector3 camrel = -camera.Position;
            Vector3 position = Vector3.Zero;
            Vector3 scale = Vector3.One;
            Quaternion orientation = Quaternion.Identity;
            uint tintPaletteIndex = 0;
            Vector3 bbmin = (arche != null) ? arche.BBMin : rndbl.Key.BoundingBoxMin;
            Vector3 bbmax = (arche != null) ? arche.BBMax : rndbl.Key.BoundingBoxMax;
            Vector3 bscen = (arche != null) ? arche.BSCenter : rndbl.Key.BoundingCenter;
            float radius = (arche != null) ? arche.BSRadius : rndbl.Key.BoundingSphereRadius;
            float distance = 0;// (camrel + bscen).Length();
            bool interiorent = false;
            bool castshadow = true;

            if (entity != null)
            {
                position = entity.Position;
                scale = entity.Scale;
                orientation = entity.Orientation;
                tintPaletteIndex = entity._CEntityDef.tintValue;
                bbmin = entity.BBMin;
                bbmax = entity.BBMax;
                bscen = entity.BSCenter;
                camrel += position;
                distance = entity.Distance;
                castshadow = (entity.MloParent == null);//don't cast sun/moon shadows if this is an interior entity - optimisation!
                interiorent = (entity.MloParent != null);
            }
            else
            {
                distance = (camrel + bscen).Length();
            }

            if (rndbl.HasAnims)
            {
                rndbl.UpdateAnims(currentRealTime);
            }
            if (rndbl.Cloth != null)
            {
                rndbl.Cloth.Update(currentRealTime);
            }
            if (rndbl.IsPedProp)
            {
                rndbl.UpdatePropTransform();
            }

            if (rendercollisionmeshes && rendercollisionmeshlayerdrawable)
            {
                if ((entity == null) || ((entity._CEntityDef.flags & 4) == 0)) //skip if entity embedded collisions disabled
                {
                    Drawable sdrawable = rndbl.Key as Drawable;
                    if ((sdrawable != null) && (sdrawable.Bound != null))
                    {
                        RenderCollisionMesh(sdrawable.Bound, entity);
                    }
                    FragDrawable fdrawable = rndbl.Key as FragDrawable;
                    if (fdrawable != null)
                    {
                        if (fdrawable.Bound != null)
                        {
                            RenderCollisionMesh(fdrawable.Bound, entity);
                        }
                        var fbound = fdrawable.OwnerFragment?.PhysicsLODGroup?.PhysicsLOD1?.Bound;
                        if (fbound != null)
                        {
                            RenderCollisionMesh(fbound, entity);//TODO: these probably have extra transforms..!
                        }
                    }
                }
            }
            if (renderskeletons && rndbl.HasSkeleton)
            {
                RenderSkeleton(rndbl, entity);
            }

            if (renderlights && shaders != null && shaders.deferred && (rndbl.Lights != null))
            {
                entity?.EnsureLights(rndbl.Key);



                //reinit lights when added/removed from editor
                var dd = rndbl.Key as Drawable;
                var fd = rndbl.Key as FragDrawable;
                var lights = dd?.LightAttributes?.data_items;
                if ((lights == null) && (fd != null) && (fd?.OwnerFragment?.Drawable == fd))
                {
                    lights = fd.OwnerFragment.LightAttributes?.data_items;
                }
                if ((lights != null) && (lights.Length != rndbl.Lights.Length))
                {
                    rndbl.InitLights(lights);
                }


                var linst = new RenderableLightInst();
                for (int i = 0; i < rndbl.Lights.Length; i++)
                {
                    var rndlight = rndbl.Lights[i];
                    var light = rndlight.OwnerLight;

                    if (light.UpdateRenderable == true)
                    {
                        rndlight.Init(light);
                        light.UpdateRenderable = false;
                    }

                    linst.EntityPosition = position;
                    linst.EntityRotation = orientation;
                    linst.Light = rndlight;
                    shaders.Enqueue(ref linst);
                }
            }


            bool retval = true;// false;
            if ((rndbl.AllTexturesLoaded || !waitforchildrentoload) && (shaders != null))
            {
                RenderableGeometryInst rginst = new RenderableGeometryInst();
                rginst.Inst.Renderable = rndbl;
                rginst.Inst.CamRel = camrel;
                rginst.Inst.Position = position;
                rginst.Inst.Scale = scale;
                rginst.Inst.Orientation = orientation;
                rginst.Inst.TintPaletteIndex = tintPaletteIndex;
                rginst.Inst.BBMin = bbmin;
                rginst.Inst.BBMax = bbmax;
                rginst.Inst.BSCenter = bscen;
                rginst.Inst.Radius = radius;
                rginst.Inst.Distance = distance;
                rginst.Inst.CastShadow = castshadow;


                RenderableModel[] models = isselected ? rndbl.AllModels : rndbl.HDModels;

                for (int mi = 0; mi < models.Length; mi++)
                {
                    var model = models[mi];

                    if (isselected)
                    {
                        if (SelectionModelDrawFlags.ContainsKey(model.DrawableModel))
                        { continue; } //filter out models in selected item that aren't flagged for drawing.
                    }

                    if (!RenderIsModelFinalRender(model) && !renderproxies)
                    { continue; } //filter out reflection proxy models...

                    for (int gi = 0; gi < model.Geometries.Length; gi++)
                    {
                        var geom = model.Geometries[gi];
                        var dgeom = geom.DrawableGeom;

                        if (dgeom.UpdateRenderableParameters) //when edited by material editor
                        {
                            geom.Init(dgeom);
                            dgeom.UpdateRenderableParameters = false;
                        }

                        if (isselected)
                        {
                            if (geom.disableRendering || SelectionGeometryDrawFlags.ContainsKey(dgeom))
                            { continue; } //filter out geometries in selected item that aren't flagged for drawing.
                        }
                        else
                        {
                            if (geom.disableRendering)
                            { continue; } //filter out certain geometries like certain hair parts that shouldn't render by default
                        }

                        if (geom.isHair)
                        {
                            if (SelDrawable != null && (SelDrawable.IsHairScaleEnabled || SelectedDrawableChanged))
                            {
                                Bone[] scaleBones = new Bone[2];
                                if (rndbl.Skeleton?.Bones == null) continue;
                                scaleBones[0] = rndbl.Skeleton.Bones.Items[100];
                                scaleBones[1] = rndbl.Skeleton.Bones.Items[101];

                                //reverse value
                                SelDrawable.HairScaleValue = 1.0f - SelDrawable.HairScaleValue;

                                foreach (var b in scaleBones)
                                {
                                    b.Scale = SelDrawable.IsHairScaleEnabled ? new Vector3(SelDrawable.HairScaleValue, SelDrawable.HairScaleValue, SelDrawable.HairScaleValue) : new Vector3(1.0f, 1.0f, 1.0f);
                                }

                                if (SelectedDrawableChanged)
                                {
                                    renderfloor = SelDrawable.IsHighHeelsEnabled;
                                    rndbl.ResetBoneTransforms();
                                    geom.Init(dgeom);
                                    SelectedDrawableChanged = false;
                                }
                            }
                        }

                        rginst.Geom = geom;
                        shaders.Enqueue(ref rginst);
                    }
                }
            }
            else
            {
                retval = false;
            }
            return retval;
        }

        public void RenderPed(Ped ped)
        {

            YftFile yft = ped.Yft;// GameFileCache.GetYft(SelectedModelHash);
            if (yft != null)
            {
                var vi = ped.Ymt?.VariationInfo;
                if (vi != null)
                {
                    for (int i = 0; i < 12; i++)
                    {
                        RenderPedComponent(ped, i);
                    }
                }
            }
        }

        private void RenderPedComponent(Ped ped, int i)
        {
            var drawable = ped.Drawables[i];
            var texture = ped.Textures[i];
            var cloth = ped.Clothes[i];
            var expr = ped.Expressions[i];

            if (drawable == null) return;

            var td = ped.Ytd?.TextureDict;
            var ac = ped.AnimClip;
            if (ac != null)
            {
                ac.EnableRootMotion = ped.EnableRootMotion;
            }

            var skel = ped.Skeleton;
            if (skel != null)
            {
                if (drawable.Skeleton == null)
                {
                    drawable.Skeleton = skel;//force the drawable to use this skeleton.
                }
                else if (drawable.Skeleton != skel)
                {
                    var dskel = drawable.Skeleton; //put the bones of the fragment into the drawable. drawable's bones in this case seem messed up!
                    if (skel.Bones?.Items != null)
                    {
                        for (int b = 0; b < skel.Bones.Items.Length; b++)
                        {
                            var srcbone = skel.Bones.Items[b];
                            var dstbone = srcbone;
                            if (dskel.BonesMap.TryGetValue(srcbone.Tag, out dstbone))
                            {
                                if (srcbone == dstbone) break; //bone reassignment already done!
                                dskel.Bones.Items[dstbone.Index] = srcbone;
                                dskel.BonesMap[srcbone.Tag] = srcbone;
                            }
                        }
                        dskel.BonesSorted = skel.BonesSorted;//this is pretty hacky. TODO: try and fix all this! animate only the frag skeleton!
                    }
                }
            }

            bool drawFlag = true;
            if (!SelectionDrawableDrawFlags.TryGetValue(drawable, out drawFlag))
            { drawFlag = true; }

            if (drawFlag)
            {
                RenderDrawable(drawable, null, ped.RenderEntity, 0, td, texture, ac, cloth, expr);
            }
        }

        public void RenderCollisionMesh(Bounds bounds, YmapEntityDef entity)
        {
            //enqueue a single collision mesh for rendering.

            Vector3 position;
            Vector3 scale;
            Quaternion orientation;
            if (entity != null)
            {
                position = entity.Position;
                scale = entity.Scale;
                orientation = entity.Orientation;
            }
            else
            {
                position = Vector3.Zero;
                scale = Vector3.One;
                orientation = Quaternion.Identity;
            }

            RenderableBoundComposite rndbc = renderableCache.GetRenderableBoundComp(bounds);
            if ((rndbc != null) && rndbc.IsLoaded && (shaders != null))
            {
                RenderableBoundGeometryInst rbginst = new RenderableBoundGeometryInst();
                rbginst.Inst.Renderable = rndbc;
                if (rndbc.Geometries != null)
                {
                    foreach (var geom in rndbc.Geometries)
                    {
                        if (geom == null) continue;
                        rbginst.Geom = geom;
                        
                        var pos = position;
                        var ori = orientation;
                        var sca = scale;
                        if (geom.Bound is BoundGeometry bgeom)
                        {
                            var rmat = bgeom.Transform;
                            sca = scale * rmat.ScaleVector;
                            pos = position + orientation.Multiply(rmat.TranslationVector);
                            rmat.TranslationVector = Vector3.Zero;
                            ori = orientation * Quaternion.RotationMatrix(rmat);
                        }
                        rbginst.Inst.Position = pos + ori.Multiply(geom.CenterGeom * sca);
                        rbginst.Inst.Orientation = ori;
                        rbginst.Inst.Scale = sca;
                        rbginst.Inst.CamRel = rbginst.Inst.Position - camera.Position;
                        shaders.Enqueue(ref rbginst);
                    }
                }

                if (RenderedBoundCompsListEnable) //for later hit tests
                {
                    var rb = new RenderedBoundComposite();
                    rb.BoundComp = rndbc;
                    rb.Entity = entity;
                    RenderedBoundComps.Add(rb);
                }
            }

        }

        private Renderable TryGetRenderable(Archetype arche, DrawableBase drawable, uint txdHash = 0, TextureDictionary txdExtra = null, Texture diffOverride = null, bool shouldOverride = false)
        {
            if (drawable == null) return null;
            //BUG: only last texdict used!! needs to cache textures per archetype........
            //(but is it possible to have the same drawable with different archetypes?)
            MetaHash texDict = txdHash;
            //uint texDictOrig = txdHash;
            uint clipDict = 0;

            if (arche != null)
            {
                texDict = arche.TextureDict.Hash;
                clipDict = arche.ClipDict.Hash;
            }


            Renderable rndbl = renderableCache.GetRenderable(drawable);
            if (rndbl == null) return null;

            if ((clipDict != 0) && (rndbl.ClipDict == null))
            {
                YcdFile ycd = gameFileCache.GetYcd(clipDict);
                if ((ycd != null) && (ycd.Loaded))
                {
                    rndbl.ClipDict = ycd;
                    MetaHash ahash = arche.Hash;
                    if (ycd.ClipMap.TryGetValue(ahash, out rndbl.ClipMapEntry)) rndbl.HasAnims = true;

                    foreach (var model in rndbl.HDModels)
                    {
                        if (model == null) continue;
                        foreach (var geom in model.Geometries)
                        {
                            if (geom == null) continue;
                            if (geom.globalAnimUVEnable)
                            {
                                uint cmeindex = geom.DrawableGeom.ShaderID + 1u;
                                MetaHash cmehash = ahash + cmeindex; //this goes to at least uv5! (from uv0) - see hw1_09.ycd
                                if (ycd.ClipMap.TryGetValue(cmehash, out geom.ClipMapEntryUV)) rndbl.HasAnims = true;
                            }
                        }
                    }
                }
            }


            var extraTexDict = (drawable.Owner as YptFile)?.PtfxList?.TextureDictionary;
            if (extraTexDict == null) extraTexDict = txdExtra;

            bool cacheSD = (rndbl.SDtxds == null);
            bool cacheHD = (renderhdtextures && (rndbl.HDtxds == null));
            if (cacheSD || cacheHD)
            {
                //cache the txd hierarchies for this renderable
                tryGetRenderableSDtxds.Clear();
                tryGetRenderableHDtxds.Clear();
                if (cacheHD && (arche != null)) //try get HD txd for the asset
                {
                    MetaHash hdtxd = gameFileCache.TryGetHDTextureHash(arche._BaseArchetypeDef.assetName);
                    if (hdtxd != arche._BaseArchetypeDef.assetName)
                    {
                        var asshdytd = gameFileCache.GetYtd(hdtxd);
                        if (asshdytd != null)
                        {
                            tryGetRenderableHDtxds.Add(asshdytd);
                        }
                    }
                }
                if (texDict != 0)
                {
                    if (cacheSD)
                    {
                        var txdytd = gameFileCache.GetYtd(texDict);
                        if (txdytd != null)
                        {
                            tryGetRenderableSDtxds.Add(txdytd);
                        }
                    }
                    if (cacheHD)
                    {
                        MetaHash hdtxd = gameFileCache.TryGetHDTextureHash(texDict);
                        if (hdtxd != texDict)
                        {
                            var txdhdytd = gameFileCache.GetYtd(hdtxd);
                            if (txdhdytd != null)
                            {
                                tryGetRenderableHDtxds.Add(txdhdytd);
                            }
                        }
                    }
                    MetaHash ptxdname = gameFileCache.TryGetParentYtdHash(texDict);
                    while (ptxdname != 0) //look for parent HD txds
                    {
                        if (cacheSD)
                        {
                            var pytd = gameFileCache.GetYtd(ptxdname);
                            if (pytd != null)
                            {
                                tryGetRenderableSDtxds.Add(pytd);
                            }
                        }
                        if (cacheHD)
                        {
                            MetaHash phdtxdname = gameFileCache.TryGetHDTextureHash(ptxdname);
                            if (phdtxdname != ptxdname)
                            {
                                var phdytd = gameFileCache.GetYtd(phdtxdname);
                                if (phdytd != null)
                                {
                                    tryGetRenderableHDtxds.Add(phdytd);
                                }
                            }
                        }
                        ptxdname = gameFileCache.TryGetParentYtdHash(ptxdname);
                    }
                }
                if (cacheSD) rndbl.SDtxds = tryGetRenderableSDtxds.ToArray();
                if (cacheHD) rndbl.HDtxds = tryGetRenderableHDtxds.ToArray();
            }

            bool alltexsloaded = true;
            for (int mi = 0; mi < rndbl.AllModels.Length; mi++)
            {
                var model = rndbl.AllModels[mi];

                if (!RenderIsModelFinalRender(model) && !renderproxies)
                {
                    continue; //filter out reflection proxy models...
                }

                foreach (var geom in model.Geometries)
                {
                    if (geom.Textures != null)
                    {
                        for (int i = 0; i < geom.Textures.Length; i++)
                        {
                            if (diffOverride != null)
                            {
                                var texParamHash = (i < geom.TextureParamHashes?.Length) ? geom.TextureParamHashes[i] : 0;

                                if (shouldOverride)
                                {
                                    LiveTextureMode liveTextureMode = LiveTextureMode.Diffuse;
                                    switch (texParamHash)
                                    {
                                        case ShaderParamNames.DiffuseSampler:
                                            liveTextureMode = LiveTextureMode.Diffuse;
                                            break;
                                        case ShaderParamNames.SpecSampler:
                                            liveTextureMode = LiveTextureMode.Specular;
                                            break;
                                        case ShaderParamNames.BumpSampler:
                                            liveTextureMode = LiveTextureMode.Normal;
                                            break;
                                    }

                                    if (LiveTextureSelectedMode == liveTextureMode)
                                    {
                                        if (LiveTextureEnabled)
                                        {
                                            savedTexture = savedTexture ?? (geom.Textures[i] as Texture);
                                            geom.Textures[i] = diffOverride;
                                        }
                                        else if (savedTexture != null)
                                        {
                                            geom.Textures[i] = savedTexture;
                                            savedTexture = null;
                                        }
                                    }

                                    if (!LiveTextureEnabled && texParamHash == ShaderParamNames.DiffuseSampler)
                                    {
                                        geom.Textures[i] = diffOverride;
                                    }
                                }
                                else if (texParamHash == ShaderParamNames.DiffuseSampler)
                                {
                                    geom.Textures[i] = diffOverride;
                                }
                            }

                            var tex = geom.Textures[i];
                            var ttex = tex as Texture;
                            Texture dtex = null;
                            RenderableTexture rdtex = null;
                            if ((tex != null) && (ttex == null))
                            {
                                //TextureRef means this RenderableTexture needs to be loaded from texture dict...
                                if (extraTexDict != null) //for ypt files, first try the embedded tex dict..
                                {
                                    dtex = extraTexDict.Lookup(tex.NameHash);
                                }

                                if (dtex == null) //else //if (texDict != 0)
                                {
                                    bool waitingforload = false;
                                    if (rndbl.SDtxds != null)
                                    {
                                        //check the SD texture hierarchy
                                        for (int j = 0; j < rndbl.SDtxds.Length; j++)
                                        {
                                            var txd = rndbl.SDtxds[j];
                                            if (txd.Loaded)
                                            {
                                                dtex = txd.TextureDict?.Lookup(tex.NameHash);
                                            }
                                            else
                                            {
                                                txd = gameFileCache.GetYtd(txd.Key.Hash);//keep trying to load it - sometimes resuests can get lost (!)
                                                waitingforload = true;
                                            }
                                            if (dtex != null) break;
                                        }

                                        if (waitingforload)
                                        {
                                            alltexsloaded = false;
                                        }
                                    }

                                    if ((dtex == null) && (!waitingforload))
                                    {
                                        //not present in dictionary... check already loaded texture dicts... (maybe resident?)
                                        var ytd2 = gameFileCache.TryGetTextureDictForTexture(tex.NameHash);
                                        if (ytd2 != null)
                                        {
                                            if (ytd2.Loaded)
                                            {
                                                if (ytd2.TextureDict != null)
                                                {
                                                    dtex = ytd2.TextureDict.Lookup(tex.NameHash);
                                                }
                                            }
                                            else
                                            {
                                                alltexsloaded = false;
                                            }
                                        }

                                        //else { } //couldn't find texture dict?

                                        if ((dtex == null) && (ytd2 == null))// rndbl.SDtxds.Length == 0)//texture not found..
                                        {
                                            if (drawable.ShaderGroup?.TextureDictionary != null)//check any embedded texdict
                                            {
                                                dtex = drawable.ShaderGroup.TextureDictionary.Lookup(tex.NameHash);
                                                if (dtex != null)
                                                { } //this shouldn't really happen as embedded textures should already be loaded! (not as TextureRef)
                                            }
                                        }
                                    }
                                }

                                if (dtex != null)
                                {
                                    geom.Textures[i] = dtex; //cache it for next time to avoid the lookup...
                                    ttex = dtex;
                                }
                            }


                            if (ttex != null) //ensure renderable texture
                            {
                                rdtex = renderableCache.GetRenderableTexture(ttex);
                            }

                            geom.RenderableTextures[i] = rdtex;

                            RenderableTexture rhdtex = null;
                            if (renderhdtextures)
                            {
                                Texture hdtex = geom.TexturesHD[i];
                                if (hdtex == null)
                                {
                                    //look for a replacement HD texture...
                                    if (rndbl.HDtxds != null)
                                    {
                                        for (int j = 0; j < rndbl.HDtxds.Length; j++)
                                        {
                                            var txd = rndbl.HDtxds[j];
                                            if (txd.Loaded)
                                            {
                                                hdtex = txd.TextureDict?.Lookup(tex.NameHash);
                                            }
                                            else
                                            {
                                                txd = gameFileCache.GetYtd(txd.Key.Hash);//keep trying to load it - sometimes resuests can get lost (!)
                                            }
                                            if (hdtex != null) break;
                                        }
                                    }
                                    if (hdtex != null)
                                    {
                                        geom.TexturesHD[i] = hdtex;
                                    }
                                }
                                if (hdtex != null)
                                {
                                    rhdtex = renderableCache.GetRenderableTexture(hdtex);
                                }
                            }
                            geom.RenderableTexturesHD[i] = rhdtex;

                        }
                    }
                }
            }


            rndbl.AllTexturesLoaded = alltexsloaded;


            return rndbl;
        }
    }


    public struct RenderedDrawable
    {
        public DrawableBase Drawable;
        public Archetype Archetype;
        public YmapEntityDef Entity;
    }
    public struct RenderedBoundComposite
    {
        public RenderableBoundComposite BoundComp;
        public YmapEntityDef Entity;
    }

    public struct RenderSkeletonItem
    {
        public Renderable Renderable;
        public YmapEntityDef Entity;
    }



    public enum WorldRenderMode
    {
        Default = 0,
        SingleTexture = 1,
        VertexNormals = 2,
        VertexTangents = 3,
        VertexColour = 4,
        TextureCoord = 5,
    }

    public enum LiveTextureMode
    {
        Diffuse = 0,
        Normal = 1,
        Specular = 2
    }

    public class RenderLodManager
    {
        public rage__eLodType MaxLOD = rage__eLodType.LODTYPES_DEPTH_ORPHANHD;
        public float LodDistMult = 1.0f;
        public bool MapViewEnabled = false;
        public float MapViewDist = 1.0f;
        public bool ShowScriptedYmaps = true;
        public bool HDLightsEnabled = true;
        public bool LODLightsEnabled = true;

        public Camera Camera = null;
        public Vector3 Position = Vector3.Zero;

        public Dictionary<MetaHash, YmapFile> CurrentYmaps = new Dictionary<MetaHash, YmapFile>();
        private List<MetaHash> RemoveYmaps = new List<MetaHash>();
        public Dictionary<YmapEntityDef, YmapEntityDef> RootEntities = new Dictionary<YmapEntityDef, YmapEntityDef>();
        public List<YmapEntityDef> VisibleLeaves = new List<YmapEntityDef>();

        public Dictionary<uint, YmapLODLight> LodLightsDict = new Dictionary<uint, YmapLODLight>();
        public HashSet<YmapEntityDef.LightInstance> VisibleLights = new HashSet<YmapEntityDef.LightInstance>();
        public HashSet<YmapEntityDef.LightInstance> VisibleLightsPrev = new HashSet<YmapEntityDef.LightInstance>();
        public HashSet<YmapLODLights> UpdateLodLights = new HashSet<YmapLODLights>();

        public void Update(Dictionary<MetaHash, YmapFile> ymaps, Camera camera, float elapsed)
        {
            Camera = camera;
            Position = camera.Position;

            foreach (var kvp in ymaps)
            {
                var ymap = kvp.Value;
                if (ymap._CMapData.parent != 0) //ensure parent references on ymaps
                {
                    ymaps.TryGetValue(ymap._CMapData.parent, out YmapFile pymap);
                    if (pymap == null) //skip adding ymaps until parents are available
                    { continue; }
                    if (ymap.Parent != pymap)
                    {
                        ymap.ConnectToParent(pymap);
                    }
                }
            }

            RemoveYmaps.Clear();
            foreach (var kvp in CurrentYmaps)
            {
                YmapFile ymap = null;
                if (!ymaps.TryGetValue(kvp.Key, out ymap) || (ymap != kvp.Value) || (ymap.IsScripted && !ShowScriptedYmaps) || (ymap.LodManagerUpdate))
                {
                    RemoveYmaps.Add(kvp.Key);
                }
            }
            foreach (var remYmap in RemoveYmaps)
            {
                var ymap = CurrentYmaps[remYmap];
                CurrentYmaps.Remove(remYmap);
                var remEnts = ymap.LodManagerOldEntities ?? ymap.AllEntities;
                if (remEnts != null)    // remove this ymap's entities from the tree.....
                {
                    for (int i = 0; i < remEnts.Length; i++)
                    {
                        var ent = remEnts[i];
                        RootEntities.Remove(ent);
                        ent.LodManagerChildren?.Clear();
                        ent.LodManagerChildren = null;
                        ent.LodManagerRenderable = null;
                        if ((ent.Parent != null) && (ent.Parent.Ymap != ymap))
                        {
                            ent.Parent.LodManagerRemoveChild(ent);
                        }
                    }
                }
                var remLodLights = ymap.LODLights?.LodLights;
                if (remLodLights != null)
                {
                    for (int i = 0; i < remLodLights.Length; i++)
                    {
                        LodLightsDict.Remove(remLodLights[i].Hash);
                    }
                }
                ymap.LodManagerUpdate = false;
                ymap.LodManagerOldEntities = null;
            }
            foreach (var kvp in ymaps)
            {
                var ymap = kvp.Value;
                if (ymap.IsScripted && !ShowScriptedYmaps)
                { continue; }
                if ((ymap._CMapData.parent != 0) && (ymap.Parent == null)) //skip adding ymaps until parents are available
                { continue; }
                if (!CurrentYmaps.ContainsKey(kvp.Key))
                {
                    CurrentYmaps.Add(kvp.Key, kvp.Value);
                    if (ymap.AllEntities != null)    // add this ymap's entities to the tree...
                    {
                        for (int i = 0; i < ymap.AllEntities.Length; i++)
                        {
                            var ent = ymap.AllEntities[i];
                            if (ent.Parent != null)
                            {
                                ent.Parent.LodManagerAddChild(ent);
                            }
                            else
                            {
                                RootEntities[ent] = ent;
                            }
                        }
                    }
                    var addLodLights = ymap.LODLights?.LodLights;
                    if (addLodLights != null)
                    {
                        for (int i = 0; i < addLodLights.Length; i++)
                        {
                            var light = addLodLights[i];
                            LodLightsDict[light.Hash] = light;
                        }
                    }
                }
            }


            VisibleLeaves.Clear();
            VisibleLights.Clear();
            foreach (var kvp in RootEntities)
            {
                var ent = kvp.Key;
                if (EntityVisibleAtMaxLodLevel(ent))
                {
                    ent.Distance = MapViewEnabled ? MapViewDist : (ent.Position - Position).Length();
                    if (ent.Distance <= (ent.LodDist * LodDistMult))
                    {
                        RecurseAddVisibleLeaves(ent);
                    }
                }
            }

            UpdateLodLights.Clear();
            foreach (var light in VisibleLights)
            {
                if (VisibleLightsPrev.Contains(light) == false)
                {
                    if (LodLightsDict.TryGetValue(light.Hash, out var lodlight))
                    {
                        lodlight.Enabled = false;
                        UpdateLodLights.Add(lodlight.LodLights);
                    }
                }
            }
            foreach (var light in VisibleLightsPrev)
            {
                if (VisibleLights.Contains(light) == false)
                {
                    if (LodLightsDict.TryGetValue(light.Hash, out var lodlight))
                    {
                        lodlight.Enabled = true;
                        UpdateLodLights.Add(lodlight.LodLights);
                    }
                }
            }


            var vl = VisibleLights;
            VisibleLights = VisibleLightsPrev;
            VisibleLightsPrev = vl;
        }

        private void RecurseAddVisibleLeaves(YmapEntityDef ent)
        {
            var clist = GetEntityChildren(ent);
            if (clist != null)
            {
                var cnode = clist.First;
                while (cnode != null)
                {
                    RecurseAddVisibleLeaves(cnode.Value);
                    cnode = cnode.Next;
                }
            }
            else
            {
                if (EntityVisible(ent))
                {
                    VisibleLeaves.Add(ent);

                    if (HDLightsEnabled && (ent.Lights != null))
                    {
                        for (int i = 0; i < ent.Lights.Length; i++)
                        {
                            VisibleLights.Add(ent.Lights[i]);
                        }
                    }
                }
            }
        }



        private LinkedList<YmapEntityDef> GetEntityChildren(YmapEntityDef ent)
        {
            //get the children list for this entity, if all the hcildren are available, and they are within range
            if (!EntityChildrenVisibleAtMaxLodLevel(ent)) return null;
            var clist = ent.LodManagerChildren;
            if ((clist != null) && (clist.Count >= ent._CEntityDef.numChildren))
            {
                if (ent.Parent != null)//already calculated root entities distance
                {
                    ent.Distance = MapViewEnabled ? MapViewDist : (ent.Position - Position).Length();
                }
                if (ent.Distance <= (ent.ChildLodDist * LodDistMult))
                {
                    return clist;
                }
                else
                {
                    var cnode = clist.First;
                    while (cnode != null)
                    {
                        var child = cnode.Value;
                        child.Distance = MapViewEnabled ? MapViewDist : (child.Position - Position).Length();
                        if (child.Distance <= (child.LodDist * LodDistMult))
                        {
                            return clist;
                        }
                        cnode = cnode.Next;
                    }
                }
            }
            return null;
        }

        private bool EntityVisible(YmapEntityDef ent)
        {
            if (MapViewEnabled)
            {
                return Camera.ViewFrustum.ContainsAABBNoFrontClipNoOpt(ref ent.BBMin, ref ent.BBMax);
            }
            else
            {
                return Camera.ViewFrustum.ContainsAABBNoClip(ref ent.BBCenter, ref ent.BBExtent);
            }
        }
        private bool EntityVisibleAtMaxLodLevel(YmapEntityDef ent)
        {
            if (MaxLOD != rage__eLodType.LODTYPES_DEPTH_ORPHANHD)
            {
                if ((ent._CEntityDef.lodLevel == rage__eLodType.LODTYPES_DEPTH_ORPHANHD) ||
                    (ent._CEntityDef.lodLevel < MaxLOD))
                {
                    return false;
                }
            }
            return true;
        }
        private bool EntityChildrenVisibleAtMaxLodLevel(YmapEntityDef ent)
        {
            if (MaxLOD != rage__eLodType.LODTYPES_DEPTH_ORPHANHD)
            {
                if ((ent._CEntityDef.lodLevel == rage__eLodType.LODTYPES_DEPTH_ORPHANHD) ||
                    (ent._CEntityDef.lodLevel <= MaxLOD))
                {
                    return false;
                }
            }
            return true;
        }


    }

}
