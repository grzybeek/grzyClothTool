using CodeWalker.GameFiles;
using grzyClothTool.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace grzyClothTool.Helpers;

public class BuildResourceHelper
{
    private readonly AddonManager _addon;
    private readonly string _projectName;
    private readonly string _buildPath;

    public BuildResourceHelper(AddonManager addon, string name, string path)
    {
        _addon = addon;
        _projectName = name;
        _buildPath = path;

        Directory.CreateDirectory(Path.Combine(_buildPath, "stream"));
    }

    public byte[] BuildYMT()
    {
        var mb = new MetaBuilder();
        var mdb = mb.EnsureBlock(MetaName.CPedVariationInfo);
        var CPed = new CPedVariationInfo
        {
            bHasDrawblVariations = 1,
            bHasTexVariations = 1,
            bHasLowLODs = 0,
            bIsSuperLOD = 0
        };

        ArrayOfBytes12 availComp = new();
        var generatedAvailComp = _addon.GenerateAvailComp();
        availComp.SetBytes(generatedAvailComp);
        CPed.availComp = availComp;

        var allCompDrawablesArray = _addon.Drawables.Where(x => x.IsProp == false).ToArray();
        var allPropDrawablesArray = _addon.Drawables.Where(x => x.IsProp == true).ToArray();

        var components = new Dictionary<byte, CPVComponentData>();
        for (byte i = 0; i < generatedAvailComp.Length; i++)
        {
            if (generatedAvailComp[i] == 255) { continue; }

            var drawablesArray = allCompDrawablesArray.Where(x => x.TypeNumeric == i).ToArray();
            var drawables = new CPVDrawblData[drawablesArray.Length];

            for (int d = 0; d < drawables.Length; d++)
            {
                drawables[d].propMask = (byte)(drawablesArray[d].HasSkin ? 17 : 1);
                drawables[d].numAlternatives = 0;
                drawables[d].clothData = new CPVDrawblData__CPVClothComponentData() { ownsCloth = 0 };

                var texturesArray = drawablesArray[d].Textures.ToArray();
                var textures = new CPVTextureData[texturesArray.Length];
                for (int t = 0; t < textures.Length; t++)
                {
                    textures[t].texId = (byte)(drawablesArray[d].HasSkin ? 1 : 0);
                    textures[t].distribution = 255; //seems to be always 255
                }
                drawables[d].aTexData = mb.AddItemArrayPtr(MetaName.CPVTextureData, textures);
            };

            components[i] = new CPVComponentData()
            {
                numAvailTex = (byte)allCompDrawablesArray.Where(x => x.TypeNumeric == i).Sum(y => y.Textures.Count),
                aDrawblData3 = mb.AddItemArrayPtr(MetaName.CPVDrawblData, drawables)
            };
        }

        CPed.aComponentData3 = mb.AddItemArrayPtr(MetaName.CPVComponentData, components.Values.ToArray());

        var compInfos = new CComponentInfo[allCompDrawablesArray.Length];
        for (int i = 0; i < compInfos.Length; i++)
        {
            var drawable = allCompDrawablesArray[i];
            compInfos[i].Unk_802196719 = JenkHash.GenHash(drawable.Audio);
            compInfos[i].Unk_4233133352 = JenkHash.GenHash("none"); //todo
            compInfos[i].Unk_128864925 = new ArrayOfFloats5 { f0 = 0, f1 = 0, f2 = 0, f3 = 0, f4 = drawable.HighHeelsValue }; //expression mods
            compInfos[i].flags = 0;
            compInfos[i].inclusions = 0;
            compInfos[i].exclusions = 0;
            compInfos[i].Unk_1613922652 = ePedVarComp.PV_COMP_HEAD;
            compInfos[i].Unk_2114993291 = 0;
            compInfos[i].Unk_3509540765 = (byte)drawable.TypeNumeric;
            compInfos[i].Unk_4196345791 = (byte)drawable.Number;
        }

        CPed.compInfos = mb.AddItemArrayPtr(MetaName.CComponentInfo, compInfos);

        var propInfo = new CPedPropInfo
        {
            numAvailProps = (byte)allPropDrawablesArray.Length
        };

        var props = new CPedPropMetaData[allPropDrawablesArray.Length];
        for (int i = 0; i < props.Length; i++)
        {
            var prop = allPropDrawablesArray[i];
            props[i].audioId = JenkHash.GenHash(prop.Audio);
            props[i].expressionMods = new ArrayOfFloats5 { f0 = prop.HairScaleValue, f1 = 0, f2 = 0, f3 = 0, f4 = 0 };

            var texturesArray = prop.Textures.ToArray();
            var textures = new CPedPropTexData[texturesArray.Length];
            for (int t = 0; t < textures.Length; t++)
            {
                textures[t].inclusions = 0;
                textures[t].exclusions = 0;
                textures[t].texId = (byte)t;
                textures[t].inclusionId = 0;
                textures[t].exclusionId = 0;
                textures[t].distribution = 255;
            }

            ePropRenderFlags renderFlag = 0;
            if(Enum.TryParse(prop.RenderFlag, out ePropRenderFlags res)){
                renderFlag = res;
            }

            props[i].texData = mb.AddItemArrayPtr(MetaName.CPedPropTexData, textures);
            props[i].renderFlags = renderFlag;
            props[i].propFlags = 0;
            props[i].flags = 0;
            props[i].anchorId = (byte)prop.TypeNumeric;
            props[i].propId = (byte)prop.Number;
            props[i].Unk_2894625425 = 0;
        }
        propInfo.aPropMetaData = mb.AddItemArrayPtr(MetaName.CPedPropMetaData, props);

        var uniqueProps = allPropDrawablesArray.DistinctBy(x => x.TypeNumeric).ToArray();
        var anchors = new CAnchorProps[uniqueProps.Length];
        for (int i = 0; i < anchors.Length; i++)
        {
            var propsOfType = allPropDrawablesArray.Where(x => x.TypeNumeric == uniqueProps[i].TypeNumeric);
            List<byte> items = [];
            foreach (var p in propsOfType)
            {
                items.Add((byte)p.Textures.Count);
            }

            anchors[i].props = mb.AddByteArrayPtr([.. items]);
            anchors[i].anchor = ((eAnchorPoints)propsOfType.First().TypeNumeric);
        }
        propInfo.aAnchors = mb.AddItemArrayPtr(MetaName.CAnchorProps, anchors);

        CPed.propInfo = propInfo;
        CPed.dlcName = JenkHash.GenHash(_projectName);

        mb.AddItem(MetaName.CPedVariationInfo, CPed);


        mb.AddStructureInfo(MetaName.CPedVariationInfo);
        mb.AddStructureInfo(MetaName.CPedPropInfo);
        mb.AddStructureInfo(MetaName.CPedPropTexData);
        mb.AddStructureInfo(MetaName.CAnchorProps);
        mb.AddStructureInfo(MetaName.CComponentInfo);
        mb.AddStructureInfo(MetaName.CPVComponentData);
        mb.AddStructureInfo(MetaName.CPVDrawblData);
        mb.AddStructureInfo(MetaName.CPVDrawblData__CPVClothComponentData);
        mb.AddStructureInfo(MetaName.CPVTextureData);
        mb.AddStructureInfo(MetaName.CPedPropMetaData);
        mb.AddEnumInfo(MetaName.ePedVarComp);
        mb.AddEnumInfo(MetaName.eAnchorPoints);
        mb.AddEnumInfo(MetaName.ePropRenderFlags);

        Meta meta = mb.GetMeta();
        meta.Name = _projectName;

        byte[] data = ResourceBuilder.Build(meta, 2);

        return data;
    }

