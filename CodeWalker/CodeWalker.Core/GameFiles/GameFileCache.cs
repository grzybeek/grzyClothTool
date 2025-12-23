using SharpDX;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace CodeWalker.GameFiles
{
    public class GameFileCache
    {
        public RpfManager RpfMan;
        private Action<string> UpdateStatus;
        private Action<string> ErrorLog;
        public int MaxItemsPerLoop = 1; //to keep things flowing...

        private ConcurrentQueue<GameFile> requestQueue = new ConcurrentQueue<GameFile>();

        ////dynamic cache
        private Cache<GameFileCacheKey, GameFile> mainCache;
        public volatile bool IsInited = false;

        private volatile bool archetypesLoaded = false;
        private Dictionary<uint, Archetype> archetypeDict = new Dictionary<uint, Archetype>();
        private Dictionary<uint, RpfFileEntry> textureLookup = new Dictionary<uint, RpfFileEntry>();
        private Dictionary<MetaHash, MetaHash> textureParents;
        private Dictionary<MetaHash, MetaHash> hdtexturelookup;

        private object updateSyncRoot = new object();
        private object requestSyncRoot = new object();
        private object textureSyncRoot = new object(); //for the texture lookup.


        private Dictionary<GameFileCacheKey, GameFile> projectFiles = new Dictionary<GameFileCacheKey, GameFile>(); //for cache files loaded in project window: ydr,ydd,ytd,yft
        private Dictionary<uint, Archetype> projectArchetypes = new Dictionary<uint, Archetype>(); //used to override archetypes in world view with project ones




        //static indexes
        public Dictionary<uint, RpfFileEntry> YdrDict { get; private set; }
        public Dictionary<uint, RpfFileEntry> YddDict { get; private set; }
        public Dictionary<uint, RpfFileEntry> YtdDict { get; private set; }
        public Dictionary<uint, RpfFileEntry> YmapDict { get; private set; }
        public Dictionary<uint, RpfFileEntry> YftDict { get; private set; }
        public Dictionary<uint, RpfFileEntry> YbnDict { get; private set; }
        public Dictionary<uint, RpfFileEntry> YcdDict { get; private set; }
        public Dictionary<uint, RpfFileEntry> YedDict { get; private set; }
        public Dictionary<uint, RpfFileEntry> YnvDict { get; private set; }
        public Dictionary<uint, RpfFileEntry> Gxt2Dict { get; private set; }


        public Dictionary<uint, RpfFileEntry> AllYmapsDict { get; private set; }


        //static cached data loaded at init
        public Dictionary<uint, YtypFile> YtypDict { get; set; }

        public List<CacheDatFile> AllCacheFiles { get; set; }
        public Dictionary<uint, MapDataStoreNode> YmapHierarchyDict { get; set; }

        public List<YmfFile> AllManifests { get; set; }


        public bool EnableDlc { get; set; } = false;//true;//
        public bool EnableMods { get; set; } = false;

        public List<string> DlcPaths { get; set; } = new List<string>();
        public List<RpfFile> DlcActiveRpfs { get; set; } = new List<RpfFile>();
        public List<DlcSetupFile> DlcSetupFiles { get; set; } = new List<DlcSetupFile>();
        public List<DlcExtraFolderMountFile> DlcExtraFolderMounts { get; set; } = new List<DlcExtraFolderMountFile>();
        public Dictionary<string, string> DlcPatchedPaths { get; set; } = new Dictionary<string, string>();
        public List<string> DlcCacheFileList { get; set; } = new List<string>();
        public List<string> DlcNameList { get; set; } = new List<string>();
        public string SelectedDlc { get; set; } = string.Empty;

        public Dictionary<string, RpfFile> ActiveMapRpfFiles { get; set; } = new Dictionary<string, RpfFile>();

        public Dictionary<uint, World.TimecycleMod> TimeCycleModsDict = new Dictionary<uint, World.TimecycleMod>();

        public Dictionary<MetaHash, VehicleInitData> VehiclesInitDict { get; set; }
        public Dictionary<MetaHash, CPedModelInfo__InitData> PedsInitDict { get; set; }
        public Dictionary<MetaHash, PedFile> PedVariationsDict { get; set; }
        public Dictionary<MetaHash, Dictionary<MetaHash, RpfFileEntry>> PedDrawableDicts { get; set; }
        public Dictionary<MetaHash, Dictionary<MetaHash, RpfFileEntry>> PedTextureDicts { get; set; }
        public Dictionary<MetaHash, Dictionary<MetaHash, RpfFileEntry>> PedClothDicts { get; set; }


        public List<RelFile> AudioDatRelFiles = new List<RelFile>();
        public Dictionary<MetaHash, RelData> AudioConfigDict = new Dictionary<MetaHash, RelData>();
        public Dictionary<MetaHash, RelData> AudioSpeechDict = new Dictionary<MetaHash, RelData>();
        public Dictionary<MetaHash, RelData> AudioSynthsDict = new Dictionary<MetaHash, RelData>();
        public Dictionary<MetaHash, RelData> AudioMixersDict = new Dictionary<MetaHash, RelData>();
        public Dictionary<MetaHash, RelData> AudioCurvesDict = new Dictionary<MetaHash, RelData>();
        public Dictionary<MetaHash, RelData> AudioCategsDict = new Dictionary<MetaHash, RelData>();
        public Dictionary<MetaHash, RelData> AudioSoundsDict = new Dictionary<MetaHash, RelData>();
        public Dictionary<MetaHash, RelData> AudioGameDict = new Dictionary<MetaHash, RelData>();



        public List<RpfFile> BaseRpfs { get; private set; }
        public List<RpfFile> AllRpfs { get; private set; }
        public List<RpfFile> DlcRpfs { get; private set; }

        public bool DoFullStringIndex = false;
        public bool BuildExtendedJenkIndex = true;
        public bool LoadArchetypes = true;
        public bool LoadVehicles = true;
        public bool LoadPeds = true;
        public bool LoadAudio = true;
        private bool PreloadedMode = false;

        private string GTAFolder;
        private string ExcludeFolders;

        public GameFileCache(long size, double cacheTime, string folder, string dlc, bool mods, string excludeFolders)
        {
            mainCache = new Cache<GameFileCacheKey, GameFile>(size, cacheTime);//2GB is good as default
            SelectedDlc = dlc;
            EnableDlc = !string.IsNullOrEmpty(SelectedDlc);
            EnableMods = false;
            GTAFolder = folder;
            ExcludeFolders = excludeFolders;
        }


        public void Clear()
        {
            IsInited = false;

            mainCache.Clear();

            textureLookup.Clear();

            GameFile queueclear;
            while (requestQueue.TryDequeue(out queueclear))
            { } //empty the old queue out...
        }

        public void Init(Action<string> updateStatus, Action<string> errorLog)
        {
            UpdateStatus = updateStatus;
            ErrorLog = errorLog;

            Clear();


            if (RpfMan == null)
            {

                RpfMan = new RpfManager();
                RpfMan.ExcludePaths = GetExcludePaths();
                RpfMan.EnableMods = EnableMods;
                RpfMan.BuildExtendedJenkIndex = BuildExtendedJenkIndex;
                RpfMan.Init(GTAFolder, UpdateStatus, ErrorLog);//, true);


                InitGlobal();

                InitDlc();
            }
            else
            {
                GC.Collect(); //try free up some of the previously used memory..
            }

            UpdateStatus("Scan complete");


            IsInited = true;
        }

        private void InitGlobal()
        {
            BaseRpfs = GetModdedRpfList(RpfMan.BaseRpfs);
            AllRpfs = GetModdedRpfList(RpfMan.AllRpfs);
            DlcRpfs = GetModdedRpfList(RpfMan.DlcRpfs);

            UpdateStatus("Building global dictionaries...");
            InitGlobalDicts();
        }

        private void InitDlc()
        {

            UpdateStatus("Building DLC List...");
            InitDlcList();

            UpdateStatus("Building active RPF dictionary...");
            InitActiveMapRpfFiles();

            UpdateStatus("Loading global texture list...");
            InitGtxds();

            UpdateStatus("Loading strings...");
            InitStringDicts();

            UpdateStatus("Loading peds...");
            InitPeds();

            //UpdateStatus("Loading audio...");
            //InitAudio();

        }

        private void InitDlcList()
        {
            //if (!EnableDlc) return;

            string dlclistpath = "update\\update.rpf\\common\\data\\dlclist.xml";
            //if (!EnableDlc)
            //{
            //    dlclistpath = "common.rpf\\data\\dlclist.xml";
            //}
            var dlclistxml = RpfMan.GetFileXml(dlclistpath);

            DlcPaths.Clear();
            if ((dlclistxml == null) || (dlclistxml.DocumentElement == null))
            {
                ErrorLog("InitDlcList: Couldn't load " + dlclistpath + ".");
            }
            else
            {
                foreach (XmlNode pathsnode in dlclistxml.DocumentElement)
                {
                    foreach (XmlNode itemnode in pathsnode.ChildNodes)
                    {
                        DlcPaths.Add(itemnode.InnerText.ToLowerInvariant().Replace('\\', '/').Replace("platform:", "x64"));
                    }
                }
            }


            //get dlc path names in the appropriate format for reference by the dlclist paths
            Dictionary<string, RpfFile> dlcDict = new Dictionary<string, RpfFile>();
            Dictionary<string, RpfFile> dlcDict2 = new Dictionary<string, RpfFile>();
            foreach (RpfFile dlcrpf in DlcRpfs)
            {
                if (dlcrpf == null) continue;
                if (dlcrpf.NameLower == "dlc.rpf")
                {
                    string path = GetDlcRpfVirtualPath(dlcrpf.Path);
                    string name = GetDlcNameFromPath(dlcrpf.Path);
                    dlcDict[path] = dlcrpf;
                    dlcDict2[name] = dlcrpf;
                }
            }




            //find all the paths for patched files in update.rpf and build the dict
            DlcPatchedPaths.Clear();
            string updrpfpath = "update\\update.rpf";
            var updrpffile = RpfMan.FindRpfFile(updrpfpath);

            if (updrpffile != null)
            {
                try
                {
                    XmlDocument updsetupdoc = RpfMan.GetFileXml(updrpfpath + "\\setup2.xml");
                    DlcSetupFile updsetupfile = new DlcSetupFile();
                    updsetupfile.Load(updsetupdoc);

                    XmlDocument updcontentdoc = RpfMan.GetFileXml(updrpfpath + "\\" + updsetupfile.datFile);
                    DlcContentFile updcontentfile = new DlcContentFile();
                    updcontentfile.Load(updcontentdoc);

                    updsetupfile.DlcFile = updrpffile;
                    updsetupfile.ContentFile = updcontentfile;
                    updcontentfile.DlcFile = updrpffile;

                    updsetupfile.deviceName = "update";
                    updcontentfile.LoadDicts(updsetupfile, RpfMan, this);

                    if (updcontentfile.ExtraTitleUpdates != null)
                    {
                        foreach (var tumount in updcontentfile.ExtraTitleUpdates.Mounts)
                        {
                            var lpath = tumount.path.ToLowerInvariant();
                            var relpath = lpath.Replace('/', '\\').Replace("update:\\", "");
                            var dlcname = GetDlcNameFromPath(relpath);
                            RpfFile dlcfile;
                            dlcDict2.TryGetValue(dlcname, out dlcfile);
                            if (dlcfile == null)
                            { continue; }
                            var dlcpath = dlcfile.Path + "\\";
                            var files = updrpffile.GetFiles(relpath, true);
                            foreach (var file in files)
                            {
                                if (file == null) continue;
                                var fpath = file.Path;
                                var frelpath = fpath.Replace(updrpfpath, "update:").Replace('\\', '/').Replace(lpath, dlcpath).Replace('/', '\\');
                                if (frelpath.StartsWith("mods\\"))
                                {
                                    frelpath = frelpath.Substring(5);
                                }
                                DlcPatchedPaths[frelpath] = fpath;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ErrorLog("InitDlcList: Failed to load update.rpf setup files. GTA V installation may be corrupted or incomplete.\n" + ex.Message);
                }
            }
            else
            {
                ErrorLog("InitDlcList: update.rpf not found!");
            }




            DlcSetupFiles.Clear();
            DlcExtraFolderMounts.Clear();

            foreach (string path in DlcPaths)
            {
                RpfFile dlcfile;
                if (dlcDict.TryGetValue(path, out dlcfile))
                {
                    try
                    {
                        string setuppath = GetDlcPatchedPath(dlcfile.Path + "\\setup2.xml");
                        XmlDocument setupdoc = RpfMan.GetFileXml(setuppath);
                        DlcSetupFile setupfile = new DlcSetupFile();
                        setupfile.Load(setupdoc);

                        string contentpath = GetDlcPatchedPath(dlcfile.Path + "\\" + setupfile.datFile);
                        XmlDocument contentdoc = RpfMan.GetFileXml(contentpath);
                        DlcContentFile contentfile = new DlcContentFile();
                        contentfile.Load(contentdoc);

                        setupfile.DlcFile = dlcfile;
                        setupfile.ContentFile = contentfile;
                        contentfile.DlcFile = dlcfile;

                        contentfile.LoadDicts(setupfile, RpfMan, this);
                        foreach (var extramount in contentfile.ExtraMounts.Values)
                        {
                            DlcExtraFolderMounts.Add(extramount);
                        }

                        DlcSetupFiles.Add(setupfile);

                    }
                    catch (Exception ex)
                    {
                        ErrorLog("InitDlcList: Error processing DLC " + path + "\n" + ex.ToString());
                    }
                }
            }

            //load the DLC in the correct order.... 
            DlcSetupFiles = DlcSetupFiles.OrderBy(o => o.order).ToList();


            DlcNameList.Clear();
            foreach (var sfile in DlcSetupFiles)
            {
                if ((sfile == null) || (sfile.DlcFile == null)) continue;
                DlcNameList.Add(GetDlcNameFromPath(sfile.DlcFile.Path));
            }

            if (DlcNameList.Count > 0)
            {
                if (string.IsNullOrEmpty(SelectedDlc))
                {
                    SelectedDlc = DlcNameList[DlcNameList.Count - 1];
                }
            }
        }

        private void InitActiveMapRpfFiles()
        {
            ActiveMapRpfFiles.Clear();

            foreach (RpfFile baserpf in BaseRpfs) //start with all the base rpf's (eg x64a.rpf)
            {
                string path = baserpf.Path.Replace('\\', '/');
                if (path == "common.rpf")
                {
                    ActiveMapRpfFiles["common"] = baserpf;
                }
                else
                {
                    int bsind = path.IndexOf('/');
                    if ((bsind > 0) && (bsind < path.Length))
                    {
                        path = "x64" + path.Substring(bsind);

                        //if (ActiveMapRpfFiles.ContainsKey(path))
                        //{ } //x64d.rpf\levels\gta5\generic\cutsobjects.rpf // x64g.rpf\levels\gta5\generic\cutsobjects.rpf - identical?

                        ActiveMapRpfFiles[path] = baserpf;
                    }
                    else
                    {
                        //do we need to include root rpf files? generally don't seem to contain map data?
                        ActiveMapRpfFiles[path] = baserpf;
                    }
                }
            }

            if (!EnableDlc) return; //don't continue for base title only

            foreach (var rpf in DlcRpfs)
            {
                if (rpf.NameLower == "update.rpf")//include this so that files not in child rpf's can be used..
                {
                    string path = rpf.Path.Replace('\\', '/');
                    ActiveMapRpfFiles[path] = rpf;
                    break;
                }
            }


            DlcActiveRpfs.Clear();
            DlcCacheFileList.Clear();

            //int maxdlcorder = 10000000;

            Dictionary<string, List<string>> overlays = new Dictionary<string, List<string>>();

            foreach (var setupfile in DlcSetupFiles)
            {
                if (setupfile.DlcFile != null)
                {
                    //if (setupfile.order > maxdlcorder)
                    //    break;

                    var contentfile = setupfile.ContentFile;
                    var dlcfile = setupfile.DlcFile;

                    DlcActiveRpfs.Add(dlcfile);

                    for (int i = 1; i <= setupfile.subPackCount; i++)
                    {
                        var subpackPath = dlcfile.Path.Replace("\\dlc.rpf", "\\dlc" + i.ToString() + ".rpf");
                        var subpack = RpfMan.FindRpfFile(subpackPath);
                        if (subpack != null)
                        {
                            DlcActiveRpfs.Add(subpack);

                            if (setupfile.DlcSubpacks == null) setupfile.DlcSubpacks = new List<RpfFile>();
                            setupfile.DlcSubpacks.Add(subpack);
                        }
                    }



                    string dlcname = GetDlcNameFromPath(dlcfile.Path);
                    if ((dlcname == "patchday27ng") && (SelectedDlc != dlcname))
                    {
                        continue; //hack to fix map getting completely broken by this DLC.. but why? need to investigate further!
                    }



                    foreach (var rpfkvp in contentfile.RpfDataFiles)
                    {
                        string umpath = GetDlcUnmountedPath(rpfkvp.Value.filename);
                        string phpath = GetDlcRpfPhysicalPath(umpath, setupfile);

                        //if (rpfkvp.Value.overlay)
                        AddDlcOverlayRpf(rpfkvp.Key, umpath, setupfile, overlays);

                        AddDlcActiveMapRpfFile(rpfkvp.Key, phpath, setupfile);
                    }




                    DlcExtraFolderMountFile extramount;
                    DlcContentDataFile rpfdatafile;


                    foreach (var changeset in contentfile.contentChangeSets)
                    {
                        if (changeset.useCacheLoader)
                        {
                            uint cachehash = JenkHash.GenHash(changeset.changeSetName.ToLowerInvariant());
                            string cachefilename = dlcname + "_" + cachehash.ToString() + "_cache_y.dat";
                            string cachefilepath = dlcfile.Path + "\\x64\\data\\cacheloaderdata_dlc\\" + cachefilename;
                            string cachefilepathpatched = GetDlcPatchedPath(cachefilepath);
                            DlcCacheFileList.Add(cachefilepathpatched);

                            //if ((changeset.mapChangeSetData != null) && (changeset.mapChangeSetData.Count > 0))
                            //{ }
                            //else
                            //{ }
                        }
                        else
                        {
                            //if ((changeset.mapChangeSetData != null) && (changeset.mapChangeSetData.Count > 0))
                            //{ }
                            //if (changeset.executionConditions != null)
                            //{ }
                        }
                        //if (changeset.filesToInvalidate != null)
                        //{ }//not used
                        //if (changeset.filesToDisable != null)
                        //{ }//not used
                        if (changeset.filesToEnable != null)
                        {
                            foreach (string file in changeset.filesToEnable)
                            {
                                string dfn = GetDlcPlatformPath(file).ToLowerInvariant();
                                if (contentfile.ExtraMounts.TryGetValue(dfn, out extramount))
                                {
                                    //foreach (var rpfkvp in contentfile.RpfDataFiles)
                                    //{
                                    //    string umpath = GetDlcUnmountedPath(rpfkvp.Value.filename);
                                    //    string phpath = GetDlcRpfPhysicalPath(umpath, setupfile);
                                    //    //if (rpfkvp.Value.overlay)
                                    //    AddDlcOverlayRpf(rpfkvp.Key, umpath, setupfile, overlays);
                                    //    AddDlcActiveMapRpfFile(rpfkvp.Key, phpath);
                                    //}
                                }
                                else if (contentfile.RpfDataFiles.TryGetValue(dfn, out rpfdatafile))
                                {
                                    string phpath = GetDlcRpfPhysicalPath(rpfdatafile.filename, setupfile);

                                    //if (rpfdatafile.overlay)
                                    AddDlcOverlayRpf(dfn, rpfdatafile.filename, setupfile, overlays);

                                    AddDlcActiveMapRpfFile(dfn, phpath, setupfile);
                                }
                                else
                                {
                                    if (dfn.EndsWith(".rpf"))
                                    { }
                                }
                            }
                        }
                        if (changeset.executionConditions != null)
                        { }

                        if (changeset.mapChangeSetData != null)
                        {
                            foreach (var mapcs in changeset.mapChangeSetData)
                            {
                                //if (mapcs.mapChangeSetData != null)
                                //{ }//not used
                                if (mapcs.filesToInvalidate != null)
                                {
                                    foreach (string file in mapcs.filesToInvalidate)
                                    {
                                        string upath = GetDlcMountedPath(file);
                                        string fpath = GetDlcPlatformPath(upath);
                                        if (fpath.EndsWith(".rpf"))
                                        {
                                            RemoveDlcActiveMapRpfFile(fpath, overlays);
                                        }
                                        else
                                        { } //how to deal with individual files? milo_.interior
                                    }
                                }
                                if (mapcs.filesToDisable != null)
                                { }
                                if (mapcs.filesToEnable != null)
                                {
                                    foreach (string file in mapcs.filesToEnable)
                                    {
                                        string fpath = GetDlcPlatformPath(file);
                                        string umpath = GetDlcUnmountedPath(fpath);
                                        string phpath = GetDlcRpfPhysicalPath(umpath, setupfile);

                                        if (fpath != umpath)
                                        { }

                                        AddDlcOverlayRpf(fpath, umpath, setupfile, overlays);

                                        AddDlcActiveMapRpfFile(fpath, phpath, setupfile);
                                    }
                                }
                            }
                        }
                    }




                    if (dlcname == SelectedDlc)
                    {
                        break; //everything's loaded up to the selected DLC.
                    }

                }
            }
        }

        private void AddDlcActiveMapRpfFile(string vpath, string phpath, DlcSetupFile setupfile)
        {
            vpath = vpath.ToLowerInvariant();
            phpath = phpath.ToLowerInvariant();
            if (phpath.EndsWith(".rpf"))
            {
                RpfFile rpffile = RpfMan.FindRpfFile(phpath);
                if (rpffile != null)
                {
                    ActiveMapRpfFiles[vpath] = rpffile;
                }
                else
                { }
            }
            else
            { } //how to handle individual files? eg interiorProxies.meta
        }
        private void AddDlcOverlayRpf(string path, string umpath, DlcSetupFile setupfile, Dictionary<string, List<string>> overlays)
        {
            string opath = GetDlcOverlayPath(umpath, setupfile);
            if (opath == path) return;
            List<string> overlayList;
            if (!overlays.TryGetValue(opath, out overlayList))
            {
                overlayList = new List<string>();
                overlays[opath] = overlayList;
            }
            overlayList.Add(path);
        }
        private void RemoveDlcActiveMapRpfFile(string vpath, Dictionary<string, List<string>> overlays)
        {
            List<string> overlayList;
            if (overlays.TryGetValue(vpath, out overlayList))
            {
                foreach (string overlayPath in overlayList)
                {
                    if (ActiveMapRpfFiles.ContainsKey(overlayPath))
                    {
                        ActiveMapRpfFiles.Remove(overlayPath);
                    }
                    else
                    { }
                }
                overlays.Remove(vpath);
            }

            if (ActiveMapRpfFiles.ContainsKey(vpath))
            {
                ActiveMapRpfFiles.Remove(vpath);
            }
            else
            { } //nothing to remove?
        }
        private string GetDlcRpfPhysicalPath(string path, DlcSetupFile setupfile)
        {
            string devname = setupfile.deviceName.ToLowerInvariant();
            string fpath = GetDlcPlatformPath(path).ToLowerInvariant();
            string kpath = fpath;//.Replace(devname + ":\\", "");
            string dlcpath = setupfile.DlcFile.Path;
            fpath = fpath.Replace(devname + ":", dlcpath);
            fpath = fpath.Replace("x64:", dlcpath + "\\x64").Replace('/', '\\');
            if (setupfile.DlcSubpacks != null)
            {
                if (RpfMan.FindRpfFile(fpath) == null)
                {
                    foreach (var subpack in setupfile.DlcSubpacks)
                    {
                        dlcpath = subpack.Path;
                        var tpath = kpath.Replace(devname + ":", dlcpath);
                        tpath = tpath.Replace("x64:", dlcpath + "\\x64").Replace('/', '\\');
                        if (RpfMan.FindRpfFile(tpath) != null)
                        {
                            return GetDlcPatchedPath(tpath);
                        }
                    }
                }
            }
            return GetDlcPatchedPath(fpath);
        }
        private string GetDlcOverlayPath(string path, DlcSetupFile setupfile)
        {
            string devname = setupfile.deviceName.ToLowerInvariant();
            string fpath = path.Replace("%PLATFORM%", "x64").Replace('\\', '/').ToLowerInvariant();
            string opath = fpath.Replace(devname + ":/", "");
            return opath;
        }
        private string GetDlcRpfVirtualPath(string path)
        {
            path = path.Replace('\\', '/');
            if (path.StartsWith("mods/"))
            {
                path = path.Substring(5);
            }
            if (path.Length > 7)
            {
                path = path.Substring(0, path.Length - 7);//trim off "dlc.rpf"
            }
            if (path.StartsWith("x64"))
            {
                int bsind = path.IndexOf('/'); //replace x64*.rpf
                if ((bsind > 0) && (bsind < path.Length))
                {
                    path = "x64" + path.Substring(bsind);
                }
                else
                { } //no hits here
            }
            else if (path.StartsWith("update/x64/dlcpacks"))
            {
                path = path.Replace("update/x64/dlcpacks", "dlcpacks:");
            }
            else
            { } //no hits here

            return path;
        }
        private string GetDlcNameFromPath(string path)
        {
            string[] parts = path.ToLowerInvariant().Split('\\');
            if (parts.Length > 1)
            {
                return parts[parts.Length - 2].ToLowerInvariant();
            }
            return path;
        }
        public static string GetDlcPlatformPath(string path)
        {
            return path.Replace("%PLATFORM%", "x64").Replace('\\', '/').Replace("platform:", "x64").ToLowerInvariant();
        }
        private string GetDlcMountedPath(string path)
        {
            foreach (var efm in DlcExtraFolderMounts)
            {
                foreach (var fm in efm.FolderMounts)
                {
                    if ((fm.platform == null) || (fm.platform == "x64"))
                    {
                        if (path.StartsWith(fm.path))
                        {
                            path = path.Replace(fm.path, fm.mountAs);
                        }
                    }
                }
            }
            return path;
        }
        private string GetDlcUnmountedPath(string path)
        {
            foreach (var efm in DlcExtraFolderMounts)
            {
                foreach (var fm in efm.FolderMounts)
                {
                    if ((fm.platform == null) || (fm.platform == "x64"))
                    {
                        if (path.StartsWith(fm.mountAs))
                        {
                            path = path.Replace(fm.mountAs, fm.path);
                        }
                    }
                }
            }
            return path;
        }
        public string GetDlcPatchedPath(string path)
        {
            string p;
            if (DlcPatchedPaths.TryGetValue(path, out p))
            {
                return p;
            }
            return path;
        }

        private List<RpfFile> GetModdedRpfList(List<RpfFile> list)
        {
            //if (!EnableMods) return new List<RpfFile>(list);
            List<RpfFile> rlist = new List<RpfFile>();
            RpfFile f;
            if (!EnableMods)
            {
                foreach (var file in list)
                {
                    if (!file.Path.StartsWith("mods"))
                    {
                        rlist.Add(file);
                    }
                }
            }
            else
            {
                foreach (var file in list)
                {
                    if (RpfMan.ModRpfDict.TryGetValue(file.Path, out f))
                    {
                        rlist.Add(f);
                    }
                    else
                    {
                        if (file.Path.StartsWith("mods"))
                        {
                            var basepath = file.Path.Substring(5);
                            if (!RpfMan.RpfDict.ContainsKey(basepath)) //this file isn't overriding anything
                            {
                                rlist.Add(file);
                            }
                        }
                        else
                        {
                            rlist.Add(file);
                        }
                    }
                }
            }
            return rlist;
        }


        private void InitGlobalDicts()
        {
            YdrDict = new Dictionary<uint, RpfFileEntry>();
            YddDict = new Dictionary<uint, RpfFileEntry>();
            YtdDict = new Dictionary<uint, RpfFileEntry>();
            YftDict = new Dictionary<uint, RpfFileEntry>();
            YcdDict = new Dictionary<uint, RpfFileEntry>();
            YedDict = new Dictionary<uint, RpfFileEntry>();
            foreach (var rpffile in AllRpfs)
            {
                if (rpffile.AllEntries == null) continue;
                foreach (var entry in rpffile.AllEntries)
                {
                    if (entry is RpfFileEntry)
                    {
                        RpfFileEntry fentry = entry as RpfFileEntry;
                        if (entry.NameLower.EndsWith(".ydr"))
                        {
                            YdrDict[entry.ShortNameHash] = fentry;
                        }
                        else if (entry.NameLower.EndsWith(".ydd"))
                        {
                            YddDict[entry.ShortNameHash] = fentry;
                        }
                        else if (entry.NameLower.EndsWith(".ytd"))
                        {
                            YtdDict[entry.ShortNameHash] = fentry;
                        }
                        else if (entry.NameLower.EndsWith(".yft"))
                        {
                            YftDict[entry.ShortNameHash] = fentry;
                        }
                        else if (entry.NameLower.EndsWith(".ycd"))
                        {
                            YcdDict[entry.ShortNameHash] = fentry;
                        }
                        else if (entry.NameLower.EndsWith(".yed"))
                        {
                            YedDict[entry.ShortNameHash] = fentry;
                        }
                    }
                }
            }

        }

        private void InitGtxds()
        {

            var parentTxds = new Dictionary<MetaHash, MetaHash>();

            IEnumerable<RpfFile> rpfs = PreloadedMode ? AllRpfs : (IEnumerable<RpfFile>)ActiveMapRpfFiles.Values;

            var addTxdRelationships = new Action<Dictionary<string, string>>((from) =>
            {
                foreach (var kvp in from)
                {
                    uint chash = JenkHash.GenHash(kvp.Key.ToLowerInvariant());
                    uint phash = JenkHash.GenHash(kvp.Value.ToLowerInvariant());
                    if (!parentTxds.ContainsKey(chash))
                    {
                        parentTxds.Add(chash, phash);
                    }
                    else
                    {
                    }
                }
            });

            var addRpfTxdRelationships = new Action<IEnumerable<RpfFile>>((from) =>
            {
                foreach (RpfFile file in from)
                {
                    if (file.AllEntries == null) continue;
                    foreach (RpfEntry entry in file.AllEntries)
                    {
                        try
                        {
                            if ((entry.NameLower == "gtxd.ymt") || (entry.NameLower == "gtxd.meta") || (entry.NameLower == "mph4_gtxd.ymt"))
                            {
                                GtxdFile ymt = RpfMan.GetFile<GtxdFile>(entry);
                                if (ymt.TxdRelationships != null)
                                {
                                    addTxdRelationships(ymt.TxdRelationships);
                                }
                            }
                            else if (entry.NameLower == "vehicles.meta")
                            {
                                VehiclesFile vf = RpfMan.GetFile<VehiclesFile>(entry);//could also get loaded in InitVehicles...
                                if (vf.TxdRelationships != null)
                                {
                                    addTxdRelationships(vf.TxdRelationships);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            string errstr = entry.Path + "\n" + ex.ToString();
                            ErrorLog(errstr);
                        }
                    }
                }

            });


            addRpfTxdRelationships(rpfs);


            if (EnableDlc)
            {
                addRpfTxdRelationships(DlcActiveRpfs);
            }


            textureParents = parentTxds;




            //ensure resident global texture dicts:
            YtdFile ytd1 = new YtdFile(GetYtdEntry(JenkHash.GenHash("mapdetail")));
            LoadFile(ytd1);
            AddTextureLookups(ytd1);

            YtdFile ytd2 = new YtdFile(GetYtdEntry(JenkHash.GenHash("vehshare")));
            LoadFile(ytd2);
            AddTextureLookups(ytd2);



        }

        public void InitStringDicts()
        {
            string langstr = "american_rel"; //todo: make this variable?
            string langstr2 = "americandlc.rpf";
            string langstr3 = "american.rpf";

            Gxt2Dict = new Dictionary<uint, RpfFileEntry>();
            var gxt2files = new List<Gxt2File>();
            foreach (var rpf in AllRpfs)
            {
                foreach (var entry in rpf.AllEntries)
                {
                    if (entry is RpfFileEntry fentry)
                    {
                        var p = entry.Path;
                        if (entry.NameLower.EndsWith(".gxt2") && (p.Contains(langstr) || p.Contains(langstr2) || p.Contains(langstr3)))
                        {
                            Gxt2Dict[entry.ShortNameHash] = fentry;

                            if (DoFullStringIndex)
                            {
                                var gxt2 = RpfMan.GetFile<Gxt2File>(entry);
                                if (gxt2 != null)
                                {
                                    for (int i = 0; i < gxt2.TextEntries.Length; i++)
                                    {
                                        var e = gxt2.TextEntries[i];
                                        GlobalText.Ensure(e.Text, e.Hash);
                                    }
                                    gxt2files.Add(gxt2);
                                }
                            }
                        }
                    }
                }
            }

            if (!DoFullStringIndex)
            {
                string globalgxt2path = "x64b.rpf\\data\\lang\\" + langstr + ".rpf\\global.gxt2";
                var globalgxt2 = RpfMan.GetFile<Gxt2File>(globalgxt2path);
                if (globalgxt2 != null)
                {
                    for (int i = 0; i < globalgxt2.TextEntries.Length; i++)
                    {
                        var e = globalgxt2.TextEntries[i];
                        GlobalText.Ensure(e.Text, e.Hash);
                    }
                }
                return;
            }


            GlobalText.FullIndexBuilt = true;





            foreach (var rpf in AllRpfs)
            {
                foreach (var entry in rpf.AllEntries)
                {
                    if (entry.NameLower.EndsWith("statssetup.xml"))
                    {
                        var xml = RpfMan.GetFileXml(entry.Path);
                        if (xml == null)
                        { continue; }

                        var statnodes = xml.SelectNodes("StatsSetup/stats/stat");

                        foreach (XmlNode statnode in statnodes)
                        {
                            if (statnode == null)
                            { continue; }
                            var statname = Xml.GetStringAttribute(statnode, "Name");
                            if (string.IsNullOrEmpty(statname))
                            { continue; }

                            var statnamel = statname.ToLowerInvariant();
                            StatsNames.Ensure(statname);
                            StatsNames.Ensure(statnamel);

                            StatsNames.Ensure("sp_" + statnamel);
                            StatsNames.Ensure("mp0_" + statnamel);
                            StatsNames.Ensure("mp1_" + statnamel);

                        }
                    }
                }
            }

            StatsNames.FullIndexBuilt = true;
        }

        public void InitPeds()
        {
            if (!LoadPeds) return;

            IEnumerable<RpfFile> rpfs = PreloadedMode ? AllRpfs : (IEnumerable<RpfFile>)ActiveMapRpfFiles.Values;
            List<RpfFile> dlcrpfs = new List<RpfFile>();
            if (EnableDlc)
            {
                foreach (var rpf in DlcActiveRpfs)
                {
                    dlcrpfs.Add(rpf);
                    if (rpf.Children == null) continue;
                    foreach (var crpf in rpf.Children)
                    {
                        dlcrpfs.Add(crpf);
                        if (crpf.Children?.Count > 0)
                        { }
                    }
                }
            }



            var allPeds = new Dictionary<MetaHash, CPedModelInfo__InitData>();
            var allPedsFiles = new List<PedsFile>();
            var allPedYmts = new Dictionary<MetaHash, PedFile>();
            var allPedDrwDicts = new Dictionary<MetaHash, Dictionary<MetaHash, RpfFileEntry>>();
            var allPedTexDicts = new Dictionary<MetaHash, Dictionary<MetaHash, RpfFileEntry>>();
            var allPedClothDicts = new Dictionary<MetaHash, Dictionary<MetaHash, RpfFileEntry>>();


            Dictionary<MetaHash, RpfFileEntry> ensureDict(Dictionary<MetaHash, Dictionary<MetaHash, RpfFileEntry>> coll, MetaHash hash)
            {
                Dictionary<MetaHash, RpfFileEntry> dict;
                if (!coll.TryGetValue(hash, out dict))
                {
                    dict = new Dictionary<MetaHash, RpfFileEntry>();
                    coll[hash] = dict;
                }
                return dict;
            }

            var addPedDicts = new Action<string, MetaHash, RpfDirectoryEntry>((namel, hash, dir) =>
            {
                Dictionary<MetaHash, RpfFileEntry> dict = null;
                var files = dir?.Files;
                if (files != null)
                {
                    foreach (var file in files)
                    {
                        if (file.NameLower == namel + ".yld")
                        {
                            dict = ensureDict(allPedClothDicts, hash);
                            dict[file.ShortNameHash] = file;
                        }
                    }
                }

                if (dir?.Directories != null)
                {
                    foreach (var cdir in dir.Directories)
                    {
                        if (cdir.NameLower == namel)
                        {
                            dir = cdir;
                            break;
                        }
                    }
                    files = dir?.Files;
                    if (files != null)
                    {
                        foreach (var file in files)
                        {
                            if (file?.NameLower == null) continue;
                            if (file.NameLower.EndsWith(".ydd"))
                            {
                                dict = ensureDict(allPedDrwDicts, hash);
                                dict[file.ShortNameHash] = file;
                            }
                            else if (file.NameLower.EndsWith(".ytd"))
                            {
                                dict = ensureDict(allPedTexDicts, hash);
                                dict[file.ShortNameHash] = file;
                            }
                            else if (file.NameLower.EndsWith(".yld"))
                            {
                                dict = ensureDict(allPedClothDicts, hash);
                                dict[file.ShortNameHash] = file;
                            }
                        }
                    }
                }
            });

            var addPedsFiles = new Action<IEnumerable<RpfFile>>((from) =>
            {
                foreach (RpfFile file in from)
                {
                    if (file.AllEntries == null) continue;
                    foreach (RpfEntry entry in file.AllEntries)
                    {
#if !DEBUG
                        try
#endif
                        {
                            if ((entry.NameLower == "peds.ymt") || (entry.NameLower == "peds.meta"))
                            {
                                var pf = RpfMan.GetFile<PedsFile>(entry);
                                if (pf.InitDataList?.InitDatas != null)
                                {
                                    foreach (var initData in pf.InitDataList.InitDatas)
                                    {
                                        var name = initData.Name.ToLowerInvariant();
                                        var hash = JenkHash.GenHash(name);
                                        if (allPeds.ContainsKey(hash))
                                        { }
                                        allPeds[hash] = initData;
                                    }
                                }
                                allPedsFiles.Add(pf);
                            }
                        }
#if !DEBUG
                        catch (Exception ex)
                        {
                            string errstr = entry.Path + "\n" + ex.ToString();
                            ErrorLog(errstr);
                        }
#endif
                    }
                }
            });

            var addPedFiles = new Action<IEnumerable<RpfFile>>((from) =>
            {
                foreach (RpfFile file in from)
                {
                    if (file.AllEntries == null) continue;
                    foreach (RpfEntry entry in file.AllEntries)
                    {
#if !DEBUG
                        try
#endif
                        {
                            if (entry.NameLower.EndsWith(".ymt"))
                            {
                                var testname = entry.GetShortNameLower();
                                var testhash = JenkHash.GenHash(testname);
                                if (allPeds.ContainsKey(testhash))
                                {
                                    var pf = RpfMan.GetFile<PedFile>(entry);
                                    if (pf != null)
                                    {
                                        allPedYmts[testhash] = pf;
                                        addPedDicts(testname, testhash, entry.Parent);
                                    }
                                }
                            }
                        }
#if !DEBUG
                        catch (Exception ex)
                        {
                            string errstr = entry.Path + "\n" + ex.ToString();
                            ErrorLog(errstr);
                        }
#endif
                    }
                }
            });



            addPedsFiles(rpfs);
            addPedsFiles(dlcrpfs);

            addPedFiles(rpfs);
            addPedFiles(dlcrpfs);



            PedsInitDict = allPeds;
            PedVariationsDict = allPedYmts;
            PedDrawableDicts = allPedDrwDicts;
            PedTextureDicts = allPedTexDicts;
            PedClothDicts = allPedClothDicts;


            foreach (var kvp in PedsInitDict)
            {
                if (!PedVariationsDict.ContainsKey(kvp.Key))
                { }//checking we found them all!
            }


        }

        public void InitAudio()
        {
            if (!LoadAudio) return;

            Dictionary<uint, RpfFileEntry> datrelentries = new Dictionary<uint, RpfFileEntry>();
            void addRpfDatRelEntries(RpfFile rpffile)
            {
                if (rpffile.AllEntries == null) return;
                foreach (var entry in rpffile.AllEntries)
                {
                    if (entry is RpfFileEntry)
                    {
                        RpfFileEntry fentry = entry as RpfFileEntry;
                        if (entry.NameLower.EndsWith(".rel"))
                        {
                            datrelentries[entry.NameHash] = fentry;
                        }
                    }
                }
            }

            var audrpf = RpfMan.FindRpfFile("x64\\audio\\audio_rel.rpf");
            if (audrpf != null)
            {
                addRpfDatRelEntries(audrpf);
            }

            if (EnableDlc)
            {
                var updrpf = RpfMan.FindRpfFile("update\\update.rpf");
                if (updrpf != null)
                {
                    addRpfDatRelEntries(updrpf);
                }
                foreach (var dlcrpf in DlcActiveRpfs) //load from current dlc rpfs
                {
                    addRpfDatRelEntries(dlcrpf);
                }
                if (DlcActiveRpfs.Count == 0) //when activated from RPF explorer... DLCs aren't initialised fully
                {
                    foreach (var rpf in AllRpfs) //this is a bit of a hack - DLC orders won't be correct so likely will select wrong versions of things
                    {
                        if (rpf.NameLower.StartsWith("dlc"))
                        {
                            addRpfDatRelEntries(rpf);
                        }
                    }
                }
            }


            var audioDatRelFiles = new List<RelFile>();
            var audioConfigDict = new Dictionary<MetaHash, RelData>();
            var audioSpeechDict = new Dictionary<MetaHash, RelData>();
            var audioSynthsDict = new Dictionary<MetaHash, RelData>();
            var audioMixersDict = new Dictionary<MetaHash, RelData>();
            var audioCurvesDict = new Dictionary<MetaHash, RelData>();
            var audioCategsDict = new Dictionary<MetaHash, RelData>();
            var audioSoundsDict = new Dictionary<MetaHash, RelData>();
            var audioGameDict = new Dictionary<MetaHash, RelData>();



            foreach (var datrelentry in datrelentries.Values)
            {
                var relfile = RpfMan.GetFile<RelFile>(datrelentry);
                if (relfile == null) continue;

                audioDatRelFiles.Add(relfile);

                var d = audioGameDict;
                var t = relfile.RelType;
                switch (t)
                {
                    case RelDatFileType.Dat4: 
                        d = relfile.IsAudioConfig ? audioConfigDict : audioSpeechDict; 
                        break;
                    case RelDatFileType.Dat10ModularSynth:
                        d = audioSynthsDict;
                        break;
                    case RelDatFileType.Dat15DynamicMixer:
                        d = audioMixersDict;
                        break;
                    case RelDatFileType.Dat16Curves:
                        d = audioCurvesDict;
                        break;
                    case RelDatFileType.Dat22Categories:
                        d = audioCategsDict;
                        break;
                    case RelDatFileType.Dat54DataEntries:
                        d = audioSoundsDict;
                        break;
                    case RelDatFileType.Dat149:
                    case RelDatFileType.Dat150:
                    case RelDatFileType.Dat151:
                    default:
                        d = audioGameDict;
                        break;
                }

                foreach (var reldata in relfile.RelDatas)
                {
                    if (reldata.NameHash == 0) continue;
                    //if (d.TryGetValue(reldata.NameHash, out var exdata) && (exdata.TypeID != reldata.TypeID))
                    //{ }//sanity check
                    d[reldata.NameHash] = reldata;
                }

            }




            AudioDatRelFiles = audioDatRelFiles;
            AudioConfigDict = audioConfigDict;
            AudioSpeechDict = audioSpeechDict;
            AudioSynthsDict = audioSynthsDict;
            AudioMixersDict = audioMixersDict;
            AudioCurvesDict = audioCurvesDict;
            AudioCategsDict = audioCategsDict;
            AudioSoundsDict = audioSoundsDict;
            AudioGameDict = audioGameDict;

        }

        public void TryLoadEnqueue(GameFile gf)
        {
            if (((!gf.Loaded)) && (requestQueue.Count < 10))// && (!gf.LoadQueued)
            {
                requestQueue.Enqueue(gf);
                gf.LoadQueued = true;
            }
        }

        public Archetype GetArchetype(uint hash)
        {
            if (!archetypesLoaded) return null;
            Archetype arch = null;
            projectArchetypes.TryGetValue(hash, out arch);
            if (arch != null) return arch;
            archetypeDict.TryGetValue(hash, out arch);
            return arch;
        }
        public MapDataStoreNode GetMapNode(uint hash)
        {
            if (!IsInited) return null;
            MapDataStoreNode node = null;
            YmapHierarchyDict.TryGetValue(hash, out node);
            return node;
        }

        public YdrFile GetYdr(uint hash)
        {
            if (!IsInited) return null;
            lock (requestSyncRoot)
            {
                var key = new GameFileCacheKey(hash, GameFileType.Ydr);
                if (projectFiles.TryGetValue(key, out GameFile pgf))
                {
                    return pgf as YdrFile;
                }
                YdrFile ydr = mainCache.TryGet(key) as YdrFile;
                if (ydr == null)
                {
                    var e = GetYdrEntry(hash);
                    if (e != null)
                    {
                        ydr = new YdrFile(e);
                        if (mainCache.TryAdd(key, ydr))
                        {
                            TryLoadEnqueue(ydr);
                        }
                        else
                        {
                            ydr.LoadQueued = false;
                            //ErrorLog("Out of cache space - couldn't load drawable: " + JenkIndex.GetString(hash)); //too spammy...
                        }
                    }
                    else
                    {
                        //ErrorLog("Drawable not found: " + JenkIndex.GetString(hash)); //too spammy...
                    }
                }
                else if (!ydr.Loaded)
                {
                    TryLoadEnqueue(ydr);
                }
                return ydr;
            }
        }
        public YddFile GetYdd(uint hash)
        {
            if (!IsInited) return null;
            lock (requestSyncRoot)
            {
                var key = new GameFileCacheKey(hash, GameFileType.Ydd);
                if (projectFiles.TryGetValue(key, out GameFile pgf))
                {
                    return pgf as YddFile;
                }
                YddFile ydd = mainCache.TryGet(key) as YddFile;
                if (ydd == null)
                {
                    var e = GetYddEntry(hash);
                    if (e != null)
                    {
                        ydd = new YddFile(e);
                        if (mainCache.TryAdd(key, ydd))
                        {
                            TryLoadEnqueue(ydd);
                        }
                        else
                        {
                            ydd.LoadQueued = false;
                            //ErrorLog("Out of cache space - couldn't load drawable dictionary: " + JenkIndex.GetString(hash)); //too spammy...
                        }
                    }
                    else
                    {
                        //ErrorLog("Drawable dictionary not found: " + JenkIndex.GetString(hash)); //too spammy...
                    }
                }
                else if (!ydd.Loaded)
                {
                    TryLoadEnqueue(ydd);
                }
                return ydd;
            }
        }
        public YtdFile GetYtd(uint hash)
        {
            if (!IsInited) return null;
            lock (requestSyncRoot)
            {
                var key = new GameFileCacheKey(hash, GameFileType.Ytd);
                if (projectFiles.TryGetValue(key, out GameFile pgf))
                {
                    return pgf as YtdFile;
                }
                YtdFile ytd = mainCache.TryGet(key) as YtdFile;
                if (ytd == null)
                {
                    var e = GetYtdEntry(hash);
                    if (e != null)
                    {
                        ytd = new YtdFile(e);
                        if (mainCache.TryAdd(key, ytd))
                        {
                            TryLoadEnqueue(ytd);
                        }
                        else
                        {
                            ytd.LoadQueued = false;
                            //ErrorLog("Out of cache space - couldn't load texture dictionary: " + JenkIndex.GetString(hash)); //too spammy...
                        }
                    }
                    else
                    {
                        //ErrorLog("Texture dictionary not found: " + JenkIndex.GetString(hash)); //too spammy...
                    }
                }
                else if (!ytd.Loaded)
                {
                    TryLoadEnqueue(ytd);
                }
                return ytd;
            }
        }
        public YmapFile GetYmap(uint hash)
        {
            if (!IsInited) return null;
            lock (requestSyncRoot)
            {
                var key = new GameFileCacheKey(hash, GameFileType.Ymap);
                YmapFile ymap = mainCache.TryGet(key) as YmapFile;
                if (ymap == null)
                {
                    var e = GetYmapEntry(hash);
                    if (e != null)
                    {
                        ymap = new YmapFile(e);
                        if (mainCache.TryAdd(key, ymap))
                        {
                            TryLoadEnqueue(ymap);
                        }
                        else
                        {
                            ymap.LoadQueued = false;
                            //ErrorLog("Out of cache space - couldn't load ymap: " + JenkIndex.GetString(hash));
                        }
                    }
                    else
                    {
                        //ErrorLog("Ymap not found: " + JenkIndex.GetString(hash)); //too spammy...
                    }
                }
                else if (!ymap.Loaded)
                {
                    TryLoadEnqueue(ymap);
                }
                return ymap;
            }
        }
        public YftFile GetYft(uint hash)
        {
            if (!IsInited) return null;
            lock (requestSyncRoot)
            {
                var key = new GameFileCacheKey(hash, GameFileType.Yft);
                YftFile yft = mainCache.TryGet(key) as YftFile;
                if (projectFiles.TryGetValue(key, out GameFile pgf))
                {
                    return pgf as YftFile;
                }
                if (yft == null)
                {
                    var e = GetYftEntry(hash);
                    if (e != null)
                    {
                        yft = new YftFile(e);
                        if (mainCache.TryAdd(key, yft))
                        {
                            TryLoadEnqueue(yft);
                        }
                        else
                        {
                            yft.LoadQueued = false;
                            //ErrorLog("Out of cache space - couldn't load yft: " + JenkIndex.GetString(hash)); //too spammy...
                        }
                    }
                    else
                    {
                        //ErrorLog("Yft not found: " + JenkIndex.GetString(hash)); //too spammy...
                    }
                }
                else if (!yft.Loaded)
                {
                    TryLoadEnqueue(yft);
                }
                return yft;
            }
        }
        public YbnFile GetYbn(uint hash)
        {
            if (!IsInited) return null;
            lock (requestSyncRoot)
            {
                var key = new GameFileCacheKey(hash, GameFileType.Ybn);
                YbnFile ybn = mainCache.TryGet(key) as YbnFile;
                if (ybn == null)
                {
                    var e = GetYbnEntry(hash);
                    if (e != null)
                    {
                        ybn = new YbnFile(e);
                        if (mainCache.TryAdd(key, ybn))
                        {
                            TryLoadEnqueue(ybn);
                        }
                        else
                        {
                            ybn.LoadQueued = false;
                            //ErrorLog("Out of cache space - couldn't load ybn: " + JenkIndex.GetString(hash)); //too spammy...
                        }
                    }
                    else
                    {
                        //ErrorLog("Ybn not found: " + JenkIndex.GetString(hash)); //too spammy...
                    }
                }
                else if (!ybn.Loaded)
                {
                    TryLoadEnqueue(ybn);
                }
                return ybn;
            }
        }
        public YcdFile GetYcd(uint hash)
        {
            if (!IsInited) return null;
            lock (requestSyncRoot)
            {
                var key = new GameFileCacheKey(hash, GameFileType.Ycd);
                YcdFile ycd = mainCache.TryGet(key) as YcdFile;
                if (ycd == null)
                {
                    var e = GetYcdEntry(hash);
                    if (e != null)
                    {
                        ycd = new YcdFile(e);
                        if (mainCache.TryAdd(key, ycd))
                        {
                            TryLoadEnqueue(ycd);
                        }
                        else
                        {
                            ycd.LoadQueued = false;
                            //ErrorLog("Out of cache space - couldn't load ycd: " + JenkIndex.GetString(hash)); //too spammy...
                        }
                    }
                    else
                    {
                        //ErrorLog("Ycd not found: " + JenkIndex.GetString(hash)); //too spammy...
                    }
                }
                else if (!ycd.Loaded)
                {
                    TryLoadEnqueue(ycd);
                }
                return ycd;
            }
        }
        public YedFile GetYed(uint hash)
        {
            if (!IsInited) return null;
            lock (requestSyncRoot)
            {
                var key = new GameFileCacheKey(hash, GameFileType.Yed);
                YedFile yed = mainCache.TryGet(key) as YedFile;
                if (yed == null)
                {
                    var e = GetYedEntry(hash);
                    if (e != null)
                    {
                        yed = new YedFile(e);
                        if (mainCache.TryAdd(key, yed))
                        {
                            TryLoadEnqueue(yed);
                        }
                        else
                        {
                            yed.LoadQueued = false;
                            //ErrorLog("Out of cache space - couldn't load yed: " + JenkIndex.GetString(hash)); //too spammy...
                        }
                    }
                    else
                    {
                        //ErrorLog("Yed not found: " + JenkIndex.GetString(hash)); //too spammy...
                    }
                }
                else if (!yed.Loaded)
                {
                    TryLoadEnqueue(yed);
                }
                return yed;
            }
        }
        public YnvFile GetYnv(uint hash)
        {
            if (!IsInited) return null;
            lock (requestSyncRoot)
            {
                var key = new GameFileCacheKey(hash, GameFileType.Ynv);
                YnvFile ynv = mainCache.TryGet(key) as YnvFile;
                if (ynv == null)
                {
                    var e = GetYnvEntry(hash);
                    if (e != null)
                    {
                        ynv = new YnvFile(e);
                        if (mainCache.TryAdd(key, ynv))
                        {
                            TryLoadEnqueue(ynv);
                        }
                        else
                        {
                            ynv.LoadQueued = false;
                            //ErrorLog("Out of cache space - couldn't load ycd: " + JenkIndex.GetString(hash)); //too spammy...
                        }
                    }
                    else
                    {
                        //ErrorLog("Ycd not found: " + JenkIndex.GetString(hash)); //too spammy...
                    }
                }
                else if (!ynv.Loaded)
                {
                    TryLoadEnqueue(ynv);
                }
                return ynv;
            }
        }


        public RpfFileEntry GetYdrEntry(uint hash)
        {
            RpfFileEntry entry;
            YdrDict.TryGetValue(hash, out entry);
            return entry;
        }
        public RpfFileEntry GetYddEntry(uint hash)
        {
            RpfFileEntry entry;
            YddDict.TryGetValue(hash, out entry);
            return entry;
        }
        public RpfFileEntry GetYtdEntry(uint hash)
        {
            RpfFileEntry entry;
            YtdDict.TryGetValue(hash, out entry);
            return entry;
        }
        public RpfFileEntry GetYmapEntry(uint hash)
        {
            RpfFileEntry entry;
            if (!YmapDict.TryGetValue(hash, out entry))
            {
                AllYmapsDict.TryGetValue(hash, out entry);
            }
            return entry;
        }
        public RpfFileEntry GetYftEntry(uint hash)
        {
            RpfFileEntry entry;
            YftDict.TryGetValue(hash, out entry);
            return entry;
        }
        public RpfFileEntry GetYbnEntry(uint hash)
        {
            RpfFileEntry entry;
            YbnDict.TryGetValue(hash, out entry);
            return entry;
        }
        public RpfFileEntry GetYcdEntry(uint hash)
        {
            RpfFileEntry entry;
            YcdDict.TryGetValue(hash, out entry);
            return entry;
        }
        public RpfFileEntry GetYedEntry(uint hash)
        {
            RpfFileEntry entry;
            YedDict.TryGetValue(hash, out entry);
            return entry;
        }
        public RpfFileEntry GetYnvEntry(uint hash)
        {
            RpfFileEntry entry;
            YnvDict.TryGetValue(hash, out entry);
            return entry;
        }



        public bool LoadFile<T>(T file) where T : GameFile, PackedFile
        {
            if (file == null) return false;
            RpfFileEntry entry = file.RpfFileEntry;
            if (entry != null)
            {
                return RpfMan.LoadFile(file, entry);
            }
            return false;
        }


        public T GetFileUncached<T>(RpfFileEntry e) where T : GameFile, new()
        {
            var f = new T();
            f.RpfFileEntry = e;
            TryLoadEnqueue(f);
            return f;
        }


        public void BeginFrame()
        {
            lock (requestSyncRoot)
            {
                mainCache.BeginFrame();
            }
        }


        public bool ContentThreadProc()
        {
            Monitor.Enter(updateSyncRoot);

            GameFile req;
            //bool loadedsomething = false;

            int itemcount = 0;

            while (requestQueue.TryDequeue(out req) && (itemcount < MaxItemsPerLoop))
            {
                //process content requests.
                if (req.Loaded)
                    continue; //it's already loaded... (somehow)

                if ((req.LastUseTime - DateTime.Now).TotalSeconds > 0.5)
                    continue; //hasn't been requested lately..! ignore, will try again later if necessary

                itemcount++;
                //if (!loadedsomething)
                //{
                //UpdateStatus("Loading " + req.RpfFileEntry.Name + "...");
                //}

                switch (req.Type)
                {
                    case GameFileType.Ydr:
                        req.Loaded = LoadFile(req as YdrFile);
                        break;
                    case GameFileType.Ydd:
                        req.Loaded = LoadFile(req as YddFile);
                        break;
                    case GameFileType.Ytd:
                        req.Loaded = LoadFile(req as YtdFile);
                        //if (req.Loaded) AddTextureLookups(req as YtdFile);
                        break;
                    case GameFileType.Ymap:
                        YmapFile y = req as YmapFile;
                        req.Loaded = LoadFile(y);
                        if (req.Loaded) y.InitYmapEntityArchetypes(this);
                        break;
                    case GameFileType.Yft:
                        req.Loaded = LoadFile(req as YftFile);
                        break;
                    case GameFileType.Ybn:
                        req.Loaded = LoadFile(req as YbnFile);
                        break;
                    case GameFileType.Ycd:
                        req.Loaded = LoadFile(req as YcdFile);
                        break;
                    case GameFileType.Yed:
                        req.Loaded = LoadFile(req as YedFile);
                        break;
                    case GameFileType.Ynv:
                        req.Loaded = LoadFile(req as YnvFile);
                        break;
                    case GameFileType.Yld:
                        req.Loaded = LoadFile(req as YldFile);
                        break;
                    default:
                        break;
                }

                string str = (req.Loaded ? "Loaded " : "Error loading ") + req.ToString();
                //string str = string.Format("{0}: {1}: {2}", requestQueue.Count, (req.Loaded ? "Loaded" : "Error loading"), req);

                UpdateStatus(str);
                //ErrorLog(str);
                if (!req.Loaded)
                {
                    ErrorLog("Error loading " + req.ToString());
                }


                //loadedsomething = true;
            }

            //whether or not we need another content thread loop
            bool itemsStillPending = (itemcount >= MaxItemsPerLoop);


            Monitor.Exit(updateSyncRoot);


            return itemsStillPending;
        }






        private void AddTextureLookups(YtdFile ytd)
        {
            if (ytd?.TextureDict?.TextureNameHashes?.data_items == null) return;

            lock (textureSyncRoot)
            {
                foreach (uint hash in ytd.TextureDict.TextureNameHashes.data_items)
                {
                    textureLookup[hash] = ytd.RpfFileEntry;
                }

            }
        }
        public YtdFile TryGetTextureDictForTexture(uint hash)
        {
            lock (textureSyncRoot)
            {
                RpfFileEntry e;
                if (textureLookup.TryGetValue(hash, out e))
                {
                    return GetYtd(e.ShortNameHash);
                }

            }
            return null;
        }
        public YtdFile TryGetParentYtd(uint hash)
        {
            MetaHash phash;
            if (textureParents.TryGetValue(hash, out phash))
            {
                return GetYtd(phash);
            }
            return null;
        }
        public uint TryGetParentYtdHash(uint hash)
        {
            MetaHash phash = 0;
            textureParents.TryGetValue(hash, out phash);
            return phash;
        }
        public uint TryGetHDTextureHash(uint txdhash)
        {
            MetaHash hdhash = 0;
            if (hdtexturelookup?.TryGetValue(txdhash, out hdhash) ?? false)
            {
                return hdhash;
            }
            return txdhash;
        }

        public Texture TryFindTextureInParent(uint texhash, uint txdhash)
        {
            Texture tex = null;

            var ytd = TryGetParentYtd(txdhash);
            while ((ytd != null) && (tex == null))
            {
                if (ytd.Loaded && (ytd.TextureDict != null))
                {
                    tex = ytd.TextureDict.Lookup(texhash);
                }
                if (tex == null)
                {
                    ytd = TryGetParentYtd(ytd.Key.Hash);
                }
            }

            return tex;
        }








        public DrawableBase TryGetDrawable(Archetype arche)
        {
            if (arche == null) return null;
            uint drawhash = arche.Hash;
            DrawableBase drawable = null;
            if ((arche.DrawableDict != 0))// && (arche.DrawableDict != arche.Hash))
            {
                //try get drawable from ydd...
                YddFile ydd = GetYdd(arche.DrawableDict);
                if (ydd != null)
                {
                    if (ydd.Loaded && (ydd.Dict != null))
                    {
                        Drawable d;
                        ydd.Dict.TryGetValue(drawhash, out d); //can't out to base class?
                        drawable = d;
                        if (drawable == null)
                        {
                            return null; //drawable wasn't in dict!!
                        }
                    }
                    else
                    {
                        return null; //ydd not loaded yet, or has no dict
                    }
                }
                else
                {
                    //return null; //couldn't find drawable dict... quit now?
                }
            }
            if (drawable == null)
            {
                //try get drawable from ydr.
                YdrFile ydr = GetYdr(drawhash);
                if (ydr != null)
                {
                    if (ydr.Loaded)
                    {
                        drawable = ydr.Drawable;
                    }
                }
                else
                {
                    YftFile yft = GetYft(drawhash);
                    if (yft != null)
                    {
                        if (yft.Loaded)
                        {
                            if (yft.Fragment != null)
                            {
                                drawable = yft.Fragment.Drawable;
                            }
                        }
                    }
                }
            }

            return drawable;
        }

        public DrawableBase TryGetDrawable(Archetype arche, out bool waitingForLoad)
        {
            waitingForLoad = false;
            if (arche == null) return null;
            uint drawhash = arche.Hash;
            DrawableBase drawable = null;
            if ((arche.DrawableDict != 0))// && (arche.DrawableDict != arche.Hash))
            {
                //try get drawable from ydd...
                YddFile ydd = GetYdd(arche.DrawableDict);
                if (ydd != null)
                {
                    if (ydd.Loaded)
                    {
                        if (ydd.Dict != null)
                        {
                            Drawable d;
                            ydd.Dict.TryGetValue(drawhash, out d); //can't out to base class?
                            drawable = d;
                            if (drawable == null)
                            {
                                return null; //drawable wasn't in dict!!
                            }
                        }
                        else
                        {
                            return null; //ydd has no dict
                        }
                    }
                    else
                    {
                        waitingForLoad = true;
                        return null; //ydd not loaded yet
                    }
                }
                else
                {
                    //return null; //couldn't find drawable dict... quit now?
                }
            }
            if (drawable == null)
            {
                //try get drawable from ydr.
                YdrFile ydr = GetYdr(drawhash);
                if (ydr != null)
                {
                    if (ydr.Loaded)
                    {
                        drawable = ydr.Drawable;
                    }
                    else
                    {
                        waitingForLoad = true;
                    }
                }
                else
                {
                    YftFile yft = GetYft(drawhash);
                    if (yft != null)
                    {
                        if (yft.Loaded)
                        {
                            if (yft.Fragment != null)
                            {
                                drawable = yft.Fragment.Drawable;
                            }
                        }
                        else
                        {
                            waitingForLoad = true;
                        }
                    }
                }
            }

            return drawable;
        }

        private string[] GetExcludePaths()
        {
            string[] exclpaths = ExcludeFolders.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            if (exclpaths.Length > 0)
            {
                for (int i = 0; i < exclpaths.Length; i++)
                {
                    exclpaths[i] = exclpaths[i].ToLowerInvariant();
                }
            }
            else
            {
                exclpaths = null;
            }
            return exclpaths;
        }


        private class ShaderXmlDataCollection
        {
            public MetaHash Name { get; set; }
            public Dictionary<MetaHash, int> FileNames { get; set; } = new Dictionary<MetaHash, int>();
            public Dictionary<byte, int> RenderBuckets { get; set; } = new Dictionary<byte, int>();
            public Dictionary<ShaderXmlVertexLayout, int> VertexLayouts { get; set; } = new Dictionary<ShaderXmlVertexLayout, int>();
            public Dictionary<MetaName, int> TexParams { get; set; } = new Dictionary<MetaName, int>();
            public Dictionary<MetaName, Dictionary<Vector4, int>> ValParams { get; set; } = new Dictionary<MetaName, Dictionary<Vector4, int>>();
            public Dictionary<MetaName, List<Vector4[]>> ArrParams { get; set; } = new Dictionary<MetaName, List<Vector4[]>>();
            public int GeomCount { get; set; } = 0;


            public void AddShaderUse(ShaderFX s, DrawableGeometry g)
            {
                GeomCount++;

                AddItem(s.FileName, FileNames);
                AddItem(s.RenderBucket, RenderBuckets);

                var info = g.VertexBuffer?.Info;
                if (info != null)
                {
                    AddItem(new ShaderXmlVertexLayout() { Flags = info.Flags, Types = info.Types }, VertexLayouts);
                }

                if (s.ParametersList?.Parameters == null) return;
                if (s.ParametersList?.Hashes == null) return;

                for (int i = 0; i < s.ParametersList.Count; i++)
                {
                    var h = s.ParametersList.Hashes[i];
                    var p = s.ParametersList.Parameters[i];

                    if (p.DataType == 0)//texture
                    {
                        AddItem(h, TexParams);
                    }
                    else if (p.DataType == 1)//vector
                    {
                        var vp = GetItem(h, ValParams);
                        if (p.Data is Vector4 vec)
                        {
                            AddItem(vec, vp);
                        }
                    }
                    else if (p.DataType > 1)//array
                    {
                        var ap = GetItem(h, ArrParams);
                        if (p.Data is Vector4[] arr)
                        {
                            bool found = false;
                            foreach (var exarr in ap)
                            {
                                if (exarr.Length != arr.Length) continue;
                                bool match = true;
                                for (int j = 0; j < exarr.Length; j++)
                                {
                                    if (exarr[j] != arr[j])
                                    {
                                        match = false;
                                        break;
                                    }
                                }
                                if (match)
                                {
                                    found = true;
                                    break;
                                }
                            }
                            if (!found)
                            {
                                ap.Add(arr);
                            }
                        }
                    }
                }

            }
            public void AddItem<T>(T t, Dictionary<T, int> d)
            {
                if (d.ContainsKey(t))
                {
                    d[t]++;
                }
                else
                {
                    d[t] = 1;
                }
            }
            public U GetItem<T, U>(T t, Dictionary<T, U> d) where U:new()
            {
                U r = default(U);
                if (!d.TryGetValue(t, out r))
                {
                    r = new U();
                    d[t] = r;
                }
                return r;
            }
            public List<T> GetSortedList<T>(Dictionary<T, int> d)
            {
                var kvps = d.ToList();
                kvps.Sort((a, b) => { return b.Value.CompareTo(a.Value); });
                return kvps.Select((a) => { return a.Key; }).ToList();
            }
        }
        private struct ShaderXmlVertexLayout
        {
            public VertexDeclarationTypes Types { get; set; }
            public uint Flags { get; set; }
            public VertexType VertexType { get { return (VertexType)Flags; } }
            public override string ToString()
            {
                return Types.ToString() + ", " + VertexType.ToString();
            }
        }
    }


}
