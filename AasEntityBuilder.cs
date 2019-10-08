using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AasOpcUaServer
{
    public class AasEntityBuilder
    {
        //// Static singleton for AAS entity builders
        // ugly, but simple: the singleton variables gives access to information
        //
        public static AasNodeManager nodeMgr = null;

        public AasEntityBuilder (AasNodeManager nodeMgr)
        {
            AasEntityBuilder.nodeMgr = nodeMgr;
            this.aasTypes = new AasTypeEntities(this);
        }

        //// references
        //

        public class AasReference : IReference
        {
            // private members
            private NodeId referenceTypeId = null;
            private bool isInverse = false;
            private ExpandedNodeId targetId = null;

            // public getters for IReference
            public NodeId ReferenceTypeId { get { return referenceTypeId; } }
            public bool IsInverse { get { return isInverse; } }
            public ExpandedNodeId TargetId { get { return targetId; } }

            public AasReference()
            {
            }

            public AasReference(NodeId referenceTypeId, bool isInverse, ExpandedNodeId targetId)
            {
                this.referenceTypeId = referenceTypeId;
                this.isInverse = isInverse;
                this.targetId = targetId;
            }
        }

        //// Object types
        //

        public class AasObjectTypeState : BaseObjectTypeState
        {
            public AasObjectTypeState()
                : base()
            { }
        }

        public AasObjectTypeState CreateAddObjectType(string browseDisplayName, NodeId superTypeId, uint preferredNumId = 0)
        {
            var x = new AasObjectTypeState();
            x.BrowseName = "" + browseDisplayName;
            x.DisplayName = "" + browseDisplayName;
            x.Description = new LocalizedText("en", browseDisplayName);
            x.SuperTypeId = superTypeId; 
            x.NodeId = nodeMgr.NewType(nodeMgr.SystemContext, x, preferredNumId);
            nodeMgr.AddPredefinedNode(nodeMgr.SystemContext, x);

            // TODO: with Unified Automation, setting super type automatically seems to make inverse rel in type branch?

            return x;
        }

        //// Variable types
        //

        public class AasVariableTypeState : BaseVariableTypeState
        {
            public AasVariableTypeState()
                : base()
            { }
        }

        public AasVariableTypeState CreateAddVariableType(string browseDisplayName, NodeId superTypeId, uint preferredNumId = 0)
        {
            var x = new AasVariableTypeState();
            x.BrowseName = "" + browseDisplayName;
            x.DisplayName = "" + browseDisplayName;
            x.Description = new LocalizedText("en", browseDisplayName);
            x.SuperTypeId = superTypeId;
            x.NodeId = nodeMgr.NewType(nodeMgr.SystemContext, x, preferredNumId);

            nodeMgr.AddPredefinedNode(nodeMgr.SystemContext, x);
            return x;
        }

        //// Objects
        //

        public class AasObjectState : BaseObjectState
        {
            public AasObjectState(NodeState parent) 
                : base(parent)
            { }
        }

        static int uniqueIDCount = 0;
        public AasObjectState CreateAddObject (NodeState parent, string browseDisplayName, NodeId referenceTypeFromParentId = null, NodeId typeDefinitionId = null)
        {
            var x = new AasObjectState(parent);
            x.BrowseName = "" + browseDisplayName;
            x.DisplayName = "" + browseDisplayName;
            x.Description = new LocalizedText("en", browseDisplayName);
            x.NodeId = nodeMgr.NewFromParent(nodeMgr.SystemContext, x, parent);

            nodeMgr.AddPredefinedNode(nodeMgr.SystemContext, x);
            if (parent != null)
                parent.AddChild(x);

            if (referenceTypeFromParentId != null)
            {
                try
                {
                    parent.AddReference(referenceTypeFromParentId, false, x.NodeId);
                }
                catch
                {
                    if (uniqueIDCount == 0)
                    {
                        Console.WriteLine("OPC UA NodeIDs corrected:");
                    }
                    x.NodeId += " @" + (++uniqueIDCount).ToString();
                    Console.WriteLine(x.NodeId);
                }
                if (referenceTypeFromParentId == ReferenceTypeIds.HasComponent)
                    x.AddReference(referenceTypeFromParentId, true, parent.NodeId);
                if (referenceTypeFromParentId == ReferenceTypeIds.HasProperty)
                    x.AddReference(referenceTypeFromParentId, true, parent.NodeId);
                // nodeMgr.AddReference(parentNodeId, new AasReference(referenceTypeId, false, x.NodeId));
                }

            if (typeDefinitionId != null)
            {
                x.AddReference(ReferenceTypeIds.HasTypeDefinition, false, typeDefinitionId);
                // nodeMgr.AddReference(x.NodeId, new AasReference(ReferenceTypeIds.HasTypeDefinition, false, typeDefinitionId));
            }

            return x;
        }

        //// Variables
        //

        /*
        public class AasVariableState : BaseVariableState
        {
            public AasVariableState(NodeState parent)
                : base(parent)
            { }
        }

        public AasVariableState CreateAddVariable(string browseDisplayName, NodeId dataTypeId, NodeId referenceTypeId = null, NodeId parentNodeId = null, 
            Variant? value = null)
        {
            var x = new AasVariableState(null);
            x.BrowseName = "" + browseDisplayName;
            x.DisplayName = "" + browseDisplayName;
            x.Description = new LocalizedText("en", browseDisplayName);
            x.DataType = dataTypeId;
            if (value != null)
                x.Value = value;
            x.NodeId = nodeMgr.New(nodeMgr.SystemContext, x);
            nodeMgr.AddPredefinedNode(nodeMgr.SystemContext, x);

            if (referenceTypeId != null && parentNodeId != null)
            {
                nodeMgr.AddReference(parentNodeId, new AasReference(referenceTypeId, false, x.NodeId));
            }

            return x;
        }
        */

        public PropertyState<T> CreateAddPropertyState<T>(NodeState parent, string browseDisplayName, NodeId dataTypeId, T value, NodeId referenceTypeFromParentId = null, NodeId typeDefinitionId = null, int valueRank = -2) 
        {
            var x = new PropertyState<T>(parent);
            x.BrowseName = "" + browseDisplayName;
            x.DisplayName = "" + browseDisplayName;
            x.Description = new LocalizedText("en", browseDisplayName);
            x.DataType = dataTypeId;
            if (valueRank > -2)
                x.ValueRank = valueRank;
            x.Value = (T) value;
            x.NodeId = nodeMgr.NewFromParent(nodeMgr.SystemContext, x, parent);

            nodeMgr.AddPredefinedNode(nodeMgr.SystemContext, x);
            if (parent != null)
                parent.AddChild(x);

            if (referenceTypeFromParentId != null)
            {
                parent.AddReference(referenceTypeFromParentId, false, x.NodeId);
                if (referenceTypeFromParentId == ReferenceTypeIds.HasComponent)
                    x.AddReference(referenceTypeFromParentId, true, parent.NodeId);
                if (referenceTypeFromParentId == ReferenceTypeIds.HasProperty)
                    x.AddReference(referenceTypeFromParentId, true, parent.NodeId);
                /*
                nodeMgr.AddReference(parentNodeId, new AasReference(referenceTypeId, false, x.NodeId));
                if (referenceTypeId == ReferenceTypeIds.HasComponent)
                    nodeMgr.AddReference(x.NodeId, new AasReference(ReferenceTypeIds.HasComponent, true, parentNodeId));
                if (referenceTypeId == ReferenceTypeIds.HasProperty)
                    nodeMgr.AddReference(x.NodeId, new AasReference(ReferenceTypeIds.HasProperty, true, parentNodeId));
                */
            }
            if (typeDefinitionId != null)
            {
                x.AddReference(ReferenceTypeIds.HasTypeDefinition, false, typeDefinitionId);
                /*
                nodeMgr.AddReference(x.NodeId, new AasReference(ReferenceTypeIds.HasTypeDefinition, false, typeDefinitionId));
                */
            }

            return x;
        }

        public MethodState CreateAddMethodState(NodeState parent, string browseDisplayName, Argument[] inputArgs = null, Argument[] outputArgs = null, NodeId referenceTypeFromParentId = null)
        {
            // method node
            var m = new MethodState(parent);
            m.BrowseName = "" + browseDisplayName;
            m.DisplayName = "" + browseDisplayName;
            m.Description = new LocalizedText("en", browseDisplayName);
            m.NodeId = nodeMgr.New(nodeMgr.SystemContext, m);

            nodeMgr.AddPredefinedNode(nodeMgr.SystemContext, m);
            if (parent != null)
                parent.AddChild(m);

            if (referenceTypeFromParentId != null)
            {
                parent.AddReference(referenceTypeFromParentId, false, m.NodeId);
                if (referenceTypeFromParentId == ReferenceTypeIds.HasComponent)
                    m.AddReference(referenceTypeFromParentId, true, parent.NodeId);
                if (referenceTypeFromParentId == ReferenceTypeIds.HasProperty)
                    m.AddReference(referenceTypeFromParentId, true, parent.NodeId);
                // nodeMgr.AddReference(parentNodeId, new AasReference(referenceTypeId, false, m.NodeId));
            }

            // can have inputs, outputs
            for (int i=0; i<2; i++)
            {
                // pretty argument list
                var arguments = (i == 0) ? inputArgs : outputArgs;
                if (arguments == null || arguments.Length < 1)
                    continue;

                // make a property for this
                var prop = CreateAddPropertyState<Argument[]>(
                    m,
                    (i == 0) ? "InputArguments" : "OutputArguments",
                    DataTypeIds.Argument,
                    arguments,
                    ReferenceTypeIds.HasProperty,
                    typeDefinitionId: VariableTypeIds.PropertyType,
                    valueRank: 1);

            }

            return m;
        }


        //// Entities
        //

        public class AasTypeEntities
        {
            public AasUaEntityIdentification Identification;
            public AasUaEntityAdministration Administration;
            public AasUaEntityQualifier Qualifier;
            public AasUaEntityKind Kind;
            public AasUaEntityReferable Referable;
            public AasUaEntityReference Reference;
            public AasUaEntitySemanticId SemanticId;
            public AasUaEntitySubmodel Submodel;
            public AasUaEntityProperty Property;
            public AasUaEntityCollection Collection;
            public AasUaEntityOrderedCollection OrderedCollection;
            public AasUaEntitySubmodelWrapper SubmodelWrapper;
            public AasUaEntityFile File;
            public AasUaEntityFileType FileType;
            public AasUaEntityBlob Blob;
            public AasUaEntityReferenceElement ReferenceElement;
            public AasUaEntityRelationshipElement RelationshipElement;
            public AasUaEntityOperationVariable OperationVariable;
            public AasUaEntityOperation Operation;
            public AasUaEntityAsset Asset;
            public AasUaEntityAAS AAS;

            public AasTypeEntities(AasEntityBuilder builder)
            {
                Identification = new AasUaEntityIdentification(builder, 1000);
                Administration = new AasUaEntityAdministration(builder, 1002);
                Qualifier = new AasUaEntityQualifier(builder, 1008);
                Kind = new AasUaEntityKind(builder, 1003);
                Referable = new AasUaEntityReferable(builder, 1004);
                Reference = new AasUaEntityReference(builder, 1006);
                SemanticId = new AasUaEntitySemanticId(builder, 1007);
                Submodel = new AasUaEntitySubmodel(builder, 1009);
                Property = new AasUaEntityProperty(builder, 1010);
                Collection = new AasUaEntityCollection(builder, 1011);
                OrderedCollection = new AasUaEntityOrderedCollection(builder, 1012);
                SubmodelWrapper = new AasUaEntitySubmodelWrapper(builder, 1011);
                File = new AasUaEntityFile(builder, 1012);
                FileType = new AasUaEntityFileType(builder, 1013);
                Blob = new AasUaEntityBlob(builder, 1014);
                ReferenceElement = new AasUaEntityReferenceElement(builder, 1015);
                RelationshipElement = new AasUaEntityRelationshipElement(builder, 1016);
                OperationVariable = new AasUaEntityOperationVariable(builder, 1017);
                Operation = new AasUaEntityOperation(builder, 1018);
                Asset = new AasUaEntityAsset(builder, 1001);
                AAS = new AasUaEntityAAS(builder, 1005);
            }
        }

        private AasTypeEntities aasTypes = null;
        public AasTypeEntities AasTypes { get { return aasTypes; } }

    }
}