    public FileInfo BuildMeta(bool isMale)
    {
        var eCharacter = isMale ? "SCR_CHAR_MULTIPLAYER" : "SCR_CHAR_MULTIPLAYER_F";
        var genderLetter = GetGenderLetter(isMale);
        var pedName = GetPedName(isMale);

        StringBuilder sb = new();
        sb.AppendLine(MetaXmlBase.XmlHeader);

        MetaXmlBase.OpenTag(sb, 0, "ShopPedApparel");
        MetaXmlBase.StringTag(sb, 4, "pedName", pedName);
        MetaXmlBase.StringTag(sb, 4, "dlcName", _projectName);
        MetaXmlBase.StringTag(sb, 4, "fullDlcName", pedName + "_" + _projectName);
        MetaXmlBase.StringTag(sb, 4, "eCharacter", eCharacter);
        MetaXmlBase.StringTag(sb, 4, "creatureMetaData", "mp_creaturemetadata_" + genderLetter + "_" + _projectName);

        MetaXmlBase.OpenTag(sb, 4, "pedOutfits");
        MetaXmlBase.CloseTag(sb, 4, "pedOutfits");
        MetaXmlBase.OpenTag(sb, 4, "pedComponents");
        MetaXmlBase.CloseTag(sb, 4, "pedComponents");
        MetaXmlBase.OpenTag(sb, 4, "pedProps");
        MetaXmlBase.CloseTag(sb, 4, "pedProps");

        MetaXmlBase.CloseTag(sb, 0, "ShopPedApparel");

        var xml = sb.ToString();


        var finalPath = Path.Combine(_buildPath, pedName + "_" + _projectName + ".meta");
        File.WriteAllText(finalPath, xml);

        return new FileInfo(finalPath);
    }

