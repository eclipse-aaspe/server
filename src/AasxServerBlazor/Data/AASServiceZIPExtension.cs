using AasxRestServerLibrary;
using AasxServer;
using AasxServerBlazor.Pages;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using static AasxServerBlazor.Pages.TreePage;
using static AdminShellNS.AdminShellV20;

namespace AasxServerBlazor.Data
{
    public class AASServiceZIPExtension : AASServiceExtensionBase, IAASServiceExtension
    {
        public bool IsSuitableFor(File file)
        {
            var filePath = file.value;

            // currently, we only check the file extension; maybe, other checks (e.g. for certain semantic ids)
            // should be implemented in the future
            return filePath.EndsWith("zip");
        }

        public void CreateItems(Item fileItem, System.IO.Stream zipFileStream, string fileRestURL)
        {
            var zipArchive = AasxHttpContextHelperZipExtensions.LoadZipArchive(zipFileStream);

            var zip = CreateArchiveItem(fileItem, zipArchive, fileRestURL);

            fileItem.Childs = new List<Item>() { zip };
        }

        protected override List<Item> CreateChildren(Item item, object tag)
        {
            var children = new List<Item>();

            var jObject = tag as JObject;
            var subentries = jObject?.GetValue("subEntries") as JArray;

            if (subentries == null)
            {
                return children;
            }

            var dirs = subentries.ToList().FindAll(e => e is JObject && (e as JObject).GetValue("type")?.ToString() == "directory").Select(e => e as JObject);
            var files = subentries.ToList().FindAll(e => e is JObject && (e as JObject).GetValue("type")?.ToString() == "file").Select(e => e as JObject);

            foreach (var entry in dirs)
            {
                children.Add(CreateItem(item, entry.GetValue("name")?.ToString(), entry, "Dir"));
            }
            foreach (var entry in files)
            {
                children.Add(CreateItem(item, entry.GetValue("name")?.ToString(), entry, "File"));
            }

            return children;
        }

        private ExtensionItem CreateArchiveItem(Item parentItem, ZipArchive archive, string fileRestURL)
        {
            ZipJsonConverter converter = new ZipJsonConverter(archive);
            JObject json = converter.BuildJsonRecursively(archive, "", true);

            return CreateItem(parentItem, parentItem.Text, json, "ZIP", fileRestURL);
        }

        public string ViewNodeDetails(TreePage.Item item, int line, int col)
        {
            if (item is null || (item.Tag as JObject) is null)
            {
                return null;
            }

            var jObject = item.Tag as JObject;

            JToken type;
            if (!jObject.TryGetValue("type", out type))
            {
                return null;
            }

            switch (line)
            {
                case 0:
                    if (col == 0)
                        return "Full Name";
                    if (col == 1)
                        return jObject.GetValue("fullName")?.ToString();
                    break;
            }

            if (type.ToString() == "file")
            {
                switch (line)
                {
                    case 1:
                        if (col == 0)
                            return "Length";
                        if (col == 1)
                            return jObject.GetValue("length")?.ToString();
                        break;
                    case 2:
                        if (col == 0)
                            return "Compressed Length";
                        if (col == 1)
                            return jObject.GetValue("compressedLength")?.ToString();
                        break;
                }
            }

            return null;
        }

        public string ViewNodeID(TreePage.Item item)
        {
            return item?.Text;
        }

        public string ViewNodeType(TreePage.Item item)
        {
            return item?.Type;
        }

        public string ViewNodeInfo(TreePage.Item item)
        {
            return null;
        }

        public string GetFragmentType(Item item)
        {
            return "zip";
        }

        public string GetFragment(Item item)
        {
            JObject jObject = item.Tag as JObject;

            if (jObject is null)
            {
                return null;
            }

            return jObject.GetValue("fullName").ToString();
        }

        public override string GetDownloadLink(Item item)
        {
            var restURL = this.GetRestURL(item);

            if (restURL == null)
            {
                return null;
            }

            JObject jObject = item.Tag as JObject;
            if (jObject.GetValue("type").ToString() == "file")
            {
                return restURL + "?content=raw";
            }
            else
            {
                return null;
            }

        }

        public override bool RepresentsFileToBeBrowsed(Item item)
        {
            JObject jObject = item.Tag as JObject;
            return (jObject?.GetValue("type").ToString() == "file");
        }
    }
}
