
using Opc.Ua;
using Opc.Ua.Export;
using Opc.Ua.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace AasxServerBlazor.Data
{
    public class SimpleNodeManager : CustomNodeManager2
    {
        public SimpleNodeManager(IServerInternal server, ApplicationConfiguration configuration)
        : base(server, configuration)
        {
            SystemContext.NodeIdFactory = this;

            List<string> namespaces = new List<string>();
            foreach (string nodesetFile in UANodesetViewer._nodeSetFilenames)
            {
                using (Stream stream = new FileStream(nodesetFile, FileMode.Open))
                {
                    UANodeSet nodeSet = UANodeSet.Read(stream);
                    if ((nodeSet.NamespaceUris != null) && (nodeSet.NamespaceUris.Length > 0))
                    {
                        foreach (string ns in nodeSet.NamespaceUris)
                        {
                            if (!namespaces.Contains(ns))
                            {
                                namespaces.Add(ns);
                            }
                        }
                    }
                }
            }

            NamespaceUris = namespaces.ToArray();
        }

        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            lock (Lock)
            {
                IList<IReference> references = null;
                if (!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out references))
                {
                    externalReferences[ObjectIds.ObjectsFolder] = references = new List<IReference>();
                }

                if (UANodesetViewer._nodeSetFilenames.Count > 0)
                {
                    // we need as many passes as we have nodesetfiles to make sure all references can be resolved
                    for (int i = 0; i < UANodesetViewer._nodeSetFilenames.Count; i++)
                    {
                        foreach (string nodesetFile in UANodesetViewer._nodeSetFilenames)
                        {
                            ImportNodeset2Xml(externalReferences, nodesetFile, i);
                        }

                        Console.WriteLine("Import nodes pass " + i.ToString() + " completed!");
                    }
                }

                AddReverseReferences(externalReferences);
            }
        }

        private void ImportNodeset2Xml(IDictionary<NodeId, IList<IReference>> externalReferences, string resourcepath, int pass)
        {
            using (Stream stream = new FileStream(resourcepath, FileMode.Open))
            {
                UANodeSet nodeSet = UANodeSet.Read(stream);

                NodeStateCollection predefinedNodes = new NodeStateCollection();
                nodeSet.Import(SystemContext, predefinedNodes);
                
                for (int i = 0; i < predefinedNodes.Count; i++)
                {
                    try
                    {
                        AddPredefinedNode(SystemContext, predefinedNodes[i]);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Pass " + pass.ToString() + ": Importing node ns=" + predefinedNodes[i].NodeId.NamespaceIndex + ";i=" + predefinedNodes[i].NodeId.Identifier + " (" + predefinedNodes[i].DisplayName + ") failed with error: " + ex.Message);
                    }
                }
            }
        }
    }
}
