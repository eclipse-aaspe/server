﻿using AasxServer;
using AasxServerBlazor.Models;
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

            for (int i = 0; i < Program.envimax; i++)
            {
                TreeNodeData root = new TreeNodeData();
                root.EnvIndex = i;
                if (Program.env[i] != null)
                {
                    root.Text = Program.env[i].AasEnv.AdministrationShells[0].idShort;
                    root.Tag = Program.env[i].AasEnv.AdministrationShells[0];
                    if (Program.envSymbols[i] != "L")
                    {
                        CreateViewFromAASEnv(root, Program.env[i].AasEnv, i);
                        viewItems.Add(root);
                    }
                }

                if (Program.envSymbols[i] == "L")
                {
                    root.Text = Path.GetFileName(Program.envFileName[i]);
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
                }
            }

            rootItem.Children = treeNodeDataList;

            foreach (TreeNodeData nodeData in treeNodeDataList)
            {
                nodeData.Parent = rootItem;
            }
        }

        private void CreateViewFromUACloudLibraryNodeset(TreeNodeData rootItem, Uri uri, int i)
        {
            List<TreeNodeData> treeNodeDataList = new List<TreeNodeData>();

            try
            {
                UANodesetViewer viewer = new UANodesetViewer();
                viewer.Login(uri.AbsoluteUri, Environment.GetEnvironmentVariable("UACLUsername"), Environment.GetEnvironmentVariable("UACLPassword"));

                NodesetViewerNode rootNode = viewer.GetRootNode().GetAwaiter().GetResult();
                if (rootNode.Children)
                {
                    List<NodesetViewerNode> children = viewer.GetChildren(rootNode.Id).GetAwaiter().GetResult();
                    foreach (NodesetViewerNode node in children)
                    {
                        TreeNodeData smeItem = new TreeNodeData();
                        smeItem.EnvIndex = i;
                        smeItem.Text = node.Text;
                        smeItem.Type = "UANode";
                        smeItem.Tag = new SubmodelElement() { idShort = node.Text };
                        treeNodeDataList.Add(smeItem);
                    }

                    rootItem.Children = treeNodeDataList;

                    foreach (TreeNodeData nodeData in treeNodeDataList)
                    {
                        nodeData.Parent = rootItem;
                    }
                }

                viewer.Disconnect();
            }
            catch (Exception)
            {
                // ignore this part of the AAS
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
