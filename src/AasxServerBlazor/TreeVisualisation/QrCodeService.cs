using System;
using System.IO;
using AasxServer;
using QRCoder;

namespace AasxServerBlazor.TreeVisualisation;

public class QrCodeService
{
    public string GetQrCodeLink(TreeItem treeItem)
    {
        if (treeItem == null)
        {
            return "";
        }

        Program.hostPort.Split(':');

        var treeItemTag = treeItem.Tag;

        if (treeItemTag is not AssetAdministrationShell aas)
        {
            return "";
        }

        var asset = aas.AssetInformation;
        if (asset == null)
        {
            return "";
        }

        var url = asset.GlobalAssetId;

        return url;
    }

    public string GetQrCodeImage(TreeItem treeItem)
    {
        var o = treeItem?.Tag;

        if (o is not AssetAdministrationShell)
        {
            return "";
        }

        var image = Program.generatedQrCodes[treeItem.Tag];

        return image ?? "";
    }

    public void CreateQrCodeImage(TreeItem treeItem)
    {
        if (treeItem == null)
        {
            return;
        }

        Program.hostPort.Split(':');

        var treeItemTag = treeItem.Tag;

        if (treeItemTag is not AssetAdministrationShell aas)
        {
            return;
        }

        if (Program.generatedQrCodes.ContainsKey(treeItem.Tag))
        {
            Program.generatedQrCodes.Remove(treeItem.Tag);
            return;
        }

        var asset = aas.AssetInformation;
        if (asset == null)
        {
            return;
        }

        var url = asset.GlobalAssetId;

        var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new QRCode(qrCodeData);
        var qrCodeImage = qrCode.GetGraphic(20);
        using var memory = new MemoryStream();
        qrCodeImage.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
        var base64 = Convert.ToBase64String(memory.ToArray());

        Program.generatedQrCodes.Add(treeItem.Tag, base64);
    }
}