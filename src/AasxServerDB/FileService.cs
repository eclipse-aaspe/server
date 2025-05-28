namespace AasxServerDB;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using AasCore.Aas3_0;
using AasxServerStandardBib.Exceptions;
using AasxServerStandardBib.Logging;
using AdminShellNS;
using Contracts.Exceptions;
using Extensions;
using static System.Net.Mime.MediaTypeNames;

public class FileService
{
    internal static void ReadFileInZip(string envFileName, out byte[] content, out long fileSize, IAppLogger<EntityFrameworkPersistenceService> scopedLogger, out string fileName, AasCore.Aas3_0.File file)
    {
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

            using (var fileStream = new FileStream(AasContext.DataPath + "/files/" + Path.GetFileName(envFileName) + ".zip", FileMode.Open))
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
            }
        }
        // incorrect value
        else
        {
            scopedLogger.LogError($"Incorrect value {file.Value} of the Submodel-Element File with IdShort {file.IdShort}");
            throw new UnprocessableEntityException($"Incorrect value {file.Value} of the File with IdShort {file.IdShort}.");
        }
    }

    internal static void ReplaceFileInZip(string envFileName, IAppLogger<EntityFrameworkPersistenceService> scopedLogger, AasCore.Aas3_0.File file, string fileName, string contentType, MemoryStream stream)
    {
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

            var targetFile = Path.Combine(sourcePath, fileName);

            //ToDo: Verify whether we really need a temporary file
            string tempFilePath = Path.Combine(Path.GetTempPath(), fileName);

            using (FileStream fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
            {
                stream.WriteTo(fileStream);
            }

            scopedLogger.LogInformation($"Value of the Submodel-Element File with IdShort {file.IdShort} is a File-Path.");

            using (var fileStream = new FileStream(AasContext.DataPath + "/files/" + Path.GetFileName(envFileName) + ".zip", FileMode.Open))
            {
                using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Update))
                {
                    var entry = archive.GetEntry(file.Value);
                    entry?.Delete();
                    archive.CreateEntryFromFile(tempFilePath, file.Value);
                }
            }

            Directory.Delete(tempFilePath);
        }
        // incorrect value
        else
        {
            scopedLogger.LogError($"Incorrect value {file.Value} of the Submodel-Element File with IdShort {file.IdShort}");
            throw new UnprocessableEntityException($"Incorrect value {file.Value} of the File with IdShort {file.IdShort}.");
        }
    }

    internal static void DeleteFileInZip(string envFileName, IAppLogger<EntityFrameworkPersistenceService> scopedLogger, AasCore.Aas3_0.File file)
    {
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


                using (var fileStream = new FileStream(AasContext.DataPath + "/files/" + Path.GetFileName(envFileName) + ".zip", FileMode.Open))
                {
                    using (var archive = new ZipArchive(fileStream, ZipArchiveMode.Update))
                    {
                        var entry = archive.GetEntry(file.Value);

                        entry?.Delete();
                    }
                }

                file.Value = string.Empty;

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
    }

    internal static bool ReadFromThumbnail(IAssetInformation assetInformation, string envFileName, out byte[] byteArray, out long fileSize)
    {
        bool isFileOperationSuceeded = false;

        byteArray = null;
        fileSize = 0;

        if (assetInformation.DefaultThumbnail != null && !string.IsNullOrEmpty(assetInformation.DefaultThumbnail.Path))
        {
            try
            {
                string fcopy = Path.GetFileName(envFileName) + "__thumbnail";
                fcopy = fcopy.Replace("/", "_");
                fcopy = fcopy.Replace(".", "_");
                var result = System.IO.File.Open(AasContext.DataPath + "/files/" + fcopy + ".dat", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                // Post-condition
                if (!(result == null || result.CanRead))
                {
                    // throw new InvalidOperationException("Unexpected unreadable result stream");
                    return isFileOperationSuceeded;
                }

                byteArray = result.ToByteArray();
                fileSize = byteArray.Length;
                result.Close();
                isFileOperationSuceeded = true;
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

    internal static bool ReplaceThumbnail(ref IAssetInformation assetInformation, string envFileName, string fileName, string contentType, MemoryStream stream)
    {
        bool isFileOperationSuceeded = false;

        //ToDo: Use content type
        //if (string.IsNullOrEmpty(contentType))
        //{
        //    contentType = "application/octet-stream";
        //}

        if (assetInformation.DefaultThumbnail == null)
        {
            assetInformation.DefaultThumbnail = new Resource(fileName);
        }
        else
        {
            assetInformation.DefaultThumbnail.Path = fileName;
        }

        string fcopy = Path.GetFileName(envFileName) + "__thumbnail";
        fcopy = fcopy.Replace("/", "_");
        fcopy = fcopy.Replace(".", "_");

        try
        {
            var result = System.IO.File.Open(AasContext.DataPath + "/files/" + fcopy + ".dat", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);

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

            isFileOperationSuceeded = true;

            return isFileOperationSuceeded;
        }
        catch (Exception)
        {
            return isFileOperationSuceeded;
        }
    }

    internal static bool DeleteThumbnail(ref IAssetInformation assetInformation, string envFileName)
    {
        bool isFileOperationSuceeded = false;

        if (assetInformation.DefaultThumbnail != null && !string.IsNullOrEmpty(assetInformation.DefaultThumbnail.Path))
        {
            string fcopy = Path.GetFileName(envFileName) + "__thumbnail";
            fcopy = fcopy.Replace("/", "_");
            fcopy = fcopy.Replace(".", "_");

            try
            {
                System.IO.File.Delete(AasContext.DataPath + "/files/" + fcopy + ".dat");

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
