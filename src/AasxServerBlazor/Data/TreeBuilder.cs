using AasxServer;
using AasxServerBlazor.Models;
using Aml.Engine.CAEX;
using System;
using System.Collections.Generic;
using System.IO;
using static AasxServerBlazor.Pages.TreePage;
using static AdminShellNS.AdminShellV20;

namespace AasxServerBlazor.Data
{
    public class TreeBuilder
    {
        public List<TreeNodeData> BuildTree()
        {
            List<TreeNodeData> viewItems = new List<TreeNodeData>();

            for (int i = 0; i < Program.env.Count; i++)
            {
                TreeNodeData root = new TreeNodeData();
                root.EnvIndex = i;
                if (Program.env[i] != null)
                {
                    root.Text = Program.env[i].AasEnv.AdministrationShells[0].idShort;
                    root.Tag = Program.env[i].AasEnv.AdministrationShells[0];
                    CreateViewFromAASEnv(root, Program.env[i].AasEnv, i);
                    viewItems.Add(root);
                }
            }

            return viewItems;
        }

        private void CreateViewFromAASEnv(TreeNodeData root, AdministrationShellEnv aasEnv, int i)
        {
            List<TreeNodeData> subModelTreeNodeDataList = new List<TreeNodeData>();
            foreach (Submodel subModel in aasEnv.Submodels)
            {
                if (subModel != null && subModel.idShort != null)
                {
                    TreeNodeData subModelTreeNodeData = new TreeNodeData();
                    subModelTreeNodeData.EnvIndex = i;
                    subModelTreeNodeData.Text = subModel.idShort;
                    subModelTreeNodeData.Tag = subModel;
                    subModelTreeNodeDataList.Add(subModelTreeNodeData);
                    CreateViewFromSubModel(subModelTreeNodeData, subModel, i);
                }
            }

            root.Children = subModelTreeNodeDataList;

            foreach (TreeNodeData nodeData in subModelTreeNodeDataList)
            {
                nodeData.Parent = root;
            }
        }

        private void CreateViewFromSubModel(TreeNodeData rootItem, Submodel subModel, int i)
        {
            List<TreeNodeData> subModelElementTreeNodeDataList = new List<TreeNodeData>();
            foreach (SubmodelElementWrapper subModelElementWrapper in subModel.submodelElements)
            {
                TreeNodeData subModelElementTreeNodeData = new TreeNodeData();
                subModelElementTreeNodeData.EnvIndex = i;
                subModelElementTreeNodeData.Text = subModelElementWrapper.submodelElement.idShort;
                subModelElementTreeNodeData.Tag = subModelElementWrapper.submodelElement;
                subModelElementTreeNodeDataList.Add(subModelElementTreeNodeData);

                if (subModelElementWrapper.submodelElement is SubmodelElementCollection)
                {
                    SubmodelElementCollection submodelElementCollection = subModelElementWrapper.submodelElement as SubmodelElementCollection;
                    CreateViewFromSubModelElementCollection(subModelElementTreeNodeData, submodelElementCollection, i);
                }

                if (subModelElementWrapper.submodelElement is Operation)
                {
                    Operation operation = subModelElementWrapper.submodelElement as Operation;
                    CreateViewFromOperation(subModelElementTreeNodeData, operation, i);
                }

                if (subModelElementWrapper.submodelElement is Entity)
                {
                    Entity entity = subModelElementWrapper.submodelElement as Entity;
                    CreateViewFromEntity(subModelElementTreeNodeData, entity, i);
                }
            }

            rootItem.Children = subModelElementTreeNodeDataList;

            foreach (TreeNodeData nodeData in subModelElementTreeNodeDataList)
            {
                nodeData.Parent = rootItem;
            }
        }

