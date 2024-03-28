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

        var prefix = "http://";
        var hostPort = Program.hostPort;

        var split = Program.hostPort.Split(':');

        var o = treeItem.Tag;

        if (o is AssetAdministrationShell)
        {
            var aas = o as AssetAdministrationShell;
            var asset = aas.AssetInformation;
            if (asset != null)
            {
                var url = asset.GlobalAssetId;
                var link = url;

                return link;
            }
        }

        return "";
    }

    public string GetQrCodeImage(TreeItem treeItem)
    {
        if (treeItem == null)
        {
            return "";
        }

        var o = treeItem.Tag;

        if (o is AssetAdministrationShell)
        {
            var image = Program.generatedQrCodes[treeItem.Tag];

            if (image != null)
                return image;
        }

        return "";
    }

    public void CreateQrCodeImage(TreeItem treeItem)
    {
        if (treeItem == null)
        {
            return;
        }

        var prefix = "http://";
        var hostPort = Program.hostPort;

        var split = Program.hostPort.Split(':');

        var o = treeItem.Tag;

        if (o is AssetAdministrationShell)
        {
            if (Program.generatedQrCodes.ContainsKey(treeItem.Tag))
            {
                Program.generatedQrCodes.Remove(treeItem.Tag);
                return;
            }

            var aas = o as AssetAdministrationShell;
            var asset = aas.AssetInformation;
            if (asset != null)
            {
                var url = asset.GlobalAssetId;
                var link = url;

                var qrGenerator = new QRCodeGenerator();
                var qrCodeData = qrGenerator.CreateQrCode(link, QRCodeGenerator.ECCLevel.Q);
                var qrCode = new QRCode(qrCodeData);
                var qrCodeImage = qrCode.GetGraphic(20);
                using (MemoryStream memory = new MemoryStream())
                {
                    qrCodeImage.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                    var base64 = Convert.ToBase64String(memory.ToArray());

                    Program.generatedQrCodes.Add(treeItem.Tag, base64);
                }
            }
        }
    }
}