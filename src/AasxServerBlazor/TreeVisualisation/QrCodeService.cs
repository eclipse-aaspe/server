using System;
using System.Drawing.Imaging;
using System.IO;
using AasxServer;
using QRCoder;

namespace AasxServerBlazor.TreeVisualisation;

/// <inheritdoc cref="IQrCodeService"/>
public class QrCodeService: IQrCodeService
{
    private readonly QRCodeGenerator _qrCodeGenerator;

    public QrCodeService()
    {
        _qrCodeGenerator = new QRCodeGenerator();
    }

    public string GetQrCodeLink(TreeItem treeItem)
    {
        if (treeItem is not {Tag: AssetAdministrationShell aas})
        {
            return string.Empty;
        }

        var asset = aas.AssetInformation;
        return asset?.GlobalAssetId ?? string.Empty;
    }

    public string GetQrCodeImage(TreeItem treeItem)
    {
        if (treeItem is not {Tag: AssetAdministrationShell})
        {
            return string.Empty;
        }

        return Program.generatedQrCodes.TryGetValue(treeItem.Tag, out var image) ? image : "";
    }

    public void CreateQrCodeImage(TreeItem treeItem)
    {
        if (treeItem is not {Tag: AssetAdministrationShell aas})
        {
            return;
        }

        if (Program.generatedQrCodes.ContainsKey(treeItem.Tag))
        {
            Program.generatedQrCodes.Remove(treeItem.Tag);
            return;
        }

        var asset = aas.AssetInformation;

        var url = asset.GlobalAssetId;
        var qrCodeData = _qrCodeGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new QRCode(qrCodeData);
        var qrCodeImage = qrCode.GetGraphic(20);

        try
        {
            using var memory = new MemoryStream();
            qrCodeImage.Save(memory, ImageFormat.Bmp);
            var base64 = Convert.ToBase64String(memory.ToArray());

            Program.generatedQrCodes.Add(treeItem.Tag, base64);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}