        private void CreateViewFromSubModelElementCollection(TreeNodeData rootItem, SubmodelElementCollection subModelElementCollection, int i)
        {
            List<TreeNodeData> treeNodeDataList = new List<TreeNodeData>();
            foreach (SubmodelElementWrapper subModelElementWrapper in subModelElementCollection.value)
            {
                if (subModelElementWrapper != null && subModelElementWrapper.submodelElement != null)
                {
                    TreeNodeData smeItem = new TreeNodeData();
                    smeItem.EnvIndex = i;
                    smeItem.Text = subModelElementWrapper.submodelElement.idShort;
                    smeItem.Tag = subModelElementWrapper.submodelElement;
                    treeNodeDataList.Add(smeItem);

                    if (subModelElementWrapper.submodelElement is SubmodelElementCollection)
                    {
                        SubmodelElementCollection smecNext = subModelElementWrapper.submodelElement as SubmodelElementCollection;
                        CreateViewFromSubModelElementCollection(smeItem, smecNext, i);
                    }

                    if (subModelElementWrapper.submodelElement is Operation)
                    {
                        Operation operation = subModelElementWrapper.submodelElement as Operation;
                        CreateViewFromOperation(smeItem, operation, i);
                    }

                    if (subModelElementWrapper.submodelElement is Entity)
                    {
                        Entity entity = subModelElementWrapper.submodelElement as Entity;
                        CreateViewFromEntity(smeItem, entity, i);
                    }

                    if ((subModelElementWrapper.submodelElement.idShort == "NODESET2_XML")
                    && Uri.IsWellFormedUriString(subModelElementWrapper.submodelElement.ValueAsText(), UriKind.Absolute))
                    {
                        CreateViewFromUACloudLibraryNodeset(smeItem, new Uri(subModelElementWrapper.submodelElement.ValueAsText()), i);
                    }

                    if (subModelElementWrapper.submodelElement.idShort == "CAEX")

                    {
                        CreateViewFromAMLCAEXFile(smeItem, subModelElementWrapper.submodelElement.ValueAsText(), i);
                    }
                }
            }

            rootItem.Children = treeNodeDataList;

            foreach (TreeNodeData nodeData in treeNodeDataList)
            {
                nodeData.Parent = rootItem;
            }
        }

        private void CreateViewFromAMLCAEXFile(TreeNodeData rootItem, string filename, int i)
        {
            try
            {
                Stream packagePartStream = Program.env[i].GetLocalStreamFromPackage(filename);
                CAEXDocument doc = CAEXDocument.LoadFromStream(packagePartStream);
                List<TreeNodeData> treeNodeDataList = new List<TreeNodeData>();

                foreach (var instanceHirarchy in doc.CAEXFile.InstanceHierarchy)
                {
                    TreeNodeData smeItem = new TreeNodeData();
                    smeItem.EnvIndex = i;
                    smeItem.Text = instanceHirarchy.ID;
                    smeItem.Type = "AML";
                    smeItem.Tag = new SubmodelElement() { idShort = instanceHirarchy.Name };
                    smeItem.Children = new List<TreeNodeData>();
                    treeNodeDataList.Add(smeItem);

                    foreach (var internalElement in instanceHirarchy.InternalElement)
                    {
                        CreateViewFromInternalElement(smeItem, (List<TreeNodeData>)smeItem.Children, internalElement, i);
                    }
                }

                foreach (var roleclassLib in doc.CAEXFile.RoleClassLib)
                {
                    TreeNodeData smeItem = new TreeNodeData();
                    smeItem.EnvIndex = i;
                    smeItem.Text = roleclassLib.ID;
                    smeItem.Type = "AML";
                    smeItem.Tag = new SubmodelElement() { idShort = roleclassLib.Name };
                    smeItem.Children = new List<TreeNodeData>();
                    treeNodeDataList.Add(smeItem);

                    foreach (RoleFamilyType roleClass in roleclassLib.RoleClass)
                    {
                        CreateViewFromRoleClasses(smeItem, (List<TreeNodeData>)smeItem.Children, roleClass, i);
                    }
                }

                foreach (var systemUnitClassLib in doc.CAEXFile.SystemUnitClassLib)
                {
                    TreeNodeData smeItem = new TreeNodeData();
                    smeItem.EnvIndex = i;
                    smeItem.Text = systemUnitClassLib.ID;
                    smeItem.Type = "AML";
                    smeItem.Tag = new SubmodelElement() { idShort = systemUnitClassLib.Name };
                    smeItem.Children = new List<TreeNodeData>();
                    treeNodeDataList.Add(smeItem);

                    foreach (SystemUnitFamilyType systemUnitClass in systemUnitClassLib.SystemUnitClass)
                    {
                        CreateViewFromSystemUnitClasses(smeItem, (List<TreeNodeData>)smeItem.Children, systemUnitClass, i);
                    }
                }

                rootItem.Children = treeNodeDataList;

                foreach (TreeNodeData nodeData in treeNodeDataList)
                {
                    nodeData.Parent = rootItem;
                }
            }
            catch (Exception ex)
            {
                // ignore this node
                Console.WriteLine(ex);
            }
        }

