namespace AasxServerDB;
using System;
using System.IO;
using System.IO.Compression;
using AasCore.Aas3_0;
using AasxServerStandardBib.Exceptions;
using AasxServerStandardBib.Logging;
using AdminShellNS;
using Contracts.Exceptions;
using Extensions;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.Expressions.Internal;

public class FileService
{
    public const string ThumnbnailsFolderName = "thumbnails";
    public const string FilesFolderName = "files";
    public const string XmlFolderName = "xml";

    internal static string FormatToFileName(string name)
    {
        name = name.Replace("/", "_");
        name = name.Replace(".", "_");
        name = name.Replace(":", "_");
        return name;
    }

    internal static string GetThumbnailZipPath(string aasId)
    {
        return Path.Combine(AasContext.DataPath, FilesFolderName, ThumnbnailsFolderName, FormatToFileName(aasId) + ".zip");
    }

    internal static void CreateThumbnailZipFile(IAssetAdministrationShell aas, Stream thumbnailStreamFromPackage = null)
    {
        using (var fileStream = new FileStream(GetThumbnailZipPath(aas.Id), FileMode.Create))
        {
            using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Create))
            {
                if (thumbnailStreamFromPackage != null)
                {
                    try
                    {
                        var onlyFileName = Path.GetFileName(aas.AssetInformation.DefaultThumbnail?.Path);
                        var tempFilePath = Path.Combine(Path.GetTempPath(), onlyFileName);

                        using (var fst = System.IO.File.Create(tempFilePath))
                        {
                            thumbnailStreamFromPackage.CopyTo(fst);
                        }

                        archive.CreateEntryFromFile(tempFilePath, aas.AssetInformation.DefaultThumbnail?.Path);

                    }
                    catch (Exception e)
                    {
                    }
                }
            }
        }
    }

    internal static void InitFileSystem(bool reloadDBFiles)
    {
        var filesPath = Path.Combine(AasContext.DataPath, FilesFolderName);

        if (reloadDBFiles && Directory.Exists(filesPath))
        {
            Directory.Delete(filesPath, true);
        }

        if (!Directory.Exists(filesPath))
            Directory.CreateDirectory(filesPath);

        var xmlPath = Path.Combine(AasContext.DataPath, XmlFolderName);

        if (reloadDBFiles && Directory.Exists(xmlPath))
        {
            Directory.Delete(xmlPath, true);
        }

        if (!Directory.Exists(xmlPath))
            Directory.CreateDirectory(xmlPath);

        var thumbnailFolderPath = Path.Combine(filesPath, ThumnbnailsFolderName);
        if (!Directory.Exists(thumbnailFolderPath))
            Directory.CreateDirectory(thumbnailFolderPath);

        var path = Path.Combine(filesPath, "_unpacked" + ".zip");

        if (reloadDBFiles || !System.IO.File.Exists(path))
        {
            using (var fileStream = new FileStream(path, FileMode.OpenOrCreate))
            {
                using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Create))
                {
                }
            }
        }

    }

    internal static string GetFilesZipPath(string envFileName)
    {
        if (envFileName == null)
        {
            return Path.Combine(AasContext.DataPath, FilesFolderName, "_unpacked.zip");
        }

        var zipFileName = Path.GetFileName(envFileName) + ".zip";
        return Path.Combine(AasContext.DataPath, FilesFolderName, zipFileName);
    }

    internal static bool ReadFileInZip(IAppLogger<EntityFrameworkPersistenceService> scopedLogger, string envFileName, AasCore.Aas3_0.File file, out byte[] content,
        out long fileSize, out string fileName)
    {
        bool isFileOperationSuceeded = false;
        content = null;
        fileSize = 0;

        fileName = file.Value;

        if (string.IsNullOrEmpty(fileName))
        {
            scopedLogger.LogError($"File name is empty. Cannot fetch the file.");
            throw new UnprocessableEntityException($"File value Null!!");
        }

        //check if it is external location
        if (file.Value.StartsWith("http") || file.Value.StartsWith("https"))
        {
            scopedLogger.LogWarning($"Value of the Submodel-Element File with IdShort {file.IdShort} is an external link.");
            throw new NotImplementedException($"File location for {file.IdShort} is external {file.Value}. Currently this fuctionality is not supported.");
        }
        //Check if a directory
        else if (file.Value.StartsWith('/') || file.Value.StartsWith('\\'))
        {
            scopedLogger.LogInformation($"Value of the Submodel-Element File with IdShort {file.IdShort} is a File-Path.");

            try
            {
                using (var fileStream = new FileStream(GetFilesZipPath(envFileName), FileMode.Open))
                {
                    using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Read))
                    {
                        var archiveFile = archive.GetEntry(file.Value);
                        var tempStream = archiveFile.Open();
                        var ms = new MemoryStream();
                        tempStream.CopyTo(ms);
                        ms.Position = 0;
                        content = ms.ToByteArray();

                        fileSize = content.Length;
                    }
                    isFileOperationSuceeded = true;
                    return isFileOperationSuceeded;
                }
            }
            catch (Exception)
            {
            }

        }
        // incorrect value
        else
        {
            scopedLogger.LogError($"Incorrect value {file.Value} of the Submodel-Element File with IdShort {file.IdShort}");
            throw new UnprocessableEntityException($"Incorrect value {file.Value} of the File with IdShort {file.IdShort}.");
        }

        return isFileOperationSuceeded;
    }

    internal static bool ReplaceFileInZip(IAppLogger<EntityFrameworkPersistenceService> scopedLogger, string envFileName, ref AasCore.Aas3_0.File file, string fileName, string contentType, MemoryStream stream)
    {
        bool isFileOperationSuceeded = false;

        if (string.IsNullOrEmpty(file.Value))
        {
            scopedLogger.LogError($"File name is empty. Cannot fetch the file.");
            throw new UnprocessableEntityException($"File value Null!!");
        }

        //check if it is external location
        if (file.Value.StartsWith("http") || file.Value.StartsWith("https"))
        {
            scopedLogger.LogWarning($"Value of the Submodel-Element File with IdShort {file.IdShort} is an external link.");
            throw new NotImplementedException($"File location for {file.IdShort} is external {file.Value}. Currently this fuctionality is not supported.");
        }
        //Check if a directory
        else if (file.Value.StartsWith('/') || file.Value.StartsWith('\\'))
        {
            //check if the value consists file extension
            string sourcePath;
            if (Path.HasExtension(file.Value))
            {
                sourcePath = Path.GetDirectoryName(file.Value); //This should get platform specific path, without file name
            }
            else
            {
                sourcePath = Path.Combine(file.Value);
            }

            //ToDo: Verify whether we really need a temporary file
            string tempFilePath = Path.Combine(Path.GetTempPath(), fileName);

            using (FileStream fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
            {
                stream.WriteTo(fileStream);
            }

            scopedLogger.LogInformation($"Value of the Submodel-Element File with IdShort {file.IdShort} is a File-Path.");

            try
            {
                using (var fileStream = new FileStream(AasContext.DataPath + "/files/" + Path.GetFileName(envFileName) + ".zip", FileMode.Open))
                {
                    using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Update))
                    {
                        var entry = archive.GetEntry(file.Value);
                        entry?.Delete();
                        archive.CreateEntryFromFile(tempFilePath, file.Value);
                    }
                }

                file.Value = sourcePath;
                isFileOperationSuceeded = true;

                if (System.IO.File.Exists(tempFilePath))
                {
                    System.IO.File.Delete(tempFilePath);
                }
            }
            catch (Exception)
            {
            }

        }
        // incorrect value
        else
        {
            scopedLogger.LogError($"Incorrect value {file.Value} of the Submodel-Element File with IdShort {file.IdShort}");
            throw new UnprocessableEntityException($"Incorrect value {file.Value} of the File with IdShort {file.IdShort}.");
        }

        return isFileOperationSuceeded;
    }

    internal static bool DeleteFileInZip(IAppLogger<EntityFrameworkPersistenceService> scopedLogger, string envFileName, AasCore.Aas3_0.File file)
    {
        bool isFileOperationSuceeded = false;

        if (!string.IsNullOrEmpty(file.Value))
        {
            //check if it is external location
            if (file.Value.StartsWith("http") || file.Value.StartsWith("https"))
            {
                scopedLogger.LogWarning($"Value of the Submodel-Element File with IdShort {file.IdShort} is an external link.");
                throw new NotImplementedException($"File location for {file.IdShort} is external {file.Value}. Currently this fuctionality is not supported.");
            }
            //Check if a directory
            else if (file.Value.StartsWith('/') || file.Value.StartsWith('\\'))
            {
                scopedLogger.LogInformation($"Value of the Submodel-Element File with IdShort {file.IdShort} is a File-Path.");

                try
                {
                    using (var fileStream = new FileStream(GetFilesZipPath(envFileName), FileMode.Open))
                    {
                        using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Update))
                        {
                            var entry = archive.GetEntry(file.Value);

                            entry?.Delete();
                        }
                    }

                    file.Value = string.Empty;
                    isFileOperationSuceeded = true;

                    return isFileOperationSuceeded;
                }
                catch (Exception)
                {
                    return isFileOperationSuceeded;
                }

                //ToDo: Do notification
                //Program.signalNewData(1);
                //scopedLogger.LogDebug($"Deleted the file at {idShortPath} from submodel with Id {submodelIdentifier}");
            }
            // incorrect value
            else
            {
                scopedLogger.LogError($"Incorrect value {file.Value} of the Submodel-Element File with IdShort {file.IdShort}");
                throw new OperationNotSupported($"Incorrect value {file.Value} of the File with IdShort {file.IdShort}.");
            }
        }

        return isFileOperationSuceeded;
    }

    internal static bool ReadFromThumbnail(IAssetInformation assetInformation, string aasIdentifier, out byte[] content, out long fileSize)
    {
        bool isFileOperationSuceeded = false;

        content = null;
        fileSize = 0;

        if (assetInformation.DefaultThumbnail != null && !string.IsNullOrEmpty(assetInformation.DefaultThumbnail.Path))
        {
            try
            {
                using (var fileStream = new FileStream(GetThumbnailZipPath(aasIdentifier), FileMode.Open))
                {
                    using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Read))
                    {
                        var archiveFile = archive.GetEntry(assetInformation.DefaultThumbnail.Path);
                        using var tempStream = archiveFile.Open();
                        var ms = new MemoryStream();
                        tempStream.CopyTo(ms);
                        ms.Position = 0;
                        content = ms.ToByteArray();

                        fileSize = content.Length;

                        isFileOperationSuceeded = true;
                    }
                }
            }
            catch (Exception)
            {
                return isFileOperationSuceeded;
            }
        }
        else
        {
            throw new NotFoundException($"No default thumbnail embedded in the AssetInformation of the requested AAS.");
        }

        return isFileOperationSuceeded;
    }

    internal static bool ReplaceThumbnail(ref IAssetInformation assetInformation, string aasIdentifier, string fileName, string contentType, MemoryStream stream)
    {
        bool isFileOperationSuceeded = false;

        //ToDo: Use content type
        //if (string.IsNullOrEmpty(contentType))
        //{
        //    contentType = "application/octet-stream";
        //}

        var onlyFileName = Path.GetFileName(fileName);

        try
        {
            var tempFilePath = Path.Combine(Path.GetTempPath(), onlyFileName);
            var path = Path.Combine("aasx", "images", onlyFileName);

            using var result = System.IO.File.Open(tempFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);

            // Post-condition
            if (!(result == null || result.CanRead))
            {
                // throw new InvalidOperationException("Unexpected unreadable result stream");
                return isFileOperationSuceeded;
            }

            //Write to the part
            stream.Position = 0;
            using (Stream dest = result)
            {
                stream.CopyTo(dest);
            }

            result.Close();

            using (var zipFileStream = new FileStream(GetThumbnailZipPath(aasIdentifier), FileMode.Open))
            {
                using (var archive = new ZipArchive(zipFileStream, ZipArchiveMode.Update))
                {
                    if (assetInformation.DefaultThumbnail != null)
                    {
                        var entry = archive.GetEntry(assetInformation.DefaultThumbnail.Path);
                        entry?.Delete();
                    }

                    archive.CreateEntryFromFile(tempFilePath, path);
                }
            }

            if (assetInformation.DefaultThumbnail == null)
            {
                assetInformation.DefaultThumbnail = new Resource(path);
            }
            else
            {
                assetInformation.DefaultThumbnail.Path = path;
            }

            if (System.IO.File.Exists(tempFilePath))
            {
                System.IO.File.Delete(tempFilePath);
            }

            isFileOperationSuceeded = true;

            return isFileOperationSuceeded;
        }
        catch (Exception)
        {
            return isFileOperationSuceeded;
        }
    }

    internal static bool DeleteThumbnail(ref IAssetInformation assetInformation, string aasIdentifier)
    {
        bool isFileOperationSuceeded = false;

        if (assetInformation.DefaultThumbnail != null && !string.IsNullOrEmpty(assetInformation.DefaultThumbnail.Path))
        {
            try
            {
                using (var zipFileStream = new FileStream(GetThumbnailZipPath(aasIdentifier), FileMode.OpenOrCreate))
                {
                    using (var archive = new ZipArchive(zipFileStream, ZipArchiveMode.Update))
                    {
                        if (assetInformation.DefaultThumbnail != null)
                        {
                            var entry = archive.GetEntry(assetInformation.DefaultThumbnail.Path);
                            entry?.Delete();
                        }
                    }
                }

                assetInformation.DefaultThumbnail = null;

                isFileOperationSuceeded = true;
            }
            catch (Exception)
            {
                isFileOperationSuceeded = false;
            }
        }
        else
        {
            throw new NotFoundException($"No default thumbnail embedded in the AssetInformation of the requested AAS.");
        }

        return isFileOperationSuceeded;
    }
}