    public void BuildFiles(bool isMale, byte[] ymtBytes)
    {
        var pedName = GetPedName(isMale);

        var ymtPath = Path.Combine(_buildPath, "stream", pedName + "_" + _projectName + ".ymt");
        File.WriteAllBytes(ymtPath, ymtBytes);

        var drawables = _addon.Drawables.Where(x => x.Sex == isMale).ToList();
        foreach (var d in drawables)
        {
            var drawablePedName = d.IsProp ? pedName + "_p" : pedName;
            var genderFolderName = isMale ? "[male]" : "[female]";
            var folderPath = Path.Combine(_buildPath, "stream", genderFolderName, d.TypeName);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var prefix = drawablePedName + "_" + _projectName + "^";
            prefix = RemoveInvalidChars(prefix);

            var finalPath = Path.Combine(folderPath, prefix + d.Name + d.File.Extension);
            File.Copy(d.File.FullName, finalPath, true);

            foreach (var t in d.Textures)
            {
                var texFile = t.File;
                var displayName = RemoveInvalidChars(t.DisplayName);
                var finalTexPath = Path.Combine(folderPath, prefix + displayName + texFile.Extension);

                File.Copy(texFile.FullName, finalTexPath, true);
            }
        }

        GenerateCreatureMetadata(drawables, isMale);
    }

    private string RemoveInvalidChars(string input)
    {
        return string.Concat(input.Split(Path.GetInvalidFileNameChars()));
    }

    public void BuildFxManifest(List<string> metaFiles)
    {
        StringBuilder contentBuilder = new();
        contentBuilder.AppendLine("-- This resource was generated by grzyClothTool :)");
        contentBuilder.AppendLine();
        contentBuilder.AppendLine("fx_version 'cerulean'");
        contentBuilder.AppendLine("game { 'gta5' }");
        contentBuilder.AppendLine();
        contentBuilder.AppendLine("files {");

        string filesSection = string.Join(",\n  ", metaFiles.Select(f => $"'{f}'"));
        contentBuilder.AppendLine($"  {filesSection}");

        contentBuilder.AppendLine("}");
        contentBuilder.AppendLine();

        foreach (var file in metaFiles)
        {
            contentBuilder.AppendLine($"data_file 'SHOP_PED_APPAREL_META_FILE' '{file}'");
        }

        var finalPath = Path.Combine(_buildPath, "fxmanifest.lua");
        File.WriteAllText(finalPath, contentBuilder.ToString());
    }