        private void CreateViewFromInternalElement(TreeNodeData rootItem, List<TreeNodeData> rootItemChildren, InternalElementType internalElement, int i)
        {
            TreeNodeData smeItem = new TreeNodeData();
            smeItem.EnvIndex = i;
            smeItem.Text = internalElement.ID;
            smeItem.Type = "AML";
            smeItem.Tag = new SubmodelElement() { idShort = internalElement.Name };
            smeItem.Parent = rootItem;
            smeItem.Children = new List<TreeNodeData>();
            rootItemChildren.Add(smeItem);

            foreach (InternalElementType childInternalElement in internalElement.InternalElement)
            {
                CreateViewFromInternalElement(smeItem, (List<TreeNodeData>)smeItem.Children, childInternalElement, i);
            }
        }

        private void CreateViewFromRoleClasses(TreeNodeData rootItem, List<TreeNodeData> rootItemChildren, RoleFamilyType roleClass, int i)
        {
            TreeNodeData smeItem = new TreeNodeData();
            smeItem.EnvIndex = i;
            smeItem.Text = roleClass.ID;
            smeItem.Type = "AML";
            smeItem.Tag = new SubmodelElement() { idShort = roleClass.Name };
            smeItem.Parent = rootItem;
            smeItem.Children = new List<TreeNodeData>();
            rootItemChildren.Add(smeItem);

            foreach (RoleFamilyType childRoleClass in roleClass.RoleClass)
            {
                CreateViewFromRoleClasses(smeItem, (List<TreeNodeData>)smeItem.Children, childRoleClass, i);
            }
        }

        private void CreateViewFromSystemUnitClasses(TreeNodeData rootItem, List<TreeNodeData> rootItemChildren, SystemUnitFamilyType systemUnitClass, int i)
        {
            TreeNodeData smeItem = new TreeNodeData();
            smeItem.EnvIndex = i;
            smeItem.Text = systemUnitClass.ID;
            smeItem.Type = "AML";
            smeItem.Tag = new SubmodelElement() { idShort = systemUnitClass.Name };
            smeItem.Parent = rootItem;
            smeItem.Children = new List<TreeNodeData>();
            rootItemChildren.Add(smeItem);

            foreach (InternalElementType childInternalElement in systemUnitClass.InternalElement)
            {
                CreateViewFromInternalElement(smeItem, (List<TreeNodeData>)smeItem.Children, childInternalElement, i);
            }

            foreach (SystemUnitFamilyType childSystemUnitClass in systemUnitClass.SystemUnitClass)
            {
                CreateViewFromSystemUnitClasses(smeItem, (List<TreeNodeData>)smeItem.Children, childSystemUnitClass, i);
            }
        }

