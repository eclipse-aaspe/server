using System;
using System.IO;
using AasxServer;

namespace AasxServerBlazor.TreeVisualisation.Builders;

internal static class ImageBuilder
{
    public static string CreateDetailsImage(TreeItem treeItem, out bool isUrl, out bool isSvg)
    {
        isSvg = false;
        isUrl = false;

        if (treeItem == null || treeItem.Tag == null)
        {
            return string.Empty;
        }

        return treeItem.Tag switch
        {
            AssetAdministrationShell _ => GetThumbnailFromAssetAdministrationShell(treeItem, out isUrl),
            AasCore.Aas3_0.File file => GetImageFromFile(file, treeItem, out isUrl, out isSvg),
            _ => string.Empty
        };
    }

    private static string GetThumbnailFromAssetAdministrationShell(TreeItem treeItem, out bool isUrl)
    {
        isUrl = false;
        lock (Program.changeAasxFile)
        {
            try
            {
                if (Program.env[treeItem.EnvironmentIndex] == null)
                {
                    return string.Empty;
                }

                using var thumbnailStream = Program.env[treeItem.EnvironmentIndex].GetLocalThumbnailStream();
                if (thumbnailStream != null)
                {
                    using var memoryStream = new MemoryStream();
                    thumbnailStream.CopyTo(memoryStream);
                    return Convert.ToBase64String(memoryStream.ToArray());
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine($"{nameof(GetThumbnailFromAssetAdministrationShell)} threw exception: {exception.Message}");
            }
        }

        return string.Empty;
    }

    private static string GetImageFromFile(IFile file, TreeItem treeItem, out bool isUrl, out bool isSvg)
    {
        isUrl = false;
        isSvg = false;

        if (string.IsNullOrEmpty(file.Value))
        {
            return string.Empty;
        }

        var split = file.Value.Split(new[] {'/'});
        if (split.Length == 2 || split.Length > 1 && split[1].ToLower() == "aasx")
        {
            split = file.Value.Split(new[] {'.'});
            if (split.Length <= 0)
            {
                return string.Empty;
            }

            var extension = split[^1].ToLower();
            switch (extension)
            {
                case "jpg":
                case "bmp":
                case "png":
                case "svg":
                    return GetImageFromPackage(file.Value, treeItem, extension, out isSvg);
            }
        }
        else
        {
            isUrl = true;
            return file.Value;
        }

        return string.Empty;
    }

    private static string GetImageFromPackage(string filePath, TreeItem treeItem, string extension, out bool isSvg)
    {
        isSvg = extension == "svg";

        try
        {
            using var localStreamFromPackage = Program.env[treeItem.EnvironmentIndex].GetLocalStreamFromPackage(filePath);
            if (localStreamFromPackage != null)
            {
                using var memoryStream = new MemoryStream();
                localStreamFromPackage.CopyTo(memoryStream);
                return Convert.ToBase64String(memoryStream.ToArray());
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine($"{nameof(GetImageFromPackage)} threw exception: {exception.Message}");
        }

        return string.Empty;
    }
}