using System.Collections.Generic;
using grzyClothTool.Models.Drawable;
using static grzyClothTool.Enums;

namespace grzyClothTool.Helpers;

public class SimplePathBuilder
{
    public static string BuildPath(GDrawable drawable, string buildPath, BuildResourceType? resourceType = null)
    {
        var pathParts = new List<string> { buildPath, "stream" };

        var genderFolder = drawable.Sex == SexType.male ? "[male]" : "[female]";
        pathParts.Add(genderFolder);

        if (resourceType == BuildResourceType.FiveM && !string.IsNullOrWhiteSpace(drawable.Group))
        {
            var groupPath = drawable.Group.Replace("/", System.IO.Path.DirectorySeparatorChar.ToString())
                                         .Replace("\\", System.IO.Path.DirectorySeparatorChar.ToString());
            pathParts.Add(groupPath);
        }

        pathParts.Add(drawable.TypeName);

        return System.IO.Path.Combine([.. pathParts]);
    }
}
