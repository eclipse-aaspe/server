using System.Collections.Generic;
using static AasxServerBlazor.Pages.TreePage;
using static AdminShellNS.AdminShellV20;
using AasxServer;
using AasxRestServerLibrary;
using System.Xml;

namespace AasxServerBlazor.Data
{
    public class AASServiceXMLExtension : IAASServiceExtension
    {
        public bool IsSuitableFor(File file)
        {
            var filePath = file.value;

            // currently, we only check the file extension; maybe, other checks (e.g. for certain semantic ids)
            // should be implemented in the future
            return filePath.EndsWith("xml");
        }

        public void CreateItems(Item xmlFileItem, File xmlFile, string fileRestURL)
        {
            try
            {
                var filePath = xmlFile.value;
                var fileStream = Program.env[xmlFileItem.envIndex].GetLocalStreamFromPackage(filePath);
                var xmlDocument = AasxHttpContextHelperXmlExtensions.LoadXmlDocument(fileStream);

                var xml = CreateXMLDocumentItem(xmlFileItem, xmlDocument);

                xmlFileItem.Childs = new List<Item>() { xml };

            }
            catch
            { }
        }

        private ExtensionItem CreateItem(Item parent, string text, object tag, string type = null)
        {
            var item = new ExtensionItem();
            item.envIndex = parent.envIndex;
            item.parent = parent;
            item.Text = text;
            item.Tag = tag;
            item.Type = type;
            item.Childs = CreateChildren(item, tag);
            item.extension = this;

            return item;
        }

        private List<Item> CreateChildren(Item item, object xmlObject)
        {
            var children = new List<Item>();

            if (xmlObject is XmlDocument)
            {
                foreach (var child in (xmlObject as XmlDocument).ChildNodes)
                {
                    if (child is XmlElement)
                    {
                        children.Add(CreateXMLElementItem(item, child as XmlElement));
                    }
                }
            } else if (xmlObject is XmlElement)
            {
                foreach (var child in (xmlObject as XmlElement).ChildNodes)
                {
                    if (child is XmlElement)
                    {
                        children.Add(CreateXMLElementItem(item, child as XmlElement));
                    }
                }
                foreach (var child in (xmlObject as XmlElement).Attributes)
                {
                    if (child is XmlAttribute)
                    {
                        children.Add(CreateXMLAttributeItem(item, child as XmlAttribute));
                    }
                }
            }

            return children;
        }


        private Item CreateXMLDocumentItem(Item parentItem, XmlDocument doc)
        {
            return CreateItem(parentItem, parentItem.Text, doc, "XML");
        }

        private Item CreateXMLElementItem(Item parentItem, XmlElement node)
        {
            return CreateItem(parentItem, node.Name, node, "Ele");
        }

        private Item CreateXMLAttributeItem(Item parentItem, XmlAttribute node)
        {
            return CreateItem(parentItem, node.Name, node, "Att");
        }

        public string ViewNodeID(Item item)
        {
            return item?.Text;
        }

        public string ViewNodeType(Item item)
        {
            return item?.Type;
        }

        public string ViewNodeDetails(Item item, int line, int col)
        {
            if (item is null || item.Tag is null)
            {
                return null;
            }

            var tag = item.Tag;

            if (tag is XmlElement)
            {
                var xmlElement = tag as XmlElement;

                switch (line)
                {
                    case 0:
                        if (col == 0)
                            return "Value";
                        if (col == 1)
                            return getTextValue(xmlElement);
                        break;
                    case 1:
                        if (col == 0)
                            return "Comment";
                        if (col == 1)
                            return getComment(xmlElement);
                        break;
                    case 2:
                        if (col == 0)
                            return "Namespace URI";
                        if (col == 1)
                            return xmlElement.NamespaceURI;
                        break;
                    default:
                        return null;
                }
            } else if (tag is XmlAttribute)
            {
                var xmlAttribute = tag as XmlAttribute;

                switch (line)
                {
                    case 0:
                        if (col == 0)
                            return "Value";
                        if (col == 1)
                            return xmlAttribute.Value;
                        break;
                    case 1:
                        if (col == 0)
                            return "Namespace URI";
                        if (col == 1)
                            return xmlAttribute.NamespaceURI;
                        break;
                    default:
                        return null;
                }
            }

            return null;
        }

        public string ViewNodeInfo(Item item)
        {
            if (item is null || item.Tag is null)
            {
                return null;
            }

            var tag = item.Tag;

            string value = null;
            if (tag is XmlElement)
            {
                value = getTextValue(tag as XmlElement);

            } else if (tag is XmlAttribute)
            {
                value = (tag as XmlAttribute).Value;
            }
            return value?.Length > 0 ? " = " + value : null;
        }

        private string getTextValue(XmlElement xmlElement)
        {
            foreach (XmlNode child in xmlElement.ChildNodes)
            {
                if (child.NodeType == XmlNodeType.Text ||
                    child.NodeType == XmlNodeType.CDATA)
                {
                    return child.Value;
                }
            }

            return null;
        }
        private string getComment(XmlElement xmlElement)
        {
            foreach (XmlNode child in xmlElement.ChildNodes)
            {
                if (child.NodeType == XmlNodeType.Comment)
                {
                    return child.Value;
                }
            }

            return null;
        }

        public string GetFragmentType(Item item)
        {
            return "xml";
        }

        public string GetFragment(Item item)
        {
            return null;
        }
    }
}