    private void GenerateCreatureMetadata(List<Models.Drawable> drawables, bool isMale)
    {
        //taken from ymteditor because it works fine xd

        var shouldGenCreatureHeels = drawables.Any(x => x.EnableHighHeels);
        var shouldGenCreatureHats = drawables.Any(x => x.EnableHairScale);
        if (!shouldGenCreatureHeels && !shouldGenCreatureHats) return;

        XElement xml = new("CCreatureMetaData");
        XElement pedCompExpressions = new("pedCompExpressions");
        if (shouldGenCreatureHeels)
        {
            var feetDrawables = drawables.Where(x => x.TypeNumeric == 6 && x.IsComponent);
            foreach (var comp in feetDrawables)
            {
                XElement pedCompItem = new("Item");
                pedCompItem.Add(new XElement("pedCompID", new XAttribute("value", string.Format("0x{0:X}", 6))));
                pedCompItem.Add(new XElement("pedCompVarIndex", new XAttribute("value", string.Format("0x{0:X}", comp.Number))));
                pedCompItem.Add(new XElement("pedCompExpressionIndex", new XAttribute("value", string.Format("0x{0:X}", 4))));
                pedCompItem.Add(new XElement("tracks", new XAttribute("content", "char_array"), 33));
                pedCompItem.Add(new XElement("ids", new XAttribute("content", "short_array"), 28462));
                pedCompItem.Add(new XElement("types", new XAttribute("content", "char_array"), 2));
                pedCompItem.Add(new XElement("components", new XAttribute("content", "char_array"), 1));
                pedCompExpressions.Add(pedCompItem);
            }
        }
        xml.Add(pedCompExpressions);

        XElement pedPropExpressions = new("pedPropExpressions");
        if (shouldGenCreatureHats)
        {
            //all original GTA have that one first entry, without it, fivem was sometimes crashing(?)
            XElement FirstpedPropItem = new("Item");
            FirstpedPropItem.Add(new XElement("pedPropID", new XAttribute("value", string.Format("0x{0:X}", 0))));
            FirstpedPropItem.Add(new XElement("pedPropVarIndex", new XAttribute("value", string.Format("0x{0:X}", -1))));
            FirstpedPropItem.Add(new XElement("pedPropExpressionIndex", new XAttribute("value", string.Format("0x{0:X}", -1))));
            FirstpedPropItem.Add(new XElement("tracks", new XAttribute("content", "char_array"), 33));
            FirstpedPropItem.Add(new XElement("ids", new XAttribute("content", "short_array"), 13201));
            FirstpedPropItem.Add(new XElement("types", new XAttribute("content", "char_array"), 2));
            FirstpedPropItem.Add(new XElement("components", new XAttribute("content", "char_array"), 1));
            pedPropExpressions.Add(FirstpedPropItem);

            foreach (var prop in drawables.Where(x => x.TypeNumeric == 0 && x.IsProp))
            {
                XElement pedPropItem = new("Item");
                pedPropItem.Add(new XElement("pedPropID", new XAttribute("value", string.Format("0x{0:X}", 0))));
                pedPropItem.Add(new XElement("pedPropVarIndex", new XAttribute("value", string.Format("0x{0:X}", prop.Number))));
                pedPropItem.Add(new XElement("pedPropExpressionIndex", new XAttribute("value", string.Format("0x{0:X}", 0))));
                pedPropItem.Add(new XElement("tracks", new XAttribute("content", "char_array"), 33));
                pedPropItem.Add(new XElement("ids", new XAttribute("content", "short_array"), 13201));
                pedPropItem.Add(new XElement("types", new XAttribute("content", "char_array"), 2));
                pedPropItem.Add(new XElement("components", new XAttribute("content", "char_array"), 1));
                pedPropExpressions.Add(pedPropItem);
            }
        }
        xml.Add(pedPropExpressions);

        //create XmlDocument from XElement
        var xmldoc = new XmlDocument();
        xmldoc.Load(xml.CreateReader());

        RbfFile rbf = XmlRbf.GetRbf(xmldoc);
        rbf.Save(_buildPath + "/stream/mp_creaturemetadata_" + GetGenderLetter(isMale) + "_" + _projectName + ".ymt");
    }

    private static string GetGenderLetter(bool isMale)
    {
        return isMale ? "m" : "f";
    }

    private static string GetPedName(bool isMale)
    {
        return isMale ? "mp_m_freemode_01" : "mp_f_freemode_01";
    }
}
