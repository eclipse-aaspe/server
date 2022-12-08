using System.Collections.Generic;
using static AasxServerBlazor.Pages.TreePage;
using static AdminShellNS.AdminShellV20;
using AasxServer;
using AasxRestServerLibrary;
using System.Xml;
using System.Xml.Linq;
using System.Linq;

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

            if (xmlObject is XDocument)
            {
                foreach (var child in (xmlObject as XDocument).Elements())
                {
                    children.Add(CreateXMLElementItem(item, child));
                }
            }
            else if (xmlObject is XElement)
            {
                foreach (var child in (xmlObject as XElement).Elements())
                {
                    children.Add(CreateXMLElementItem(item, child));
                }
                foreach (var child in (xmlObject as XElement).Attributes())
                {
                    children.Add(CreateXMLAttributeItem(item, child));
                }
            }

            return children;
        }


        private Item CreateXMLDocumentItem(Item parentItem, XDocument doc, string fileRestURL)
        {
            return CreateItem(parentItem, parentItem.Text, doc, "XML", fileRestURL);
        }

        private Item CreateXMLElementItem(Item parentItem, XElement node)
        {
            return CreateItem(parentItem, node.Name.ToString(), node, "Ele");
        }

        private Item CreateXMLAttributeItem(Item parentItem, XAttribute node)
        {
            return CreateItem(parentItem, node.Name.ToString(), node, "Att");
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

            if (tag is XElement)
            {
                var xmlElement = tag as XElement;

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
                            return xmlElement.Name.NamespaceName;
                        break;
                    default:
                        return null;
                }
            }
            else if (tag is XAttribute)
            {
                var xmlAttribute = tag as XAttribute;

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
                            return xmlAttribute.Name.NamespaceName;
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
            if (tag is XElement)
            {
                value = getTextValue(tag as XElement);

            }
            else if (tag is XAttribute)
            {
                value = (tag as XAttribute).Value;
            }
            return value?.Length > 0 ? " = " + value : null;
        }

        private string getTextValue(XElement xmlElement)
        {
            foreach (XNode child in xmlElement.Nodes())
            {
                if (child.NodeType == XmlNodeType.Text)
                {
                    return (child as XText).Value;
                }
                else if (child.NodeType == XmlNodeType.CDATA)
                {
                    return (child as XCData).Value;
                }
            }

            return null;
        }
        private string getComment(XElement xmlElement)
        {
            foreach (XNode child in xmlElement.Nodes())
            {
                if (child.NodeType == XmlNodeType.Comment)
                {
                    return (child as XComment).Value;
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
            if (item.Tag is XDocument)
            {
                return "/*";
            }
            else if (item.Tag is XElement)
            {
                return CreateXpathRecursively(item.Tag as XElement);
            }
            else if (item.Tag is XAttribute)
            {
                return CreateXpathRecursively(item.Tag as XAttribute);
            }
            else
            {
                return null;
            }
        }

        private string CreateXpathRecursively(XElement element)
        {
            XNode parent = element.Parent;
            if (parent is XElement)
            {
                XElement parentElement = parent as XElement;
                List<XElement> childrenWithCorrectName = parentElement.Elements(element.Name).ToList();

                string xPathSegment = "/" + GetLocalXpathExpression(element);

                if (childrenWithCorrectName.Count() > 1)
                {
                    for (int i = 0; i < childrenWithCorrectName.Count(); i++)
                    {
                        if (childrenWithCorrectName[i] == element)
                        {
                            xPathSegment += "[" + (i + 1) + "]";
                        }
                    }
                }

                string parentXPathSegment = CreateXpathRecursively(parentElement);

                return parentXPathSegment == null ? null : parentXPathSegment + xPathSegment;
            }
            else
            {
                return "/" + GetLocalXpathExpression(element);
            }
        }

        private string GetLocalXpathExpression(XElement node)
        {
            var nodeName = node.Name;
            if (nodeName.NamespaceName?.Length == 0)
            {
                // the node is not associated with a namespace, so we can simply use the local name as xPath expression
                return nodeName.LocalName;
            }

            var ns = nodeName.Namespace;
            var nsPrefix = node.GetPrefixOfNamespace(ns);

            if (nsPrefix?.Length > 0)
            {
                // there is a prefix for the namespace of the node so we can use this for the xPath expression
                return nsPrefix + ":" + nodeName.LocalName;
            }

            // the node is in the default namespace (without any prefix); hence, we need to use some special xPath syntax
            // to be able to adress the node (see https://stackoverflow.com/a/2530023)
            return "*[namespace-uri()='" + ns.NamespaceName + "' and local-name()='" + nodeName.LocalName + "']";
        }

        private string CreateXpathRecursively(XAttribute attribute)
        {
            XElement parent = attribute.Parent;
            string xPathSegment = "/@" + attribute.Name;
            string parentXPathSegment = CreateXpathRecursively(parent);

            return parentXPathSegment == null ? null : parentXPathSegment + xPathSegment;
        }
    }
}
