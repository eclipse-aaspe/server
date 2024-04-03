using System;
using System.IO;
using System.Linq;
using AasxServer;

namespace AasxServerBlazor.TreeVisualisation.Builders;

public class ImageBuilder
{
    public static string CreateDetailsImage(TreeItem treeItem, out bool url, out bool svg)
    {
        svg = false;
        url = false;

        if (treeItem == null)
        {
            return string.Empty;
        }

        var treeItemTag = treeItem.Tag;

        switch (treeItemTag)
        {
            case AssetAdministrationShell:
            {
                lock (Program.changeAasxFile)
                {
                    try
                    {
                        if (Program.env[treeItem.EnvironmentIndex] == null)
                            return "";

                        using var thumbnailStream = Program.env[treeItem.EnvironmentIndex].GetLocalThumbnailStream();
                        if (thumbnailStream != null)
                        {
                            using var memoryStream = new MemoryStream();
                            thumbnailStream.CopyTo(memoryStream);
                            return Convert.ToBase64String(memoryStream.ToArray());
                        }
                    }
                    catch
                    {
                    }
                }

                break;
            }
            case AasCore.Aas3_0.File file:
            {
                // Test for /aasx/
                if (!string.IsNullOrEmpty(file.Value))
                {
                    var split = file.Value.Split(new[] {'/'});
                    if (split.Length == 2 || split.Length > 1 && split[1].ToLower() == "aasx")
                    {
                        split = file.Value.Split(new[] {'.'});
                        switch (split?.Last().ToLower())
                        {
                            case "jpg":
                            case "bmp":
                            case "png":
                            case "svg":
                                try
                                {
                                    using var localStreamFromPackage = Program.env[treeItem.EnvironmentIndex].GetLocalStreamFromPackage(file.Value);
                                    if (localStreamFromPackage != null)
                                    {
                                        using var memoryStream = new MemoryStream();
                                        if (split?.Last().ToLower() == "svg")
                                        {
                                            svg = true;
                                        }

                                        localStreamFromPackage.CopyTo(memoryStream);
                                        return Convert.ToBase64String(memoryStream.ToArray());
                                    }
                                }
                                catch
                                {
                                }

                                break;
                        }
                    }
                    else
                    {
                        url = true;
                        return file.Value;
                    }
                }

                break;
            }
        }

        return string.Empty;
    }
}