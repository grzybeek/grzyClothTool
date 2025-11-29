using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace grzyClothTool.Models;

/// <summary>
/// Represents the pedalternativevariations.meta file structure for hiding hair with certain components
/// </summary>
public class PedAlternativeVariations
{
    public List<PedVariation> Peds { get; set; } = new();

    public XDocument ToXml()
    {
        var root = new XElement("CAlternateVariations");
        var pedsElement = new XElement("peds");

        foreach (var ped in Peds)
        {
            pedsElement.Add(ped.ToXml());
        }

        root.Add(pedsElement);

        return new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            root
        );
    }

    public static PedAlternativeVariations FromXml(XDocument doc)
    {
        var pedAlternativeVariations = new PedAlternativeVariations();
        
        var root = doc.Element("CAlternateVariations");
        if (root == null) return pedAlternativeVariations;

        var pedsElement = root.Element("peds");
        if (pedsElement == null) return pedAlternativeVariations;

        foreach (var pedItem in pedsElement.Elements("Item"))
        {
            var pedVariation = new PedVariation();
            
            var nameElement = pedItem.Element("name");
            if (nameElement != null)
            {
                pedVariation.Name = nameElement.Value;
            }

            var switchesElement = pedItem.Element("switches");
            if (switchesElement != null)
            {
                foreach (var switchItem in switchesElement.Elements("Item"))
                {
                    var alternateSwitch = new AlternateSwitch();
                    
                    var dlcNameHashElement = switchItem.Element("dlcNameHash");
                    if (dlcNameHashElement != null)
                    {
                        alternateSwitch.DlcNameHash = dlcNameHashElement.Value;
                    }

                    var componentElement = switchItem.Element("component");
                    if (componentElement != null)
                    {
                        alternateSwitch.Component = int.Parse(componentElement.Attribute("value")?.Value ?? "0");
                    }

                    var indexElement = switchItem.Element("index");
                    if (indexElement != null)
                    {
                        alternateSwitch.Index = int.Parse(indexElement.Attribute("value")?.Value ?? "0");
                    }

                    var altElement = switchItem.Element("alt");
                    if (altElement != null)
                    {
                        alternateSwitch.Alt = int.Parse(altElement.Attribute("value")?.Value ?? "0");
                    }

                    var sourceAssetsElement = switchItem.Element("sourceAssets");
                    if (sourceAssetsElement != null)
                    {
                        foreach (var assetItem in sourceAssetsElement.Elements("Item"))
                        {
                            var sourceAsset = new SourceAsset();
                            
                            var assetDlcElement = assetItem.Element("dlcNameHash");
                            if (assetDlcElement != null)
                            {
                                sourceAsset.DlcNameHash = assetDlcElement.Value;
                            }

                            var assetComponentElement = assetItem.Element("component");
                            if (assetComponentElement != null)
                            {
                                sourceAsset.Component = int.Parse(assetComponentElement.Attribute("value")?.Value ?? "0");
                            }

                            var assetIndexElement = assetItem.Element("index");
                            if (assetIndexElement != null)
                            {
                                sourceAsset.Index = int.Parse(assetIndexElement.Attribute("value")?.Value ?? "0");
                            }

                            alternateSwitch.SourceAssets.Add(sourceAsset);
                        }
                    }

                    pedVariation.Switches.Add(alternateSwitch);
                }
            }

            pedAlternativeVariations.Peds.Add(pedVariation);
        }

        return pedAlternativeVariations;
    }
}

public class PedVariation
{
    public string Name { get; set; } = string.Empty; // e.g., "mp_m_freemode_01" or "mp_f_freemode_01"
    public List<AlternateSwitch> Switches { get; set; } = new();

    public XElement ToXml()
    {
        var item = new XElement("Item");
        item.Add(new XElement("name", Name));

        var switchesElement = new XElement("switches");
        foreach (var sw in Switches)
        {
            switchesElement.Add(sw.ToXml());
        }
        item.Add(switchesElement);

        return item;
    }
}

public class AlternateSwitch
{
    public string? DlcNameHash { get; set; } // Optional DLC name for the hair (null for base game)
    public int Component { get; set; } // Target component to switch (e.g., 2 for hair)
    public int Index { get; set; } // Hair drawable index
    public int Alt { get; set; } // Alternative hair drawable (usually 1 for bald)
    public List<SourceAsset> SourceAssets { get; set; } = new();

    public XElement ToXml()
    {
        var item = new XElement("Item");
        
        // Add dlcNameHash if present (for DLC hairs)
        if (!string.IsNullOrEmpty(DlcNameHash))
        {
            item.Add(new XElement("dlcNameHash", DlcNameHash));
        }
        
        item.Add(new XElement("component", new XAttribute("value", Component)));
        item.Add(new XElement("index", new XAttribute("value", Index)));
        item.Add(new XElement("alt", new XAttribute("value", Alt)));

        var sourceAssetsElement = new XElement("sourceAssets");
        foreach (var asset in SourceAssets)
        {
            sourceAssetsElement.Add(asset.ToXml());
        }
        item.Add(sourceAssetsElement);

        return item;
    }
}

public class SourceAsset
{
    public string DlcNameHash { get; set; } = string.Empty; // e.g., "Female_heist"
    public int Component { get; set; } // Source component (e.g., 1 for berd/masks)
    public int Index { get; set; } // Drawable index

    public XElement ToXml()
    {
        var item = new XElement("Item");
        item.Add(new XElement("dlcNameHash", DlcNameHash));
        item.Add(new XElement("component", new XAttribute("value", Component)));
        item.Add(new XElement("index", new XAttribute("value", Index)));

        return item;
    }
}