        private void CreateViewFromUACloudLibraryNodeset(TreeNodeData rootItem, Uri uri, int i)
        {
            try
            {
                UANodesetViewer viewer = new UANodesetViewer();
                viewer.Login(uri.AbsoluteUri, Environment.GetEnvironmentVariable("UACLUsername"), Environment.GetEnvironmentVariable("UACLPassword"));

                NodesetViewerNode rootNode = viewer.GetRootNode().GetAwaiter().GetResult();
                if (rootNode.Children)
                {
                    CreateViewFromUANode(rootItem, viewer, rootNode, i);
                }

                viewer.Disconnect();
            }
            catch (Exception ex)
            {
                // ignore this part of the AAS
                Console.WriteLine(ex);
            }
        }

        private void CreateViewFromUANode(TreeNodeData rootItem, UANodesetViewer viewer, NodesetViewerNode rootNode, int i)
        {
            try
            {
                List<TreeNodeData> treeNodeDataList = new List<TreeNodeData>();
                List<NodesetViewerNode> children = viewer.GetChildren(rootNode.Id).GetAwaiter().GetResult();
                foreach (NodesetViewerNode node in children)
                {
                    TreeNodeData smeItem = new TreeNodeData();
                    smeItem.EnvIndex = i;
                    smeItem.Text = node.Text;
                    smeItem.Type = "UANode";
                    smeItem.Tag = new SubmodelElement() { idShort = node.Text };
                    treeNodeDataList.Add(smeItem);

                    if (node.Children)
                    {
                        CreateViewFromUANode(smeItem, viewer, node, i);
                    }
                }

                rootItem.Children = treeNodeDataList;

                foreach (TreeNodeData nodeData in treeNodeDataList)
                {
                    nodeData.Parent = rootItem;
                }
            }
            catch (Exception ex)
            {
                // ignore this node
                Console.WriteLine(ex);
            }
        }

        private void CreateViewFromOperation(TreeNodeData rootItem, Operation operation, int i)
        {
            List<TreeNodeData> treeNodeDataList = new List<TreeNodeData>();
            foreach (OperationVariable v in operation.inputVariable)
            {
                TreeNodeData smeItem = new TreeNodeData();
                smeItem.EnvIndex = i;
                smeItem.Text = v.value.submodelElement.idShort;
                smeItem.Type = "In";
                smeItem.Tag = v.value.submodelElement;
                treeNodeDataList.Add(smeItem);
            }

            foreach (OperationVariable v in operation.outputVariable)
            {
                TreeNodeData smeItem = new TreeNodeData();
                smeItem.EnvIndex = i;
                smeItem.Text = v.value.submodelElement.idShort;
                smeItem.Type = "Out";
                smeItem.Tag = v.value.submodelElement;
                treeNodeDataList.Add(smeItem);
            }

            foreach (OperationVariable v in operation.inoutputVariable)
            {
                TreeNodeData smeItem = new TreeNodeData();
                smeItem.EnvIndex = i;
                smeItem.Text = v.value.submodelElement.idShort;
                smeItem.Type = "InOut";
                smeItem.Tag = v.value.submodelElement;
                treeNodeDataList.Add(smeItem);
            }

            rootItem.Children = treeNodeDataList;

            foreach (TreeNodeData nodeData in treeNodeDataList)
            {
                nodeData.Parent = rootItem;
            }
        }

        private void CreateViewFromEntity(TreeNodeData rootItem, Entity entity, int i)
        {
            List<TreeNodeData> treeNodeDataList = new List<TreeNodeData>();
            foreach (SubmodelElementWrapper statement in entity.statements)
            {
                if (statement != null && statement.submodelElement != null)
                {
                    TreeNodeData smeItem = new TreeNodeData();
                    smeItem.EnvIndex = i;
                    smeItem.Text = statement.submodelElement.idShort;
                    smeItem.Type = "In";
                    smeItem.Tag = statement.submodelElement;
                    treeNodeDataList.Add(smeItem);
                }
            }

            rootItem.Children = treeNodeDataList;

            foreach (TreeNodeData nodeData in treeNodeDataList)
            {
                nodeData.Parent = rootItem;
            }
        }
    }
}
