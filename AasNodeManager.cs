/* ========================================================================
 * Copyright (c) 2005-2019 The OPC Foundation, Inc. All rights reserved.
 *
 * OPC Foundation MIT License 1.00
 * 
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * The complete license agreement can be found here:
 * http://opcfoundation.org/License/MIT/1.00/
 * ======================================================================*/

using System.Collections.Generic;
using Opc.Ua;
using Opc.Ua.Sample;
using System.Reflection;
using AdminShellNS;
using System.Diagnostics;

namespace AasOpcUaServer
{
    /// <summary>
    /// A node manager the diagnostic information exposed by the server.
    /// </summary>
    public class AasNodeManager : SampleNodeManager
    {
        private AdminShell.PackageEnv [] thePackageEnv = null;
        
        #region Constructors
        /// <summary>
        /// Initializes the node manager.
        /// </summary>
        public AasNodeManager(
            Opc.Ua.Server.IServerInternal server, 
            ApplicationConfiguration configuration,
            AdminShell.PackageEnv [] env)
        :
            base(server)
        {
            thePackageEnv = env;

            List<string> namespaceUris = new List<string>();
            namespaceUris.Add("http://www.micha.com/123");
            namespaceUris.Add("http://www.micha.com/123" + "/Instance");
            NamespaceUris = namespaceUris;

            m_typeNamespaceIndex = Server.NamespaceUris.GetIndexOrAppend(namespaceUris[0]);
            m_namespaceIndex = Server.NamespaceUris.GetIndexOrAppend(namespaceUris[1]);

            m_lastUsedId = 0;
        }
        #endregion

        #region INodeIdFactory Members
        /// <summary>
        /// Creates the NodeId for the specified node.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="node">The node.</param>
        /// <returns>The new NodeId.</returns>
        public override NodeId New(ISystemContext context, NodeState node)
        {
            uint id = Utils.IncrementIdentifier(ref m_lastUsedId);
            return new NodeId(id, m_namespaceIndex);
            // return new NodeId(node.BrowseName.Name, m_namespaceIndex);
        }
        #endregion

        public NodeId NewFromParent(ISystemContext context, NodeState node, NodeState parent) {
            // create known node ids from the full path in the AAS
            // causes an exception if anything has more than one qualifier!
            if (parent == null) {
                return new NodeId(node.BrowseName.Name, m_namespaceIndex);
            }
            if (node.BrowseName.Name == "Qualifier")
            {
                return New(context, node);
            }
            else {
                return new NodeId(parent.NodeId.Identifier.ToString() + "." + node.BrowseName.Name, m_namespaceIndex);
            }
        }

        public NodeId NewType(ISystemContext context, NodeState node, uint preferredNumId = 0)
        {
            uint id = preferredNumId;
            if (id == 0)
                id = Utils.IncrementIdentifier(ref m_lastUsedTypeId);
            return new NodeId(preferredNumId, m_typeNamespaceIndex);
        }

        #region INodeManager Members
        /// <summary>
        /// Does any initialization required before the address space can be used.
        /// </summary>
        /// <remarks>
        /// The externalReferences is an out parameter that allows the node manager to link to nodes
        /// in other node managers. For example, the 'Objects' node is managed by the CoreNodeManager and
        /// should have a reference to the root folder node(s) exposed by this node manager.  
        /// </remarks>
        public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
        {
            lock (Lock)
            {
                base.CreateAddressSpace(externalReferences);

                // var env = new AdminShell.PackageEnv("Festo-USB-stick-sample-admin-shell.aasx");

                if (true)
                {
                    var builder = new AasEntityBuilder(this);
                    var x = builder.CreateAddObject(null, "AASROOT");
                    // this one is special, needs to link to external reference
                    this.AddExternalReference(new NodeId(85, 0), ReferenceTypeIds.Organizes, false, x.NodeId, externalReferences);

                    // builder.AasTypes.Asset.CreateAddInstanceObject(x, env.AasEnv.Assets[0]);
                    for (int i = 0; i < thePackageEnv.Length; i++)
                    {
                        if (thePackageEnv[i] != null)
                        {
                            builder.AasTypes.AAS.CreateAddInstanceObject(x, thePackageEnv[i].AasEnv, thePackageEnv[i].AasEnv.AdministrationShells[0]);
                        }
                    }
                }

                Debug.WriteLine("Done with custom address space?!");
                Utils.Trace("Done with custom address space?!");
            }
        }

        public void AddReference(NodeId node, IReference reference)
        {
            var dict = new Dictionary<NodeId, IList<IReference>>();
            dict.Add(node, new List<IReference>(new IReference[] { reference }));
            this.AddReferences(dict);
        }

        /// <summary>
        /// Loads a node set from a file or resource and addes them to the set of predefined nodes.
        /// </summary>
        protected override NodeStateCollection LoadPredefinedNodes(ISystemContext context)
        {
            NodeStateCollection predefinedNodes = new NodeStateCollection();
            // predefinedNodes.LoadFromBinaryResource(context, "Opc.Ua.Sample.Boiler.Boiler.PredefinedNodes.uanodes", this.GetType().GetTypeInfo().Assembly, true);
            return predefinedNodes;
        }

        /// <summary>
        /// Replaces the generic node with a node specific to the model.
        /// </summary>
        protected override NodeState AddBehaviourToPredefinedNode(ISystemContext context, NodeState predefinedNode)
        {
            return predefinedNode;
        }

        /// <summary>
        /// Does any processing after a monitored item is created.
        /// </summary>
        protected override void OnCreateMonitoredItem(
            ISystemContext systemContext,
            MonitoredItemCreateRequest itemToCreate,
            MonitoredNode monitoredNode,
            DataChangeMonitoredItem monitoredItem)
        {
            // TBD
        }

        /// <summary>
        /// Does any processing after a monitored item is created.
        /// </summary>
        protected override void OnModifyMonitoredItem(
            ISystemContext systemContext,
            MonitoredItemModifyRequest itemToModify,
            MonitoredNode monitoredNode,
            DataChangeMonitoredItem monitoredItem,
            double previousSamplingInterval)
        {
            // TBD
        }

        /// <summary>
        /// Does any processing after a monitored item is deleted.
        /// </summary>
        protected override void OnDeleteMonitoredItem(
            ISystemContext systemContext,
            MonitoredNode monitoredNode,
            DataChangeMonitoredItem monitoredItem)
        {
            // TBD
        }

        /// <summary>
        /// Does any processing after a monitored item is created.
        /// </summary>
        protected override void OnSetMonitoringMode(
            ISystemContext systemContext,
            MonitoredNode monitoredNode,
            DataChangeMonitoredItem monitoredItem,
            MonitoringMode previousMode,
            MonitoringMode currentMode)
        {
            // TBD
        }
        #endregion

        #region Private Fields
        private ushort m_namespaceIndex;
        private ushort m_typeNamespaceIndex;
        private long m_lastUsedId;
        private long m_lastUsedTypeId;
        #endregion
    }
}
