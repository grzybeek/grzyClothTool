using CodeWalker.GameFiles;
using SharpDX;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CodeWalker.World
{
    [TypeConverter(typeof(ExpandableObjectConverter))] public class Ped
    {
        private const int FileLoadTimeoutMs = 2000;
        public string Name { get; set; } = string.Empty;
        public MetaHash NameHash { get; set; } = 0;//ped name hash
        public CPedModelInfo__InitData InitData { get; set; } = null; //ped init data
        public YddFile Ydd { get; set; } = null; //ped drawables
        public YtdFile Ytd { get; set; } = null; //ped textures
        public YldFile Yld { get; set; } = null; //ped clothes
        public YcdFile Ycd { get; set; } = null; //ped animations
        public YedFile Yed { get; set; } = null; //ped expressions
        public YftFile Yft { get; set; } = null; //ped skeleton YFT
        public PedFile Ymt { get; set; } = null; //ped variation info
        public Dictionary<MetaHash, RpfFileEntry> DrawableFilesDict { get; set; } = null;
        public Dictionary<MetaHash, RpfFileEntry> TextureFilesDict { get; set; } = null;
        public Dictionary<MetaHash, RpfFileEntry> ClothFilesDict { get; set; } = null;
        public RpfFileEntry[] DrawableFiles { get; set; } = null;
        public RpfFileEntry[] TextureFiles { get; set; } = null;
        public RpfFileEntry[] ClothFiles { get; set; } = null;
        public ClipMapEntry AnimClip { get; set; } = null;
        public Expression Expression { get; set; } = null;
        public string[] DrawableNames { get; set; } = new string[12];
        public Drawable[] Drawables { get; set; } = new Drawable[12];
        public Texture[] Textures { get; set; } = new Texture[12];
        public Expression[] Expressions { get; set; } = new Expression[12];
        public ClothInstance[] Clothes { get; set; } = new ClothInstance[12];
        public bool EnableRootMotion { get; set; } = false; //used to toggle whether or not to include root motion when playing animations
        public Skeleton Skeleton { get; set; } = null;

        public Vector3 Position { get; set; } = Vector3.Zero;
        public Quaternion Rotation { get; set; } = Quaternion.Identity;

        public YmapEntityDef RenderEntity = new YmapEntityDef(); //placeholder entity object for rendering


        public void Init(string name, GameFileCache gfc)
        {
            var hash = JenkHash.GenHash(name.ToLowerInvariant());
            Init(hash, gfc);
            Name = name;
        }
        public void Init(MetaHash pedhash, GameFileCache gfc)
        {

            Name = string.Empty;
            NameHash = 0;
            InitData = null;
            Ydd = null;
            Ytd = null;
            Yld = null;
            Ycd = null;
            Yed = null;
            Yft = null;
            Ymt = null;
            AnimClip = null;
            for (int i = 0; i < 12; i++)
            {
                Drawables[i] = null;
                Textures[i] = null;
                Expressions[i] = null;
            }


            CPedModelInfo__InitData initdata = null;
            if (gfc.PedsInitDict == null) return;
            if (!gfc.PedsInitDict.TryGetValue(pedhash, out initdata)) return;

            var ycdhash = JenkHash.GenHash(initdata.ClipDictionaryName.ToLowerInvariant());
            var yedhash = JenkHash.GenHash(initdata.ExpressionDictionaryName.ToLowerInvariant());

            //bool pedchange = NameHash != pedhash;
            //Name = pedname;
            NameHash = pedhash;
            InitData = initdata;
            Ydd = gfc.GetYdd(pedhash);
            Ytd = gfc.GetYtd(pedhash);
            //Ycd = gfc.GetYcd(ycdhash);
            Yed = gfc.GetYed(yedhash);
            Yft = gfc.GetYft(pedhash);

            PedFile pedFile = null;
            gfc.PedVariationsDict?.TryGetValue(pedhash, out pedFile);
            Ymt = pedFile;

            Dictionary<MetaHash, RpfFileEntry> peddict = null;
            gfc.PedDrawableDicts.TryGetValue(NameHash, out peddict);
            DrawableFilesDict = peddict;
            DrawableFiles = DrawableFilesDict?.Values.ToArray();
            gfc.PedTextureDicts.TryGetValue(NameHash, out peddict);
            TextureFilesDict = peddict;
            TextureFiles = TextureFilesDict?.Values.ToArray();
            gfc.PedClothDicts.TryGetValue(NameHash, out peddict);
            ClothFilesDict = peddict;
            ClothFiles = ClothFilesDict?.Values.ToArray();

            RpfFileEntry clothFile = null;
            if (ClothFilesDict?.TryGetValue(pedhash, out clothFile) ?? false)
            {
                Yld = gfc.GetFileUncached<YldFile>(clothFile);
                if (!WaitForFileLoad(Yld, () => gfc.TryLoadEnqueue(Yld)))
                {
                    Yld = null;
                }
            }



            if (!WaitForFileLoad(Ydd, () => Ydd = gfc.GetYdd(pedhash)))
            {
                Ydd = null;
            }
            if (!WaitForFileLoad(Ytd, () => Ytd = gfc.GetYtd(pedhash)))
            {
                Ytd = null;
            }
            //if (!WaitForFileLoad(Ycd, () => Ycd = gfc.GetYcd(ycdhash)))
            //{
            //    Ycd = null;
            //}
            if (!WaitForFileLoad(Yed, () => Yed = gfc.GetYed(yedhash)))
            {
                Yed = null;
            }
            if (!WaitForFileLoad(Yft, () => Yft = gfc.GetYft(pedhash)))
            {
                Yft = null;
            }


            Skeleton = Yft?.Fragment?.Drawable?.Skeleton?.Clone();

            //MetaHash cliphash = JenkHash.GenHash("idle");
            //ClipMapEntry cme = null;
            //Ycd?.ClipMap?.TryGetValue(cliphash, out cme);
            //AnimClip = cme;

            var exprhash = JenkHash.GenHash(initdata.ExpressionName.ToLowerInvariant());
            Expression expr = null;
            Yed?.ExprMap?.TryGetValue(exprhash, out expr);
            Expression = expr;


            UpdateEntity();
        }


        public void SetComponentDrawable(int index, string name, string tex, GameFileCache gfc)
        {
            if (string.IsNullOrEmpty(name))
            {
                DrawableNames[index] = null;
                Drawables[index] = null;
                Textures[index] = null;
                Expressions[index] = null;
                return;
            }

            MetaHash namehash = JenkHash.GenHash(name.ToLowerInvariant());
            Drawable d = null;
            if (Ydd?.Dict != null)
            {
                Ydd.Dict.TryGetValue(namehash, out d);
            }
            if ((d == null) && (DrawableFilesDict != null))
            {
                RpfFileEntry file = null;
                if (DrawableFilesDict.TryGetValue(namehash, out file))
                {
                    var ydd = gfc.GetFileUncached<YddFile>(file);
                    if (WaitForFileLoad(ydd, () => gfc.TryLoadEnqueue(ydd)))
                    {
                        if (ydd?.Drawables?.Length > 0)
                        {
                            d = ydd.Drawables[0];//should only be one in this dict
                        }
                    }
                }
            }

            MetaHash texhash = JenkHash.GenHash(tex.ToLowerInvariant());
            Texture t = null;
            if (Ytd?.TextureDict?.Dict != null)
            {
                Ytd.TextureDict.Dict.TryGetValue(texhash, out t);
            }
            if ((t == null) && (TextureFilesDict != null))
            {
                RpfFileEntry file = null;
                if (TextureFilesDict.TryGetValue(texhash, out file))
                {
                    var ytd = gfc.GetFileUncached<YtdFile>(file);
                    if (WaitForFileLoad(ytd, () => gfc.TryLoadEnqueue(ytd)))
                    {
                        if (ytd?.TextureDict?.Textures?.data_items.Length > 0)
                        {
                            t = ytd.TextureDict.Textures.data_items[0];//should only be one in this dict
                        }
                    }
                }
            }

            CharacterCloth cc = null;
            if (Yld?.Dict != null)
            {
                Yld.Dict.TryGetValue(namehash, out cc);
            }
            if ((cc == null) && (ClothFilesDict != null))
            {
                RpfFileEntry file = null;
                if (ClothFilesDict.TryGetValue(namehash, out file))
                {
                    var yld = gfc.GetFileUncached<YldFile>(file);
                    if (WaitForFileLoad(yld, () => gfc.TryLoadEnqueue(yld)))
                    {
                        if (yld?.ClothDictionary?.Clothes?.data_items?.Length > 0)
                        {
                            cc = yld.ClothDictionary.Clothes.data_items[0];//should only be one in this dict
                        }
                    }
                }
            }
            ClothInstance c = null;
            if (cc != null)
            {
                c = new ClothInstance();
                c.Init(cc, Skeleton);
            }

            Expression e = null;
            if (Yed?.ExprMap != null)
            {
                Yed.ExprMap.TryGetValue(namehash, out e);
            }


            if (d != null)
            {
                Drawables[index] = d.ShallowCopy() as Drawable;
            }
            if (t != null) Textures[index] = t;
            if (c != null) Clothes[index] = c;
            if (e != null) Expressions[index] = e;

            DrawableNames[index] = name;
        }

        public void SetComponentDrawable(int index, int drawbl, int alt, int tex, GameFileCache gfc)
        {
            var vi = Ymt?.VariationInfo;
            if (vi != null)
            {
                var compData = vi.GetComponentData(index);
                if (compData?.DrawblData3 != null)
                {
                    var item = (drawbl < (compData.DrawblData3?.Length ?? 0)) ? compData.DrawblData3[drawbl] : null;
                    if (item != null)
                    {
                        var name = item?.GetDrawableName(alt);
                        var texn = item?.GetTextureName(tex);
                        SetComponentDrawable(index, name, texn, gfc);
                    }
                }
            }
        }

        public void LoadDefaultComponents(GameFileCache gfc)
        {
            for (int i = 0; i < 12; i++)
            {
                SetComponentDrawable(i, 0, 0, 0, gfc);
            }
        }

        public void UpdateEntity()
        {
            RenderEntity.SetPosition(Position);
            RenderEntity.SetOrientation(Rotation);
        }

        private bool WaitForFileLoad(GameFile file, Action retryAction = null)
        {
            if (file == null) return false;
            if (file.Loaded) return true;

            const int retryIntervalMs = 50;
            var sw = Stopwatch.StartNew();
            long lastRetryTime = 0;

            while (!file.Loaded)
            {
                if (sw.ElapsedMilliseconds > FileLoadTimeoutMs)
                {
                    return false;
                }

                if (retryAction != null && sw.ElapsedMilliseconds - lastRetryTime >= retryIntervalMs)
                {
                    retryAction.Invoke();
                    lastRetryTime = sw.ElapsedMilliseconds;
                }

                Thread.Sleep(1);
            }
            return true;
        }

    }
}
