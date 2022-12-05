using System.Collections.Generic;
using static AasxServerBlazor.Pages.TreePage;
using static AdminShellNS.AdminShellV20;
using AasxServer;
using AasxRestServerLibrary;
using System.Xml;

namespace AasxServerBlazor.Data
{
    public class AASServiceXMLExtension : AASServiceExtensionBase, IAASServiceExtension
    {
        public bool IsSuitableFor(File file)
        {
            var filePath = file.value;

            // currently, we only check the file extension; maybe, other checks (e.g. for certain semantic ids)
            // should be implemented in the future
            return filePath.EndsWith("xml");
        }

        public void CreateItems(Item xmlFileItem, System.IO.Stream xmlFileStream, string fileRestURL)
        {
            try
            {
                var xmlDocument = AasxHttpContextHelperXmlExtensions.LoadXmlDocument(xmlFileStream);

                var xml = CreateXMLDocumentItem(xmlFileItem, xmlDocument, fileRestURL);

                xmlFileItem.Childs = new List<Item>() { xml };

            }
            catch
            { }
        }

        protected override List<Item> CreateChildren(Item item, object xmlObject)
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


        private Item CreateXMLDocumentItem(Item parentItem, XmlDocument doc, string fileRestURL)
        {
            return CreateItem(parentItem, parentItem.Text, doc, "XML", fileRestURL);
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
            if (item.Tag is XmlDocument)
            {
                return "//*";
            } else if (item.Tag is XmlElement)
            {
                return CreateXpathRecursively(item.Tag as XmlElement);
            }
            else if (item.Tag is XmlAttribute)
            {
                return CreateXpathRecursively(item.Tag as XmlAttribute);
            }
            else
            {
                return null;
            }
        }

        private string CreateXpathRecursively(XmlElement element)
        {
            XmlNode parent = element.ParentNode;
            if (parent is XmlElement)
            {
                XmlElement parentElement = parent as XmlElement;
                XmlNodeList childrenWithCorrectName = parentElement.GetElementsByTagName(element.Name);

                string xPathSegment = "/" + element.Name;

                if (childrenWithCorrectName.Count > 1)
                {
                    for (int i = 0; i < childrenWithCorrectName.Count; i++)
                    {
                        if (childrenWithCorrectName[i] == element)
                        {
                            xPathSegment += "[" + (i+1) + "]";
                        }
                    }
                }

                string parentXPathSegment = CreateXpathRecursively(parentElement);

                return parentXPathSegment == null ? null : parentXPathSegment + xPathSegment;
            } else if (parent is XmlDocument)
            {
                return "///" + element.Name;
            } else
            {
                return null;
            }
        }

        private string CreateXpathRecursively(XmlAttribute attribute)
        {
            XmlElement parent = attribute.OwnerElement;
                       
            string xPathSegment = "/@" + attribute.Name;
            string parentXPathSegment = CreateXpathRecursively(parent as XmlElement);

            return parentXPathSegment == null ? null : parentXPathSegment + xPathSegment;
        }
    }
}
