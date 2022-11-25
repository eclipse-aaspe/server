using System.Collections.Generic;
using static AasxServerBlazor.Pages.TreePage;
using static AdminShellNS.AdminShellV20;
using AasxServer;
using AasxRestServerLibrary;
using Aml.Engine.CAEX;

namespace AasxServerBlazor.Data
{
    public class AASServiceAMLExtension : IAASServiceExtension
    {
        public bool IsSuitableFor(File file)
        {
            var filePath = file.value;

            // currently, we only check the file extension; maybe, other checks (e.g. for certain semantic ids)
            // should be implemented in the future
            return filePath.EndsWith("aml") || filePath.EndsWith("amlx") || filePath.EndsWith("mtp");
        }

        public void CreateItems(Item caexFileItem, File caexFile)
        {
            try
            {
                var filePath = caexFile.value;
                var fileStream = Program.env[caexFileItem.envIndex].GetLocalStreamFromPackage(filePath);
                var caexDocument = AasxHttpContextHelperAmlExtensions.LoadCaexDocument(fileStream);

                var aml = CreateCAEXDocumentItem(caexFileItem, caexDocument);

                caexFileItem.Childs = new List<Item>() { aml };

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

            return item;
        }

        private List<Item> CreateChildren(Item item, object caexObject)
        {
            var children = new List<Item>();

            if (caexObject is CAEXFileType)
            {
                children.Add(CreateItem(item, "Instance Hierarchies", (caexObject as CAEXFileType).InstanceHierarchy, "AML"));
                children.Add(CreateItem(item, "System Unit Class Libraries", (caexObject as CAEXFileType).SystemUnitClassLib, "AML"));
                children.Add(CreateItem(item, "Role Class Libraries", (caexObject as CAEXFileType).RoleClassLib, "AML"));
                children.Add(CreateItem(item, "Interface Class Libraries", (caexObject as CAEXFileType).InterfaceClassLib, "AML"));
                children.Add(CreateItem(item, "Attribute Type Libraries", (caexObject as CAEXFileType).AttributeTypeLib, "AML"));
            }

            if (caexObject is IObjectWithAttributes)
            {
                foreach (var child in (caexObject as IObjectWithAttributes).Attribute)
                {
                    children.Add(CreateATItem(item, child));
                }
            }

            if (caexObject is CAEXSequenceOfCAEXObjects<InstanceHierarchyType>)
            {
                foreach (var child in (caexObject as CAEXSequenceOfCAEXObjects<InstanceHierarchyType>))
                {
                    children.Add(CreateIHItem(item, child));
                }
            }

            if (caexObject is CAEXSequenceOfCAEXObjects<SystemUnitClassLibType>)
            {
                foreach (var child in (caexObject as CAEXSequenceOfCAEXObjects<SystemUnitClassLibType>))
                {
                    children.Add(CreateSUCLibItem(item, child));
                }
            }

            if (caexObject is SystemUnitClassLibType)
            {
                foreach (var child in (caexObject as SystemUnitClassLibType).SystemUnitClass)
                {
                    children.Add(CreateSUCItem(item, child));
                }
            }

            if (caexObject is CAEXSequenceOfCAEXObjects<RoleClassLibType>)
            {
                foreach (var child in (caexObject as CAEXSequenceOfCAEXObjects<RoleClassLibType>))
                {
                    children.Add(CreateRCLibItem(item, child));
                }
            }

            if (caexObject is RoleClassLibType)
            {
                foreach (var child in (caexObject as RoleClassLibType).RoleClass)
                {
                    children.Add(CreateRCItem(item, child));
                }
            }

            if (caexObject is CAEXSequenceOfCAEXObjects<InterfaceClassLibType>)
            {
                foreach (var child in (caexObject as CAEXSequenceOfCAEXObjects<InterfaceClassLibType>))
                {
                    children.Add(CreateICLibItem(item, child));
                }
            }

            if (caexObject is InterfaceClassLibType)
            {
                foreach (var child in (caexObject as InterfaceClassLibType).InterfaceClass)
                {
                    children.Add(CreateICItem(item, child));
                }
            }

            if (caexObject is CAEXSequenceOfCAEXObjects<AttributeTypeLibType>)
            {
                foreach (var child in (caexObject as CAEXSequenceOfCAEXObjects<AttributeTypeLibType>))
                {
                    children.Add(CreateATLibItem(item, child));
                }
            }

            if (caexObject is AttributeTypeLibType)
            {
                foreach (var child in (caexObject as AttributeTypeLibType).AttributeType)
                {
                    children.Add(CreateATItem(item, child));
                }
            }

            if (caexObject is SystemUnitFamilyType)
            {
                foreach (var child in (caexObject as SystemUnitFamilyType).SystemUnitClass)
                {
                    children.Add(CreateSUCItem(item, child));
                }
            }

            if (caexObject is SystemUnitClassType)
            {
                foreach (var child in (caexObject as SystemUnitClassType).SupportedRoleClass)
                {
                    children.Add(CreateSRCItem(item, child));
                }
            }

            if (caexObject is InternalElementType)
            {
                foreach (var child in (caexObject as InternalElementType).RoleRequirements)
                {
                    children.Add(CreateRRItem(item, child));
                }
            }

            if (caexObject is IInternalElementContainer)
            {
                foreach (var child in (caexObject as IInternalElementContainer).InternalElement)
                {
                    children.Add(CreateIEItem(item, child));
                }
            }

            if (caexObject is IObjectWithExternalInterface)
            {
                foreach (var child in (caexObject as IObjectWithExternalInterface).ExternalInterface)
                {
                    children.Add(CreateEIItem(item, child));
                }
            }

            if (caexObject is SystemUnitClassType)
            {
                foreach (var child in (caexObject as SystemUnitClassType).InternalLink)
                {
                    children.Add(CreateILItem(item, child));
                }
            }

            if (caexObject is InterfaceFamilyType)
            {
                foreach (var child in (caexObject as InterfaceFamilyType).InterfaceClass)
                {
                    children.Add(CreateICItem(item, child));
                }
            }

            return children;
        }


        private Item CreateCAEXDocumentItem(Item parentItem, CAEXDocument doc)
        {
            return CreateItem(parentItem, doc.CAEXFile.FileName, doc.CAEXFile, "AML");
        }

        private Item CreateIHItem(Item parentItem, InstanceHierarchyType ih)
        {
            return CreateItem(parentItem, ih.Name, ih, "IH");
        }

        private Item CreateSUCLibItem(Item parentItem, SystemUnitClassLibType sucLib)
        {
            return CreateItem(parentItem, sucLib.Name, sucLib, "SUCLib");
        }

        private Item CreateRCLibItem(Item parentItem, RoleClassLibType rcLib)
        {
            return CreateItem(parentItem, rcLib.Name, rcLib, "RCLib");
        }

        private Item CreateICLibItem(Item parentItem, InterfaceClassLibType icLib)
        {
            return CreateItem(parentItem, icLib.Name, icLib, "ICLib");
        }

        private Item CreateATLibItem(Item parentItem, AttributeTypeLibType atLib)
        {
            return CreateItem(parentItem, atLib.Name, atLib, "ATLib");
        }

        private Item CreateIEItem(Item parentItem, InternalElementType ie)
        {
            return CreateItem(parentItem, ie.Name, ie, "IE");
        }

        private Item CreateATItem(Item parentItem, AttributeType att)
        {
            return CreateItem(parentItem, att.Name, att, "Att");
        }

        private Item CreateEIItem(Item parentItem, ExternalInterfaceType ei)
        {
            return CreateItem(parentItem, ei.Name, ei, "EI");
        }

        private Item CreateILItem(Item parentItem, InternalLinkType il)
        {
            return CreateItem(parentItem, il.Name, il, "IL");
        }

        private Item CreateSUCItem(Item parentItem, SystemUnitFamilyType suc)
        {
            return CreateItem(parentItem, suc.Name, suc, "SUC");
        }

        private Item CreateSRCItem(Item parentItem, SupportedRoleClassType src)
        {
            return CreateItem(parentItem, src.RoleClass.Name, src, "SRC");
        }

        private Item CreateRRItem(Item parentItem, RoleRequirementsType rr)
        {
            return CreateItem(parentItem, rr.RoleClass.Name, rr, "RR");
        }

        private Item CreateRCItem(Item parentItem, RoleFamilyType rc)
        {
            return CreateItem(parentItem, rc.Name, rc, "RC");
        }

        private Item CreateICItem(Item parentItem, InterfaceFamilyType ic)
        {
            return CreateItem(parentItem, ic.Name, ic, "IC");
        }

        private Item CreateATItem(Item parentItem, AttributeFamilyType at)
        {
            return CreateItem(parentItem, at.Name, at, "AT");
        }
    }
}
