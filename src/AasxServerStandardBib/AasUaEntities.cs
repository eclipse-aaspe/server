/*
Copyright (c) 2018-2019 Festo AG & Co. KG <https://www.festo.com/net/de_de/Forms/web/contact_international>
Author: Michael Hoffmeister

This source code is licensed under the Apache License 2.0 (see LICENSE.txt).

This source code may use other Open Source software components (see LICENSE.txt).
*/


using Extensions;
using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Globalization;
using AAS = AasCore.Aas3_0;

// TODO (MIHO, 2020-08-29): The UA mapping needs to be overworked in order to comply the joint aligment with I4AAS
// TODO (MIHO, 2020-08-29): The UA mapping needs to be checked for the "new" HasDataSpecification strcuture of V2.0.1

namespace AasOpcUaServer
{
    public class AasUaBaseEntity
    {
        public enum CreateMode { Type, Instance };

        /// <summary>
        /// Reference back to the entity builder
        /// </summary>
        protected AasEntityBuilder entityBuilder = null;

        public AasUaBaseEntity(AasEntityBuilder entityBuilder)
        {
            this.entityBuilder = entityBuilder;
        }

        /// <summary>
        /// Typically the node of the entity in the AAS type object space
        /// </summary>
        protected NodeState typeObject = null;

        /// <summary>
        /// If the entitiy does not have a direct type object, the object id instead (for pre-defined objects)
        /// </summary>
        protected NodeId typeObjectId = null;

        /// <summary>
        /// Getter of the type object
        /// </summary>
        public NodeState GetTypeObject()
        {
            return typeObject;
        }

        /// <summary>
        /// Getter of the type object id, either directly or via the type object (if avilable)
        /// </summary>
        /// <returns></returns>
        public NodeId GetTypeNodeId()
        {
            if (typeObjectId != null)
                return typeObjectId;
            if (typeObject == null)
                return null;
            return typeObject.NodeId;
        }
    }

    public class AasUaEntityPathType : AasUaBaseEntity
    {
        public AasUaEntityPathType(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type elements
            this.typeObject = this.entityBuilder.CreateAddDataType("AASPathType", DataTypeIds.String);
        }
    }

    public class AasUaEntityMimeType : AasUaBaseEntity
    {
        public AasUaEntityMimeType(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type elements
            this.typeObject = this.entityBuilder.CreateAddDataType("AASMimeType", DataTypeIds.String);
        }
    }

    public class AasUaEntityIdentification : AasUaBaseEntity
    {
        public AasUaEntityIdentification(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASIdentifierType",
                ObjectTypeIds.BaseObjectType, preferredTypeNumId, descriptionKey: "AAS:Identifier");
            // add some elements
            this.entityBuilder.CreateAddPropertyState<string>(this.typeObject, CreateMode.Type, "IdType",
                DataTypeIds.String, null, defaultSettings: true,
                modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
            this.entityBuilder.CreateAddPropertyState<string>(this.typeObject, CreateMode.Type, "Id",
                DataTypeIds.String, null, defaultSettings: true,
                modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
        }

        public NodeState CreateAddElements(NodeState parent, CreateMode mode,
            string identification = null,
            AasUaNodeHelper.ModellingRule modellingRule = AasUaNodeHelper.ModellingRule.None)
        {
            if (parent == null)
                return null;

            var o = this.entityBuilder.CreateAddObject(parent, mode, "Identification",
                ReferenceTypeIds.HasComponent, GetTypeObject().NodeId, modellingRule: modellingRule);

            if (mode == CreateMode.Instance)
            {
                if (identification != null)
                {
                    //this.entityBuilder.CreateAddPropertyState<string>(o, mode, "IdType",
                    //    DataTypeIds.String, "" + "" + identification.IdType, defaultSettings: true);
                    this.entityBuilder.CreateAddPropertyState<string>(o, mode, "Id",
                        DataTypeIds.String, "" + "" + identification, defaultSettings: true);
                }
            }

            return o;
        }
    }

    public class AasUaEntityAdministration : AasUaBaseEntity
    {
        public AasUaEntityAdministration(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASAdministrativeInformationType",
                ObjectTypeIds.BaseObjectType, preferredTypeNumId, descriptionKey: "AAS:AdministrativeInformation");
            // add some elements
            this.entityBuilder.CreateAddPropertyState<string>(this.typeObject, CreateMode.Type, "Version",
                DataTypeIds.String, null, defaultSettings: true,
                modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
            this.entityBuilder.CreateAddPropertyState<string>(this.typeObject, CreateMode.Type, "Revision",
                DataTypeIds.String, null, defaultSettings: true,
                modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
        }

        public NodeState CreateAddElements(NodeState parent, CreateMode mode,
            IAdministrativeInformation administration = null,
            AasUaNodeHelper.ModellingRule modellingRule = AasUaNodeHelper.ModellingRule.None)
        {
            if (parent == null)
                return null;
            // Create whole object only if required
            if (mode == CreateMode.Instance && administration == null)
                return null;

            var o = this.entityBuilder.CreateAddObject(parent, mode, "Administration",
                ReferenceTypeIds.HasComponent, GetTypeObject().NodeId, modellingRule: modellingRule);

            if (mode == CreateMode.Instance)
            {
                if (administration == null)
                    return null;
                this.entityBuilder.CreateAddPropertyState<string>(o, mode, "Version",
                    DataTypeIds.String, "" + "" + administration.Version, defaultSettings: true);
                this.entityBuilder.CreateAddPropertyState<string>(o, mode, "Revision",
                    DataTypeIds.String, "" + "" + administration.Revision, defaultSettings: true);
            }

            return o;
        }
    }

    public class AasUaEntityQualifier : AasUaBaseEntity
    {
        public AasUaEntityQualifier(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASQualifierType",
                ObjectTypeIds.BaseObjectType, preferredTypeNumId, "AAS:Qualifier");

            // add some elements
            this.entityBuilder.AasTypes.SemanticId.CreateAddInstanceObject(this.typeObject,
                CreateMode.Type, null, "SemanticId", modellingRule: AasUaNodeHelper.ModellingRule.Optional);
            this.entityBuilder.CreateAddPropertyState<string>(this.typeObject, CreateMode.Type, "Type",
                DataTypeIds.String, null, defaultSettings: true,
                modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
            this.entityBuilder.CreateAddPropertyState<string>(this.typeObject, CreateMode.Type, "Value",
                DataTypeIds.String, null, defaultSettings: true,
                modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
            this.entityBuilder.AasTypes.Reference.CreateAddElements(this.typeObject, CreateMode.Type,
                null, "ValueId", AasUaNodeHelper.ModellingRule.Optional);
        }

        public NodeState CreateAddElements(NodeState parent, CreateMode mode, IQualifier qualifier = null,
            AasUaNodeHelper.ModellingRule modellingRule = AasUaNodeHelper.ModellingRule.None)
        {
            if (parent == null)
                return null;
            // Create whole object only if required
            if (mode == CreateMode.Instance && qualifier == null)
                return null;

            if (mode == CreateMode.Type)
            {
                // plain
                var o = this.entityBuilder.CreateAddObject(parent, mode, "Qualifier", ReferenceTypeIds.HasComponent,
                    GetTypeObject().NodeId, modellingRule: modellingRule);
                return o;
            }
            else
            {
                // need data
                if (qualifier == null)
                    return null;

                // do a little extra?
                string extraName = null;
                if (qualifier.Type != null && qualifier.Type.Length > 0)
                {
                    extraName = "Qualifier:" + qualifier.Type;
                    if (qualifier.Value != null && qualifier.Value.Length > 0)
                        extraName += "=" + qualifier.Value;
                }

                var o = this.entityBuilder.CreateAddObject(parent, mode, "Qualifier",
                    ReferenceTypeIds.HasComponent, GetTypeObject().NodeId, modellingRule: modellingRule,
                    extraName: extraName);

                this.entityBuilder.AasTypes.SemanticId.CreateAddInstanceObject(o,
                    CreateMode.Instance, qualifier.SemanticId, "SemanticId");
                this.entityBuilder.CreateAddPropertyState<string>(o, mode, "Type",
                    DataTypeIds.String, "" + qualifier.Type, defaultSettings: true);
                this.entityBuilder.CreateAddPropertyState<string>(o, mode, "Value",
                    DataTypeIds.String, "" + qualifier.Value, defaultSettings: true);
                this.entityBuilder.AasTypes.Reference.CreateAddElements(o,
                    CreateMode.Instance, qualifier.ValueId, "ValueId");

                return o;
            }

        }
    }

    public class AasUaEntityAssetKind : AasUaBaseEntity
    {
        public AasUaEntityAssetKind(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            // no special type here (is a string)
        }

        //public NodeState CreateAddElements(NodeState parent, CreateMode mode, AssetKind kind = null,
        // TODO (jtikekar, 2023-09-04): What should be the default of AssetKind
        public NodeState CreateAddElements(NodeState parent, CreateMode mode, AssetKind kind = AssetKind.Type,
            AasUaNodeHelper.ModellingRule modellingRule = AasUaNodeHelper.ModellingRule.None)
        {
            if (mode == CreateMode.Instance)
                return null;

            var o = this.entityBuilder.CreateAddPropertyState<string>(parent, mode, "Kind",
                DataTypeIds.String, (mode == CreateMode.Type) ? null : "" + kind, defaultSettings: true,
                modellingRule: modellingRule);

            return o;
        }
    }

    public class AasUaEntityModelingKind : AasUaBaseEntity
    {
        public AasUaEntityModelingKind(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            // no special type here (is a string)
        }

        //public NodeState CreateAddElements(NodeState parent, CreateMode mode, ModelingKind kind = null,
        // TODO (jtikekar, 2023-09-04): default value of ModellingKind
        public NodeState CreateAddElements(NodeState parent, CreateMode mode, ModellingKind kind = ModellingKind.Template,
            AasUaNodeHelper.ModellingRule modellingRule = AasUaNodeHelper.ModellingRule.None)
        {
            if (mode == CreateMode.Instance)
                return null;

            var o = this.entityBuilder.CreateAddPropertyState<string>(parent, mode, "Kind",
                DataTypeIds.String, (mode == CreateMode.Type) ? null : "" + kind, defaultSettings: true,
                modellingRule: modellingRule);

            return o;
        }
    }

    public class AasUaEntityReferable : AasUaBaseEntity
    {
        public AasUaEntityReferable(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // NO type object required
            // see IAASReferable interface
        }

        /// <summary>
        /// This adds all Referable attributes to the parent and re-defines the descriptons 
        /// </summary>
        public NodeState CreateAddElements(NodeState parent, CreateMode mode, IReferable refdata = null)
        {
            if (parent == null)
                return null;
            if (mode == CreateMode.Instance && refdata == null)
                return null;

            if (mode == CreateMode.Type || refdata?.Category != null)
                this.entityBuilder.CreateAddPropertyState<string>(parent, mode, "Category",
                    DataTypeIds.String, (mode == CreateMode.Type) ? null : "" + refdata?.Category,
                    defaultSettings: true, modellingRule: AasUaNodeHelper.ModellingRule.Optional);

            // No idShort as typically in the DisplayName of the node

            if (mode == CreateMode.Instance)
            {
                // now, re-set the description on the parent
                // ISSUE: only ONE language supported!
                parent.Description = AasUaUtils.GetBestUaDescriptionFromAasDescription(refdata?.Description);
            }

            return null;
        }
    }

    public class AasUaEntityReferenceBase : AasUaBaseEntity
    {
        public AasUaEntityReferenceBase(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // nothing, only used to share code
        }

        /// <summary>
        /// Sets the "Keys" value information of an AAS Reference. This is especially important for referencing 
        /// outwards of the AAS (environment).
        /// </summary>
        public void CreateAddKeyElements(NodeState parent, CreateMode mode, List<IKey> keys = null)
        {
            if (parent == null)
                return;

            // MIHO: open62541 does not to process Values as string[], therefore change it temporarily

            if (this.entityBuilder != null && this.entityBuilder.theServerOptions != null
                && this.entityBuilder.theServerOptions.ReferenceKeysAsSingleString)
            {
                // fix for open62541
                var keyo = this.entityBuilder.CreateAddPropertyState<string>(parent, mode, "Values",
                    DataTypeIds.String, null, defaultSettings: true);

                if (mode == CreateMode.Instance && keyo != null)
                {
                    Reference newRef = new Reference(AasCore.Aas3_0.ReferenceTypes.ExternalReference, keys);
                    keyo.Value = AasUaUtils.ToOpcUaReference(newRef);
                }
            }
            else
            {
                // default behaviour
                var keyo = this.entityBuilder?.CreateAddPropertyState<string[]>(parent, mode, "Values",
                    DataTypeIds.Structure, null, defaultSettings: true);

                if (mode == CreateMode.Instance && keyo != null)
                {
                    Reference newRef = new Reference(AasCore.Aas3_0.ReferenceTypes.ModelReference, keys);
                    keyo.Value = AasUaUtils.ToOpcUaReferenceList(newRef)?.ToArray();
                }
            }
        }

        /// <summary>
        /// Sets the "Keys" value information of an AAS Reference. This is especially important for referencing 
        /// outwards of the AAS (environment).
        /// </summary>
        public void CreateAddKeyElements(NodeState parent, CreateMode mode, List<IIdentifiable> ids = null)
        {
            List<IKey> keys = new List<IKey>();
            if (parent == null)
                return;

            // MIHO: open62541 does not to process Values as string[], therefore change it temporarily

            if (ids != null)
            {
                foreach (var id in ids)
                {
                    var key = new Key(KeyTypes.GlobalReference, id.Id);
                    keys.Add(key);
                }
            }

            if (this.entityBuilder != null && this.entityBuilder.theServerOptions != null
                && this.entityBuilder.theServerOptions.ReferenceKeysAsSingleString)
            {
                // fix for open62541
                var keyo = this.entityBuilder.CreateAddPropertyState<string>(parent, mode, "Values",
                    DataTypeIds.String, null, defaultSettings: true);

                if (mode == CreateMode.Instance && keyo != null)
                {
                    Reference newRef = new Reference(AasCore.Aas3_0.ReferenceTypes.ExternalReference, keys);
                    keyo.Value = AasUaUtils.ToOpcUaReference(newRef);
                }
            }
            else
            {
                // default behaviour
                var keyo = this.entityBuilder?.CreateAddPropertyState<string[]>(parent, mode, "Values",
                    DataTypeIds.Structure, null, defaultSettings: true);

                if (mode == CreateMode.Instance && keyo != null)
                {
                    Reference newRef = new Reference(AasCore.Aas3_0.ReferenceTypes.ExternalReference, keys);
                    keyo.Value = AasUaUtils.ToOpcUaReferenceList(newRef)?.ToArray();
                }
            }
        }

        /// <summary>
        /// Sets the UA relation of an AAS Reference. This is especially important for reference within an AAS node 
        /// structure, to be
        /// in the style of OPC UA
        /// </summary>
        public void CreateAddReferenceElements(NodeState parent, CreateMode mode, List<Key> keys = null)
        {
            if (parent == null)
                return;

            if (mode == CreateMode.Type)
            {
                // makes no sense
            }
            else
            {
                // would make sense, but is replaced by the code in "CreateAddInstanceObjects" directly.
            }
        }
    }

    public class AasUaEntityReference : AasUaEntityReferenceBase
    {
        public AasUaEntityReference(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASReferenceType",
                ObjectTypeIds.BaseObjectType, preferredTypeNumId);
            // with some elements
            this.CreateAddKeyElements(this.typeObject, CreateMode.Type, keys: null);
            this.CreateAddReferenceElements(this.typeObject, CreateMode.Type);
        }

        public NodeState CreateAddElements(NodeState parent, CreateMode mode,
            AAS.IReference reference, string browseDisplayName = null,
            AasUaNodeHelper.ModellingRule modellingRule = AasUaNodeHelper.ModellingRule.None)
        {
            if (parent == null)
                return null;

            if (mode == CreateMode.Type)
            {
                var o = this.entityBuilder.CreateAddObject(parent, mode, browseDisplayName ?? "Reference",
                    ReferenceTypeIds.HasComponent, GetTypeObject().NodeId, modellingRule: modellingRule);
                return o;
            }
            else
            {
                if (reference == null)
                    return null;

                var o = this.entityBuilder.CreateAddObject(parent, mode, browseDisplayName ?? "Reference",
                    ReferenceTypeIds.HasComponent, GetTypeObject().NodeId);

                // explicit strings?
                if (reference is Reference modrf)
                {
                    this.CreateAddKeyElements(o, mode, modrf.Keys);

                    // find a matching concept description or other referable?
                    // as we do not have all other nodes realized, store a late action
                    this.entityBuilder.AddNodeLateAction(
                        new AasEntityBuilder.NodeLateActionLinkToReference(
                            o,
                            modrf,
                            AasEntityBuilder.NodeLateActionLinkToReference.ActionType.SetAasReference
                        ));
                }

                if (reference is Reference glbrf)
                {
                    this.CreateAddKeyElements(o, mode, glbrf.Keys);

                    // find a matching concept description or other referable?
                    // as we do not have all other nodes realized, store a late action
                    this.entityBuilder.AddNodeLateAction(
                        new AasEntityBuilder.NodeLateActionLinkToReference(
                            o,
                            new Reference(AasCore.Aas3_0.ReferenceTypes.ExternalReference, glbrf.Keys),
                            AasEntityBuilder.NodeLateActionLinkToReference.ActionType.SetAasReference
                        ));
                }

                // OK
                return o;
            }
        }
    }

    public class AasUaEntitySemanticId : AasUaEntityReferenceBase
    {
        public AasUaEntitySemanticId(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // re-use AASReferenceType for this
            this.typeObject = this.entityBuilder.AasTypes.Reference.GetTypeObject();
            // with some elements
            this.CreateAddReferenceElements(this.typeObject, CreateMode.Type);
        }

        public NodeState CreateAddInstanceObject(NodeState parent, CreateMode mode,
            AAS.IReference semid = null, string browseDisplayName = null,
            AasUaNodeHelper.ModellingRule modellingRule = AasUaNodeHelper.ModellingRule.None)
        {
            if (parent == null)
                return null;

            if (mode == CreateMode.Type)
            {
                var o = this.entityBuilder.CreateAddObject(parent, mode, browseDisplayName ?? "SemanticId",
                    ReferenceTypeIds.HasComponent, GetTypeObject().NodeId, modellingRule: modellingRule);
                return o;
            }
            else
            {
                if (semid == null)
                    return null;

                var o = this.entityBuilder.CreateAddObject(parent, mode, browseDisplayName ?? "SemanticId",
                    ReferenceTypeIds.HasComponent, GetTypeObject().NodeId);

                // explicit strings?
                this.CreateAddKeyElements(o, mode, semid.Keys);

                // find a matching concept description or other referable?
                // as we do not have all other nodes realized, store a late action
                this.entityBuilder.AddNodeLateAction(
                    new AasEntityBuilder.NodeLateActionLinkToReference(
                        parent,
                        //Reference.CreateNew(Key.ConceptDescription, semid?.Keys),
                        new Reference(AasCore.Aas3_0.ReferenceTypes.ModelReference, semid?.Keys),
                        AasEntityBuilder.NodeLateActionLinkToReference.ActionType.SetDictionaryEntry
                    ));

                // OK
                return o;
            }
        }
    }

    public class AasUaEntityAsset : AasUaBaseEntity
    {
        public AasUaEntityAsset(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASAssetType",
                ObjectTypeIds.BaseObjectType, preferredTypeNumId, descriptionKey: "AAS:Asset");
            this.entityBuilder.AasTypes.HasInterface.CreateAddInstanceReference(this.typeObject, false,
                this.entityBuilder.AasTypes.IAASIdentifiableType.GetTypeNodeId());

            // add necessary type information
            // Referable
            this.entityBuilder.AasTypes.Referable.CreateAddElements(this.typeObject, CreateMode.Type);
            // Identifiable
            this.entityBuilder.AasTypes.Identification.CreateAddElements(this.typeObject, CreateMode.Type,
                modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
            this.entityBuilder.AasTypes.Administration.CreateAddElements(this.typeObject, CreateMode.Type,
                modellingRule: AasUaNodeHelper.ModellingRule.Optional);
            // HasKind
            this.entityBuilder.AasTypes.AssetKind.CreateAddElements(this.typeObject, CreateMode.Type, modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
            this.entityBuilder.AasTypes.AssetKind.CreateAddElements(this.typeObject, CreateMode.Type,
                modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
            // HasDataSpecification
            this.entityBuilder.AasTypes.Reference.CreateAddElements(this.typeObject, CreateMode.Type,
                null, "DataSpecification", modellingRule: AasUaNodeHelper.ModellingRule.OptionalPlaceholder);
            // own attributes
            this.entityBuilder.AasTypes.Reference.CreateAddElements(this.typeObject, CreateMode.Type,
                null, "AssetIdentificationModel", modellingRule: AasUaNodeHelper.ModellingRule.Optional);
        }

        public NodeState CreateAddElements(NodeState parent, CreateMode mode, IAssetInformation asset = null,
            AasUaNodeHelper.ModellingRule modellingRule = AasUaNodeHelper.ModellingRule.None)
        {
            if (parent == null)
                return null;
            // Create whole object only if required
            if (mode == CreateMode.Instance && asset == null)
                return null;

            // containing element
            var o = this.entityBuilder.CreateAddObject(parent, mode, "Asset", ReferenceTypeIds.HasComponent,
                GetTypeObject().NodeId, modellingRule: modellingRule);

            if (mode == CreateMode.Instance)
            {
                // access
                if (asset == null)
                    return null;

                // register node record

                this.entityBuilder.AddNodeRecord(new AasEntityBuilder.NodeRecord(o, asset?.GlobalAssetId));

                // Referable
                //this.entityBuilder.AasTypes.Referable.CreateAddElements(o, CreateMode.Instance, asset);
                // Identifiable
                this.entityBuilder.AasTypes.Identification.CreateAddElements(
                    o, CreateMode.Instance, asset?.GlobalAssetId);
                //this.entityBuilder.AasTypes.Administration.CreateAddElements(
                //    o, CreateMode.Instance, asset.administration);
                // HasKind
                this.entityBuilder.AasTypes.AssetKind.CreateAddElements(o, CreateMode.Instance, asset.AssetKind);
                // HasDataSpecification
                //if (asset.hasDataSpecification != null && asset.hasDataSpecification != null)
                //    foreach (var ds in asset.hasDataSpecification)
                //        this.entityBuilder.AasTypes.Reference.CreateAddElements(
                //            o, CreateMode.Instance, ds?.dataSpecification, "DataSpecification");
                //// own attributes
                //this.entityBuilder.AasTypes.Reference.CreateAddElements(
                //    o, CreateMode.Instance, asset.assetIdentificationModelRef, "AssetIdentificationModel");
            }

            return o;
        }
    }

    public class AasUaEntityAAS : AasUaBaseEntity
    {
        public AasUaEntityAAS(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASAssetAdministrationShellType",
                ObjectTypeIds.BaseObjectType, preferredTypeNumId, descriptionKey: "AAS:AssetAdministrationShell");

            // interface
            this.entityBuilder.AasTypes.HasInterface.CreateAddInstanceReference(this.typeObject, false,
                this.entityBuilder.AasTypes.IAASIdentifiableType.GetTypeNodeId());

            // add necessary type information
            // Referable
            this.entityBuilder.AasTypes.Referable.CreateAddElements(this.typeObject, CreateMode.Type);
            // Identifiable
            this.entityBuilder.AasTypes.Identification.CreateAddElements(this.typeObject, CreateMode.Type,
                modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
            this.entityBuilder.AasTypes.Administration.CreateAddElements(this.typeObject, CreateMode.Type,
                modellingRule: AasUaNodeHelper.ModellingRule.Optional);
            // HasDataSpecification
            this.entityBuilder.AasTypes.Reference.CreateAddElements(this.typeObject, CreateMode.Type, null,
                "DataSpecification", modellingRule: AasUaNodeHelper.ModellingRule.OptionalPlaceholder);

            // own attributes
            this.entityBuilder.AasTypes.Reference.CreateAddElements(this.typeObject, CreateMode.Type, null,
                "DerivedFrom", modellingRule: AasUaNodeHelper.ModellingRule.Optional);
            // assets
            this.entityBuilder.AasTypes.Asset.CreateAddElements(this.typeObject, CreateMode.Type,
                modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
            // associated views
            //this.entityBuilder.AasTypes.View.CreateAddElements(this.typeObject, CreateMode.Type,
            //    modellingRule: AasUaNodeHelper.ModellingRule.OptionalPlaceholder);
            // associated submodels
            this.entityBuilder.AasTypes.Submodel.CreateAddElements(this.typeObject, CreateMode.Type,
                modellingRule: AasUaNodeHelper.ModellingRule.OptionalPlaceholder);
            // concept dictionary
            //this.entityBuilder.AasTypes.ConceptDictionary.CreateAddElements(this.typeObject, CreateMode.Type,
            //    modellingRule: AasUaNodeHelper.ModellingRule.OptionalPlaceholder);
        }

        public NodeState CreateAddInstanceObject(NodeState parent,
            AasCore.Aas3_0.Environment env, IAssetAdministrationShell aas)
        {
            // access
            if (env == null || aas == null)
                return null;

            // containing element
            string extraName = null;
            string browseName = "AssetAdministrationShell";
            if (aas.IdShort != null && aas.IdShort.Trim().Length > 0)
            {
                extraName = "AssetAdministrationShell:" + aas.IdShort;
                browseName = aas.IdShort;
            }
            var o = this.entityBuilder.CreateAddObject(parent, CreateMode.Instance,
                browseName, ReferenceTypeIds.HasComponent,
                GetTypeObject().NodeId, extraName: extraName);

            // register node record
            this.entityBuilder.AddNodeRecord(new AasEntityBuilder.NodeRecord(o, aas));

            // Referable
            this.entityBuilder.AasTypes.Referable.CreateAddElements(o, CreateMode.Instance, aas);
            // Identifiable
            this.entityBuilder.AasTypes.Identification.CreateAddElements(
                o, CreateMode.Instance, aas.Id);
            this.entityBuilder.AasTypes.Administration.CreateAddElements(
                o, CreateMode.Instance, aas.Administration);
            // HasDataSpecification
            if (aas.EmbeddedDataSpecifications != null && aas.EmbeddedDataSpecifications != null)
                foreach (var ds in aas.EmbeddedDataSpecifications)
                    this.entityBuilder.AasTypes.Reference.CreateAddElements(
                        o, CreateMode.Instance, ds.DataSpecification, "DataSpecification");
            // own attributes
            this.entityBuilder.AasTypes.Reference.CreateAddElements(
                o, CreateMode.Instance, aas.DerivedFrom, "DerivedFrom");

            // associated asset
            if (aas.AssetInformation != null)
            {
                this.entityBuilder.AasTypes.Asset.CreateAddElements(o, CreateMode.Instance, aas.AssetInformation);
            }

            //// associated views
            //if (aas.views != null)
            //    foreach (var vw in aas.views.views)
            //        this.entityBuilder.AasTypes.View.CreateAddElements(
            //            o, CreateMode.Instance, vw);

            // associated submodels
            if (aas.Submodels != null && aas.Submodels.Count > 0)
                foreach (var smr in aas.Submodels)
                {
                    var sm = env.FindSubmodel(smr);
                    if (sm != null)
                        this.entityBuilder.AasTypes.Submodel.CreateAddElements(
                            o, CreateMode.Instance, sm);
                }

            // make up CD dictionaries
            //if (aas.conceptDictionaries != null && aas.conceptDictionaries.Count > 0)
            //{
            //    // ReSharper disable once UnusedVariable
            //    foreach (var cdd in aas.conceptDictionaries)
            //    {
            //        // TODO (MIHO, 2020-08-06): check (again) if reference to CDs is done are shall be done
            //        // here. They are stored separately.
            //    }
            //}

            // results
            return o;
        }
    }

    public class AasUaEntitySubmodel : AasUaBaseEntity
    {
        public AasUaEntitySubmodel(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASSubmodelType",
                ObjectTypeIds.BaseObjectType, preferredTypeNumId, descriptionKey: "AAS:Submodel");
            this.entityBuilder.AasTypes.HasInterface.CreateAddInstanceReference(this.typeObject, false,
                this.entityBuilder.AasTypes.IAASIdentifiableType.GetTypeNodeId());

            // add some elements
            // Referable
            this.entityBuilder.AasTypes.Referable.CreateAddElements(this.typeObject, CreateMode.Type);
            // Identifiable
            this.entityBuilder.AasTypes.Identification.CreateAddElements(this.typeObject, CreateMode.Type,
                modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
            this.entityBuilder.AasTypes.Administration.CreateAddElements(this.typeObject, CreateMode.Type,
                modellingRule: AasUaNodeHelper.ModellingRule.Optional);
            // HasSemantics
            this.entityBuilder.AasTypes.SemanticId.CreateAddInstanceObject(this.typeObject, CreateMode.Type, null,
                "SemanticId", modellingRule: AasUaNodeHelper.ModellingRule.Optional);
            // HasKind
            this.entityBuilder.AasTypes.ModelingKind.CreateAddElements(this.typeObject, CreateMode.Type,
                modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
            // HasDataSpecification
            this.entityBuilder.AasTypes.Reference.CreateAddElements(this.typeObject, CreateMode.Type, null,
                "DataSpecification", modellingRule: AasUaNodeHelper.ModellingRule.OptionalPlaceholder);
            // Qualifiable
            this.entityBuilder.AasTypes.Qualifier.CreateAddElements(this.typeObject, CreateMode.Type,
                modellingRule: AasUaNodeHelper.ModellingRule.OptionalPlaceholder);
            // SubmodelElements
            this.entityBuilder.AasTypes.SubmodelWrapper.CreateAddElements(this.typeObject, CreateMode.Type,
                modellingRule: AasUaNodeHelper.ModellingRule.OptionalPlaceholder);
        }

        public NodeState CreateAddElements(NodeState parent, CreateMode mode, ISubmodel sm = null,
            AasUaNodeHelper.ModellingRule modellingRule = AasUaNodeHelper.ModellingRule.None)
        {
            if (parent == null)
                return null;

            if (mode == CreateMode.Type)
            {
                // create only containing element with generic name
                var o = this.entityBuilder.CreateAddObject(parent, mode, "Submodel", ReferenceTypeIds.HasComponent,
                    this.GetTypeNodeId(), modellingRule: modellingRule);
                return o;
            }
            else
            {
                // access
                if (sm == null)
                    return null;

                // containing element
                var o = this.entityBuilder.CreateAddObject(parent, mode,
                    "" + sm.IdShort, ReferenceTypeIds.HasComponent,
                    GetTypeObject().NodeId, extraName: "Submodel:" + sm.IdShort);

                // register node record
                this.entityBuilder.AddNodeRecord(new AasEntityBuilder.NodeRecord(o, sm));

                // Referable
                this.entityBuilder.AasTypes.Referable.CreateAddElements(
                    o, CreateMode.Instance, sm);
                // Identifiable
                this.entityBuilder.AasTypes.Identification.CreateAddElements(
                    o, CreateMode.Instance, sm.Id);
                this.entityBuilder.AasTypes.Administration.CreateAddElements(
                    o, CreateMode.Instance, sm.Administration);
                // HasSemantics
                this.entityBuilder.AasTypes.SemanticId.CreateAddInstanceObject(
                    o, CreateMode.Instance, sm.SemanticId, "SemanticId");
                // HasKind
                this.entityBuilder.AasTypes.ModelingKind.CreateAddElements(
                    o, CreateMode.Instance, (ModellingKind)sm.Kind);
                // HasDataSpecification
                if (sm.EmbeddedDataSpecifications != null && sm.EmbeddedDataSpecifications != null)
                    foreach (var ds in sm.EmbeddedDataSpecifications)
                        this.entityBuilder.AasTypes.Reference.CreateAddElements(
                            o, CreateMode.Instance, ds.DataSpecification, "DataSpecification");
                // Qualifiable
                if (sm.Qualifiers != null)
                    foreach (var q in sm.Qualifiers)
                        this.entityBuilder.AasTypes.Qualifier.CreateAddElements(
                            o, CreateMode.Instance, q);

                // SubmodelElements
                if (sm.SubmodelElements != null)
                    foreach (var smw in sm.SubmodelElements)
                        if (smw != null)
                            this.entityBuilder.AasTypes.SubmodelWrapper.CreateAddElements(
                                o, CreateMode.Instance, smw);

                // result
                return o;
            }
        }
    }

    /// <summary>
    /// This class is for the representation if SME in UA namespace
    /// </summary>
    public class AasUaEntitySubmodelElement : AasUaBaseEntity
    {
        public AasUaEntitySubmodelElement(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASSubmodelElementType",
                ObjectTypeIds.BaseObjectType, preferredTypeNumId, descriptionKey: "AAS:SubmodelElement");
            this.entityBuilder.AasTypes.HasInterface.CreateAddInstanceReference(this.typeObject, false,
                this.entityBuilder.AasTypes.IAASReferableType.GetTypeNodeId());

            // add some elements to the type
            // Note: in this special case, the instance elements are populated by AasUaEntitySubmodelElementBase, 
            // while the elements
            // for the type are populated here

            // Referable
            this.entityBuilder.AasTypes.Referable.CreateAddElements(this.typeObject, CreateMode.Type);
            // HasSemantics
            this.entityBuilder.AasTypes.SemanticId.CreateAddInstanceObject(this.typeObject, CreateMode.Type, null,
                "SemanticId", modellingRule: AasUaNodeHelper.ModellingRule.Optional);
            // HasKind
            this.entityBuilder.AasTypes.ModelingKind.CreateAddElements(this.typeObject, CreateMode.Type,
                modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
            // HasDataSpecification
            this.entityBuilder.AasTypes.Reference.CreateAddElements(this.typeObject, CreateMode.Type, null,
                "DataSpecification", modellingRule: AasUaNodeHelper.ModellingRule.OptionalPlaceholder);
            // Qualifiable
            this.entityBuilder.AasTypes.Qualifier.CreateAddElements(this.typeObject, CreateMode.Type,
                modellingRule: AasUaNodeHelper.ModellingRule.OptionalPlaceholder);
        }
    }

    /// <summary>
    /// This class is the base class of derived properties
    /// </summary>
    public class AasUaEntitySubmodelElementBase : AasUaBaseEntity
    {
        public AasUaEntitySubmodelElementBase(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // do NOT create type object, as this is done by sub-class
        }

        public NodeState PopulateInstanceObject(NodeState o, ISubmodelElement sme)
        {
            // access
            if (o == null || sme == null)
                return null;

            // take this as perfect opportunity to register node record
            this.entityBuilder.AddNodeRecord(new AasEntityBuilder.NodeRecord(o, sme));

            // Referable
            this.entityBuilder.AasTypes.Referable.CreateAddElements(
                o, CreateMode.Instance, sme);
            // HasSemantics
            this.entityBuilder.AasTypes.SemanticId.CreateAddInstanceObject(
                o, CreateMode.Instance, sme.SemanticId, "SemanticId");
            // HasDataSpecification
            if (sme.EmbeddedDataSpecifications != null && sme.EmbeddedDataSpecifications != null)
                foreach (var ds in sme.EmbeddedDataSpecifications)
                    this.entityBuilder.AasTypes.Reference.CreateAddElements(
                        o, CreateMode.Instance, ds.DataSpecification, "DataSpecification");
            // Qualifiable
            if (sme.Qualifiers != null)
                foreach (var q in sme.Qualifiers)
                    this.entityBuilder.AasTypes.Qualifier.CreateAddElements(
                        o, CreateMode.Instance, q);

            // result
            return o;
        }
    }

    /// <summary>
    /// This class will automatically instantiate the correct SubmodelElement entity.
    /// </summary>
    public class AasUaEntitySubmodelWrapper : AasUaBaseEntity
    {
        public AasUaEntitySubmodelWrapper(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // do NOT create type object, as this is done by sub-class
        }

        public NodeState CreateAddElements(NodeState parent, CreateMode mode,
            ISubmodelElement smw = null,
            AasUaNodeHelper.ModellingRule modellingRule = AasUaNodeHelper.ModellingRule.None)
        {
            // access
            if (parent == null)
                return null;

            if (mode == CreateMode.Type)
            {
                // create only containing element (base type) with generic name
                var o = this.entityBuilder.CreateAddObject(parent, mode,
                    "SubmodelElement", ReferenceTypeIds.HasComponent,
                    this.entityBuilder.AasTypes.SubmodelElement.GetTypeNodeId(), modellingRule: modellingRule);
                return o;
            }
            else
            {
                if (smw == null)
                    return null;

                if (smw is SubmodelElementCollection)
                {
                    var coll = smw as SubmodelElementCollection;
                    return this.entityBuilder.AasTypes.Collection.CreateAddInstanceObject(parent, coll);
                }
                else if (smw is Property)
                    return this.entityBuilder.AasTypes.Property.CreateAddInstanceObject(
                        parent, smw as Property);
                else if (smw is File)
                    return this.entityBuilder.AasTypes.File.CreateAddInstanceObject(
                        parent, smw as File);
                else if (smw is Blob)
                    return this.entityBuilder.AasTypes.Blob.CreateAddInstanceObject(
                        parent, smw as Blob);
                else if (smw is ReferenceElement)
                    return this.entityBuilder.AasTypes.ReferenceElement.CreateAddInstanceObject(
                        parent, smw as ReferenceElement);
                else if (smw is RelationshipElement)
                    return this.entityBuilder.AasTypes.RelationshipElement.CreateAddInstanceObject(
                        parent, smw as RelationshipElement);
                else if (smw is Operation)
                    return this.entityBuilder.AasTypes.Operation.CreateAddInstanceObject(
                        parent, smw as Operation);

                // nope
                return null;
            }
        }
    }

    public class AasUaEntityProperty : AasUaEntitySubmodelElementBase
    {
        public AasUaEntityProperty(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddObjectType(
                "AASPropertyType", entityBuilder.AasTypes.SubmodelElement.GetTypeNodeId(), preferredTypeNumId,
                descriptionKey: "AAS:Property");

            // elements not in the base type
            this.entityBuilder.AasTypes.Reference.CreateAddElements(this.typeObject, CreateMode.Instance, null,
                "ValueId", modellingRule: AasUaNodeHelper.ModellingRule.Optional);
            this.entityBuilder.CreateAddPropertyState<string>(this.typeObject, CreateMode.Type, "ValueType",
                DataTypeIds.String, null, defaultSettings: true,
                modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
            this.entityBuilder.CreateAddPropertyState<string>(this.typeObject, CreateMode.Type, "Value",
                DataTypeIds.BaseDataType, null, defaultSettings: true,
                modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
        }

        public NodeState CreateAddInstanceObject(NodeState parent, Property prop)
        {
            // access
            if (prop == null)
                return null;

            // for all
            var mode = CreateMode.Instance;

            // containing element
            var o = this.entityBuilder.CreateAddObject(parent, mode, "" + prop.IdShort, ReferenceTypeIds.HasComponent,
                GetTypeObject().NodeId);

            // populate common attributes
            base.PopulateInstanceObject(o, prop);

            // TODO (MIHO, 2020-08-06): not sure if to add these
            this.entityBuilder.AasTypes.Reference.CreateAddElements(o, CreateMode.Instance, prop.ValueId, "ValueId");
            this.entityBuilder.CreateAddPropertyState<string>(o, mode, "ValueType",
                DataTypeIds.String, "" + prop.ValueType, defaultSettings: true);

            // aim is to support many types natively
            var vt = prop.ValueType;
            if (prop.ValueType == DataTypeDefXsd.Boolean)
            {
                var x = (prop.Value ?? "").ToLower().Trim();
                this.entityBuilder.CreateAddPropertyState<bool>(o, mode, "Value",
                    DataTypeIds.Boolean, x == "true", defaultSettings: true);
            }
            else if (vt == DataTypeDefXsd.DateTime || vt == DataTypeDefXsd.Date || vt == DataTypeDefXsd.Time)
            {
                if (DateTime.TryParse(prop.Value, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal, out var dt))
                    this.entityBuilder.CreateAddPropertyState<Int64>(o, mode, "Value",
                        DataTypeIds.DateTime, dt.ToFileTimeUtc(), defaultSettings: true);
            }
            else if (vt == DataTypeDefXsd.Decimal || vt == DataTypeDefXsd.Integer || vt == DataTypeDefXsd.Long
                     || vt == DataTypeDefXsd.NonPositiveInteger || vt == DataTypeDefXsd.NegativeInteger)
            {
                if (Int64.TryParse(prop.Value, out var v))
                    this.entityBuilder.CreateAddPropertyState<Int64>(o, mode, "Value",
                        DataTypeIds.Int64, v, defaultSettings: true);
            }
            else if (vt == DataTypeDefXsd.Int)
            {
                if (Int32.TryParse(prop.Value, out var v))
                    this.entityBuilder.CreateAddPropertyState<Int32>(o, mode, "Value",
                        DataTypeIds.Int32, v, defaultSettings: true);
            }
            else if (vt == DataTypeDefXsd.Short)
            {
                if (Int16.TryParse(prop.Value, out var v))
                    this.entityBuilder.CreateAddPropertyState<Int16>(o, mode, "Value",
                        DataTypeIds.Int16, v, defaultSettings: true);
            }
            else if (vt == DataTypeDefXsd.Byte)
            {
                if (SByte.TryParse(prop.Value, out var v))
                    this.entityBuilder.CreateAddPropertyState<SByte>(o, mode, "Value",
                        DataTypeIds.Byte, v, defaultSettings: true);
            }
            else if (vt == DataTypeDefXsd.NonNegativeInteger || vt == DataTypeDefXsd.PositiveInteger || vt == DataTypeDefXsd.UnsignedLong)
            {
                if (UInt64.TryParse(prop.Value, out var v))
                    this.entityBuilder.CreateAddPropertyState<UInt64>(o, mode, "Value",
                        DataTypeIds.UInt64, v, defaultSettings: true);
            }
            else if (vt == DataTypeDefXsd.UnsignedInt)
            {
                if (UInt32.TryParse(prop.Value, out var v))
                    this.entityBuilder.CreateAddPropertyState<UInt32>(o, mode, "Value",
                        DataTypeIds.UInt32, v, defaultSettings: true);
            }
            else if (vt == DataTypeDefXsd.UnsignedShort)
            {
                if (UInt16.TryParse(prop.Value, out var v))
                    this.entityBuilder.CreateAddPropertyState<UInt16>(o, mode, "Value",
                        DataTypeIds.UInt16, v, defaultSettings: true);
            }
            else if (vt == DataTypeDefXsd.UnsignedByte)
            {
                if (Byte.TryParse(prop.Value, out var v))
                    this.entityBuilder.CreateAddPropertyState<Byte>(o, mode, "Value",
                        DataTypeIds.Byte, v, defaultSettings: true);
            }
            else if (vt == DataTypeDefXsd.Double)
            {
                if (double.TryParse(prop.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
                    this.entityBuilder.CreateAddPropertyState<double>(o, mode, "Value",
                        DataTypeIds.Double, v, defaultSettings: true);
            }
            else if (vt == DataTypeDefXsd.Float)
            {
                if (float.TryParse(prop.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var v))
                    this.entityBuilder.CreateAddPropertyState<float>(o, mode, "Value",
                        DataTypeIds.Float, v, defaultSettings: true);
            }
            else
            {
                // leave in string
                this.entityBuilder.CreateAddPropertyState<string>(o, mode, "Value",
                    DataTypeIds.String, prop.Value, defaultSettings: true);
            }

            // result
            return o;
        }
    }

    public class AasUaEntityCollection : AasUaEntitySubmodelElementBase
    {
        public NodeState typeObjectOrdered = null;

        public AasUaEntityCollection(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            // TODO (MIHO, 2020-08-06): use the collection element of UA?
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASSubmodelElementCollectionType",
                entityBuilder.AasTypes.SubmodelElement.GetTypeNodeId(), preferredTypeNumId,
                descriptionKey: "AAS:SubmodelElementCollection");
            this.typeObjectOrdered = this.entityBuilder.CreateAddObjectType("AASSubmodelElementOrderedCollectionType",
                this.GetTypeNodeId(), preferredTypeNumId + 1,
                descriptionKey: "AAS:SubmodelElementCollection");

            // some elements
            // ReSharper disable once RedundantExplicitArrayCreation
            foreach (var o in new NodeState[] { this.typeObject /* , this.typeObjectOrdered */ })
            {
                this.entityBuilder.CreateAddPropertyState<bool>(o, CreateMode.Type, "AllowDuplicates",
                    DataTypeIds.Boolean, false, defaultSettings: true,
                    modellingRule: AasUaNodeHelper.ModellingRule.Optional);
            }
        }

        public NodeState CreateAddInstanceObject(NodeState parent, SubmodelElementCollection coll)
        {
            // access
            if (coll == null)
                return null;

            // containing element
            var to = GetTypeObject().NodeId;
            var o = this.entityBuilder.CreateAddObject(parent, CreateMode.Instance,
                "" + coll.IdShort, ReferenceTypeIds.HasComponent, to);

            // populate common attributes
            base.PopulateInstanceObject(o, coll);

            // values
            if (coll.Value != null)
                foreach (var smw in coll.Value)
                    if (smw != null)
                        this.entityBuilder.AasTypes.SubmodelWrapper.CreateAddElements(
                            o, CreateMode.Instance, smw);

            // result
            return o;
        }
    }

    public class AasUaEntityFile : AasUaEntitySubmodelElementBase
    {
        public AasUaEntityFile(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASFileType",
                entityBuilder.AasTypes.SubmodelElement.GetTypeNodeId(),
                preferredTypeNumId, descriptionKey: "AAS:File");

            // some elements
            this.entityBuilder.CreateAddPropertyState<string>(this.typeObject, CreateMode.Type, "MimeType",
                this.entityBuilder.AasTypes.MimeType.GetTypeNodeId(), null, defaultSettings: true,
                modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
            this.entityBuilder.CreateAddPropertyState<string>(this.typeObject, CreateMode.Type, "Value",
                this.entityBuilder.AasTypes.PathType.GetTypeNodeId(), null, defaultSettings: true,
                modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
            this.entityBuilder.AasTypes.FileType.CreateAddElements(this.typeObject, CreateMode.Type);
        }

        public NodeState CreateAddInstanceObject(NodeState parent, File file)
        {
            // access
            if (file == null)
                return null;

            // containing element
            var o = this.entityBuilder.CreateAddObject(parent, CreateMode.Instance, "" + file.IdShort,
                ReferenceTypeIds.HasComponent, GetTypeObject().NodeId);

            // populate common attributes
            base.PopulateInstanceObject(o, file);

            // own attributes
            this.entityBuilder.CreateAddPropertyState<string>(o, CreateMode.Instance, "MimeType",
                this.entityBuilder.AasTypes.MimeType.GetTypeNodeId(), file.ContentType, defaultSettings: true);
            this.entityBuilder.CreateAddPropertyState<string>(o, CreateMode.Instance, "Value",
                this.entityBuilder.AasTypes.PathType.GetTypeNodeId(), file.Value, defaultSettings: true);

            // wonderful working
            // TODO (MIHO, 2021-01-01): re-enable with adoptions
            /*
            if (this.entityBuilder.AasTypes.FileType.CheckSuitablity(this.entityBuilder.package, file))
                this.entityBuilder.AasTypes.FileType.CreateAddElements(
                    o, CreateMode.Instance, this.entityBuilder.package, file);
            */
            // result
            return o;
        }
    }

    public class AasUaEntityBlob : AasUaEntitySubmodelElementBase
    {
        public AasUaEntityBlob(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASBlobType",
                entityBuilder.AasTypes.SubmodelElement.GetTypeNodeId(), preferredTypeNumId,
                descriptionKey: "AAS:Blob");

            // some elements
            this.entityBuilder.CreateAddPropertyState<string>(this.typeObject, CreateMode.Type, "ContentType",
                DataTypeIds.String, null, defaultSettings: true,
                modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
            this.entityBuilder.CreateAddPropertyState<string>(this.typeObject, CreateMode.Type, "Value",
                DataTypeIds.String, null, defaultSettings: true,
                modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
        }

        public NodeState CreateAddInstanceObject(NodeState parent, Blob blob)
        {
            // access
            if (blob == null)
                return null;

            // containing element
            var o = this.entityBuilder.CreateAddObject(parent, CreateMode.Instance, "" + blob.IdShort,
                ReferenceTypeIds.HasComponent, GetTypeObject().NodeId);

            // populate common attributes
            base.PopulateInstanceObject(o, blob);

            // own attributes
            this.entityBuilder.CreateAddPropertyState<string>(o, CreateMode.Instance, "ContentType",
                DataTypeIds.String, blob.ContentType, defaultSettings: true);
            this.entityBuilder.CreateAddPropertyState<string>(o, CreateMode.Instance, "Value",
                DataTypeIds.String, blob.Value.ToString(), defaultSettings: true);

            // result
            return o;
        }
    }

    public class AasUaEntityReferenceElement : AasUaEntitySubmodelElementBase
    {
        public AasUaEntityReferenceElement(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASReferenceElementType",
                entityBuilder.AasTypes.SubmodelElement.GetTypeNodeId(), preferredTypeNumId,
                descriptionKey: "AAS:ReferenceElement");

            // some elements
            this.entityBuilder.AasTypes.Reference.CreateAddElements(this.typeObject, CreateMode.Type, null, "Value",
                modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
        }

        public NodeState CreateAddInstanceObject(NodeState parent, ReferenceElement refElem)
        {
            // access
            if (refElem == null)
                return null;

            // containing element
            var o = this.entityBuilder.CreateAddObject(parent, CreateMode.Instance, "" + refElem.IdShort,
                ReferenceTypeIds.HasComponent, GetTypeObject().NodeId);

            // populate common attributes
            base.PopulateInstanceObject(o, refElem);

            // own attributes
            if (refElem is ReferenceElement referenceElement)
            {
                this.entityBuilder.AasTypes.Reference.CreateAddElements(o, CreateMode.Instance, referenceElement.Value, "Value");
            }

            // result
            return o;
        }
    }

    public class AasUaEntityRelationshipElement : AasUaEntitySubmodelElementBase
    {
        public AasUaEntityRelationshipElement(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASRelationshipElementType",
                entityBuilder.AasTypes.SubmodelElement.GetTypeNodeId(), preferredTypeNumId,
                descriptionKey: "AAS:RelationshipElement");

            // some elements
            this.entityBuilder.AasTypes.Reference.CreateAddElements(this.typeObject, CreateMode.Type, null,
                "First", modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
            this.entityBuilder.AasTypes.Reference.CreateAddElements(this.typeObject, CreateMode.Type, null,
                "Second", modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
        }

        public NodeState CreateAddInstanceObject(NodeState parent, RelationshipElement relElem)
        {
            // access
            if (relElem == null)
                return null;

            // containing element
            var o = this.entityBuilder.CreateAddObject(parent, CreateMode.Instance, "" + relElem.IdShort,
                ReferenceTypeIds.HasComponent, GetTypeObject().NodeId);

            // populate common attributes
            base.PopulateInstanceObject(o, relElem);

            // own attributes
            this.entityBuilder.AasTypes.Reference.CreateAddElements(o, CreateMode.Instance, relElem.First, "First");
            this.entityBuilder.AasTypes.Reference.CreateAddElements(o, CreateMode.Instance, relElem.Second, "Second");

            // result
            return o;
        }
    }

    public class AasUaEntityOperationVariable : AasUaEntitySubmodelElementBase
    {
        public AasUaEntityOperationVariable(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddObjectType("OperationVariableType",
                entityBuilder.AasTypes.SubmodelElement.GetTypeNodeId(), preferredTypeNumId,
                descriptionKey: "AAS:OperationVariable");
        }

        public NodeState CreateAddInstanceObject(NodeState parent, OperationVariable opvar)
        {
            // access
            if (opvar == null || opvar.Value == null || opvar.Value == null)
                return null;

            // containing element
            var o = this.entityBuilder.CreateAddObject(parent, CreateMode.Instance,
                "" + opvar.Value.IdShort,
                ReferenceTypeIds.HasComponent, GetTypeObject().NodeId);

            // populate common attributes
            base.PopulateInstanceObject(o, opvar.Value);

            // own attributes
            this.entityBuilder.AasTypes.SubmodelWrapper.CreateAddElements(o, CreateMode.Instance, opvar.Value);

            // result
            return o;
        }
    }

    public class AasUaEntityOperation : AasUaEntitySubmodelElementBase
    {
        public AasUaEntityOperation(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASOperationType",
                entityBuilder.AasTypes.SubmodelElement.GetTypeNodeId(), preferredTypeNumId,
                descriptionKey: "AAS:Operation");

            // indicate the Operation
            this.entityBuilder.CreateAddMethodState(this.typeObject, CreateMode.Type, "Operation",
                    inputArgs: null,
                    outputArgs: null,
                    referenceTypeFromParentId: ReferenceTypeIds.HasComponent);

            // some elements
            for (int i = 0; i < 2; i++)
            {
                var o2 = this.entityBuilder.CreateAddObject(this.typeObject, CreateMode.Type, (i == 0) ? "in" : "out",
                    ReferenceTypeIds.HasComponent,
                    this.entityBuilder.AasTypes.OperationVariable.GetTypeObject().NodeId);
                this.entityBuilder.AasTypes.OperationVariable.CreateAddInstanceObject(o2, null);
            }
        }

        public NodeState CreateAddInstanceObject(NodeState parent, Operation op)
        {
            // access
            if (op == null)
                return null;

            // containing element
            var o = this.entityBuilder.CreateAddObject(parent, CreateMode.Instance,
                "" + op.IdShort,
                ReferenceTypeIds.HasComponent, GetTypeObject().NodeId);

            // populate common attributes
            base.PopulateInstanceObject(o, op);

            // own AAS attributes (in/out op vars)
            //for (int i = 0; i < 2; i++)
            //{
            //    var opvarList = op[i];
            //    if (opvarList != null && opvarList.Count > 0)
            //    {
            //        var o2 = this.entityBuilder.CreateAddObject(o,
            //            CreateMode.Instance,
            //            (i == 0) ? "OperationInputVariables" : "OperationOutputVariables",
            //            ReferenceTypeIds.HasComponent, GetTypeObject().NodeId);
            //        foreach (var opvar in opvarList)
            //            if (opvar != null && opvar.Value != null)
            //                this.entityBuilder.AasTypes.SubmodelWrapper.CreateAddElements(
            //                    o2, CreateMode.Instance, opvar.Value);
            //    }
            //}

            var opInputVarList = op.InputVariables;
            if (opInputVarList != null && opInputVarList.Count > 0)
            {
                var o2 = this.entityBuilder.CreateAddObject(o,
                    CreateMode.Instance,
                    "OperationInputVariables",
                    ReferenceTypeIds.HasComponent, GetTypeObject().NodeId);
                foreach (var opvar in opInputVarList)
                    if (opvar != null && opvar.Value != null)
                        this.entityBuilder.AasTypes.SubmodelWrapper.CreateAddElements(
                            o2, CreateMode.Instance, opvar.Value);
            }

            var opOutputVarList = op.OutputVariables;
            if (opOutputVarList != null && opOutputVarList.Count > 0)
            {
                var o2 = this.entityBuilder.CreateAddObject(o,
                    CreateMode.Instance,
                    "OperationOutputVariables",
                    ReferenceTypeIds.HasComponent, GetTypeObject().NodeId);
                foreach (var opvar in opOutputVarList)
                    if (opvar != null && opvar.Value != null)
                        this.entityBuilder.AasTypes.SubmodelWrapper.CreateAddElements(
                            o2, CreateMode.Instance, opvar.Value);
            }

            // create a method?
            if (true)
            {
                // ReSharper disable once RedundantExplicitArrayCreation
                var args = new List<Argument>[] { new List<Argument>(), new List<Argument>() };
                for (int i = 0; i < 2; i++)
                {
                    if (i == 0)
                    {
                        CreateOperationArguments(i, ref args, op.InputVariables);
                    }
                    else if (i == 1)
                    {
                        CreateOperationArguments(i, ref args, op.OutputVariables);
                    }
                }

                var unused = this.entityBuilder.CreateAddMethodState(o, CreateMode.Instance, "Operation",
                    inputArgs: args[0].ToArray(),
                    outputArgs: args[1].ToArray(),
                    referenceTypeFromParentId: ReferenceTypeIds.HasComponent);
            }

            // result
            return o;
        }

        private void CreateOperationArguments(int i, ref List<Argument>[] args, List<IOperationVariable> opVariables)
        {
            if (opVariables != null)
            {
                foreach (var opvar in opVariables)
                {
                    // TODO (MIHO, 2020-08-06): decide to from where the name comes
                    var name = "noname";

                    // TODO (MIHO, 2020-08-06): description: get "en" Version which is appropriate?
                    LocalizedText desc = new LocalizedText("");

                    // TODO (MIHO, 2020-08-06): parse UA data type out .. OK?
                    NodeId dataType = null;
                    if (opvar.Value != null && opvar.Value != null)
                    {
                        // better name .. but not best (see below)
                        if (opvar.Value.IdShort != null
                            && opvar.Value.IdShort.Trim() != "")
                            name = "" + opvar.Value.IdShort;

                        // TODO (MIHO, 2020-08-06): description: get "en" Version is appropriate?
                        desc = AasUaUtils.GetBestUaDescriptionFromAasDescription(
                            opvar.Value.Description);

                        // currenty, only accept properties as in/out arguments. 
                        // Only these have an XSD value type!!
                        var prop = opvar.Value as Property;
                        if (prop != null)
                        {
                            // TODO (MIHO, 2020-08-06): this any better?
                            if (prop.IdShort != null && prop.IdShort.Trim() != "")
                                name = "" + prop.IdShort;

                            // TODO (MIHO, 2020-08-06): description: get "en" Version is appropriate?
                            if (desc.Text == null || desc.Text == "")
                                desc = AasUaUtils.GetBestUaDescriptionFromAasDescription(
                                    opvar.Value.Description);

                            // try convert type
                            if (!AasUaUtils.AasValueTypeToUaDataType(
                                prop.ValueType.ToString(), out var dummy, out dataType))
                                dataType = null;
                        }
                    }
                    if (dataType == null)
                        continue;

                    var a = new Argument(name, dataType, -1, desc.Text ?? "");
                    args[i].Add(a);
                }

            }
        }
    }


    public class AasUaEntityDataSpecification : AasUaBaseEntity
    {
        public AasUaEntityDataSpecification(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASDataSpecificationType",
                ObjectTypeIds.BaseObjectType, preferredTypeNumId, descriptionKey: "AAS:DataSpecification");
            this.entityBuilder.AasTypes.HasInterface.CreateAddInstanceReference(this.typeObject, false,
                this.entityBuilder.AasTypes.IAASIdentifiableType.GetTypeNodeId());
        }

    }

    public class AasUaEntityDataSpecificationIEC61360 : AasUaBaseEntity
    {
        public AasUaEntityDataSpecificationIEC61360(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASDataSpecificationIEC61360Type",
                this.entityBuilder.AasTypes.DataSpecification.GetTypeNodeId(), preferredTypeNumId,
                descriptionKey: "AAS:DataSpecificationIEC61360");

            // very special rule here for the Identifiable
            this.entityBuilder.AasTypes.HasInterface.CreateAddInstanceReference(this.typeObject, false,
                this.entityBuilder.AasTypes.IAASIdentifiableType.GetTypeNodeId());
            this.entityBuilder.AasTypes.Referable.CreateAddElements(this.typeObject, CreateMode.Type);
            this.entityBuilder.AasTypes.Identification.CreateAddElements(this.typeObject, CreateMode.Instance,
                //new IIdentifiable("http://admin-shell.io/DataSpecificationTemplates/DataSpecificationIEC61360"),
                "http://admin-shell.io/DataSpecificationTemplates/DataSpecificationIEC61360",
                modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
            this.entityBuilder.AasTypes.Administration.CreateAddElements(this.typeObject, CreateMode.Instance,
                new AdministrativeInformation(version: "1", revision: "0"),
                modellingRule: AasUaNodeHelper.ModellingRule.Optional);

            // add some more elements
            this.entityBuilder.CreateAddPropertyState<LocalizedText[]>(this.typeObject, CreateMode.Type,
                "PreferredName",
                DataTypeIds.LocalizedText,
                value: null, defaultSettings: true, valueRank: 1,
                modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);

            this.entityBuilder.CreateAddPropertyState<string>(this.typeObject, CreateMode.Type,
                "ShortName",
                DataTypeIds.String, value: null, defaultSettings: true,
                modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);

            this.entityBuilder.CreateAddPropertyState<string>(this.typeObject, CreateMode.Type,
                "Unit",
                DataTypeIds.String, value: null, defaultSettings: true,
                modellingRule: AasUaNodeHelper.ModellingRule.Optional);

            this.entityBuilder.AasTypes.Reference.CreateAddElements(this.typeObject, CreateMode.Type, null,
                "UnitId",
                modellingRule: AasUaNodeHelper.ModellingRule.Optional);

            this.entityBuilder.CreateAddPropertyState<LocalizedText[]>(this.typeObject, CreateMode.Type,
                "SourceOfDefinition",
                DataTypeIds.LocalizedText,
                value: null, defaultSettings: true, valueRank: 1,
                modellingRule: AasUaNodeHelper.ModellingRule.OptionalPlaceholder);

            this.entityBuilder.CreateAddPropertyState<string>(this.typeObject, CreateMode.Type,
                "Symbol", DataTypeIds.String,
                value: null, defaultSettings: true, modellingRule: AasUaNodeHelper.ModellingRule.Optional);

            this.entityBuilder.CreateAddPropertyState<string>(this.typeObject, CreateMode.Type,
                "DataType", DataTypeIds.String,
                value: null, defaultSettings: true, modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);

            this.entityBuilder.CreateAddPropertyState<LocalizedText[]>(this.typeObject, CreateMode.Type,
                "Definition",
                DataTypeIds.LocalizedText, value: null, defaultSettings: true, valueRank: 1,
                modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);

            this.entityBuilder.CreateAddPropertyState<string>(this.typeObject, CreateMode.Type,
                "ValueFormat",
                DataTypeIds.String, value: null, defaultSettings: true,
                modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
        }

        // TODO (jtikekar, 2023-09-04): jtikekar Temporarily commented
        //public NodeState CreateAddElements(NodeState parent, CreateMode mode,
        //    AdminShell.DataSpecificationIEC61360 ds = null,
        //    AasUaNodeHelper.ModellingRule modellingRule = AasUaNodeHelper.ModellingRule.None)
        //{
        //    if (parent == null)
        //        return null;

        //    // for the sake of clarity, we're directly splitting cases
        //    if (mode == CreateMode.Type)
        //    {
        //        // containing element (only)
        //        var o = this.entityBuilder.CreateAddObject(parent, mode, "DataSpecificationIEC61360",
        //            this.entityBuilder.AasTypes.HasAddIn.GetTypeNodeId(), GetTypeObject().NodeId,
        //            modellingRule: modellingRule);
        //        return o;
        //    }
        //    else
        //    {
        //        // access
        //        if (ds == null)
        //            return null;

        //        // we can only provide minimal unique naming 
        //        var name = "DataSpecificationIEC61360";
        //        if (ds.shortName != null && this.entityBuilder.RootDataSpecifications != null)
        //            name += "_" + ds.shortName;

        //        // containing element (depending on root folder)
        //        NodeState o = null;
        //        if (this.entityBuilder.RootDataSpecifications != null)
        //        {
        //            // under common folder
        //            o = this.entityBuilder.CreateAddObject(this.entityBuilder.RootDataSpecifications, mode, name,
        //                ReferenceTypeIds.Organizes, GetTypeObject().NodeId);
        //            // link to this object
        //            parent.AddReference(this.entityBuilder.AasTypes.HasAddIn.GetTypeNodeId(), false, o.NodeId);
        //        }
        //        else
        //        {
        //            // under parent
        //            o = this.entityBuilder.CreateAddObject(parent, mode, name,
        //                this.entityBuilder.AasTypes.HasAddIn.GetTypeNodeId(), GetTypeObject().NodeId);
        //        }

        //        // add some elements        
        //        if (ds.preferredName != null && ds.preferredName.Count > 0)
        //            this.entityBuilder.CreateAddPropertyState<LocalizedText[]>(o, mode, "PreferredName",
        //                DataTypeIds.LocalizedText,
        //                value: AasUaUtils.GetUaLocalizedTexts(ds.preferredName),
        //                defaultSettings: true, valueRank: 1);

        //        if (ds.shortName != null && ds.shortName.Count > 0)
        //            this.entityBuilder.CreateAddPropertyState<LocalizedText[]>(o, mode, "ShortName",
        //                DataTypeIds.LocalizedText,
        //                value: AasUaUtils.GetUaLocalizedTexts(ds.shortName),
        //                defaultSettings: true, valueRank: 1);

        //        if (ds.unit != null)
        //            this.entityBuilder.CreateAddPropertyState<string>(o, mode, "Unit",
        //                DataTypeIds.String, value: ds.unit, defaultSettings: true);

        //        if (ds.unitId != null)
        //            this.entityBuilder.AasTypes.Reference.CreateAddElements(o, mode,
        //                Reference.CreateNew(ds.unitId?.Value), "UnitId");

        //        if (ds.sourceOfDefinition != null)
        //            this.entityBuilder.CreateAddPropertyState<string>(o, mode, "SourceOfDefinition",
        //                DataTypeIds.String, value: ds.sourceOfDefinition, defaultSettings: true);

        //        if (ds.symbol != null)
        //            this.entityBuilder.CreateAddPropertyState<string>(o, mode, "Symbol",
        //                DataTypeIds.String, value: ds.symbol, defaultSettings: true);

        //        if (ds.dataType != null)
        //            this.entityBuilder.CreateAddPropertyState<string>(o, mode, "DataType",
        //                DataTypeIds.String, value: ds.dataType, defaultSettings: true);

        //        if (ds.definition != null && ds.definition.Count > 0)
        //            this.entityBuilder.CreateAddPropertyState<LocalizedText[]>(o, mode, "Definition",
        //                DataTypeIds.LocalizedText, value: AasUaUtils.GetUaLocalizedTexts(ds.definition),
        //                defaultSettings: true, valueRank: 1);

        //        if (ds.ValueFormat != null)
        //            this.entityBuilder.CreateAddPropertyState<string>(o, mode, "ValueFormat",
        //                DataTypeIds.String, value: ds.ValueFormat, defaultSettings: true);

        //        // return
        //        return o;
        //    }
        //}
    }

    public class AasUaEntityConceptDescription : AasUaBaseEntity
    {
        public NodeState typeObjectIrdi;
        public NodeState typeObjectUri;
        public NodeState typeObjectCustom;


        public AasUaEntityConceptDescription(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            // TODO (MIHO, 2020-08-06): check, if to make super classes for UriDictionaryEntryType?
            this.typeObjectIrdi = this.entityBuilder.CreateAddObjectType("AASIrdiConceptDescriptionType",
                this.entityBuilder.AasTypes.IrdiDictionaryEntryType.GetTypeNodeId(), 0,
                descriptionKey: "AAS:ConceptDescription");
            this.entityBuilder.AasTypes.HasInterface.CreateAddInstanceReference(this.typeObjectIrdi, false,
                this.entityBuilder.AasTypes.IAASIdentifiableType.GetTypeNodeId());

            this.typeObjectUri = this.entityBuilder.CreateAddObjectType("AASUriConceptDescriptionType",
                this.entityBuilder.AasTypes.UriDictionaryEntryType.GetTypeNodeId(), 0,
                descriptionKey: "AAS:ConceptDescription");
            this.entityBuilder.AasTypes.HasInterface.CreateAddInstanceReference(this.typeObjectUri, false,
                this.entityBuilder.AasTypes.IAASIdentifiableType.GetTypeNodeId());

            this.typeObjectCustom = this.entityBuilder.CreateAddObjectType("AASCustomConceptDescriptionType",
                this.entityBuilder.AasTypes.DictionaryEntryType.GetTypeNodeId(), 0,
                descriptionKey: "AAS:ConceptDescription");
            this.entityBuilder.AasTypes.HasInterface.CreateAddInstanceReference(this.typeObjectCustom, false,
                this.entityBuilder.AasTypes.IAASIdentifiableType.GetTypeNodeId());

            // for each of them, add some elements
            // ReSharper disable once RedundantExplicitArrayCreation
            foreach (var o in new NodeState[] { this.typeObjectIrdi, this.typeObjectUri, this.typeObjectCustom })
            {
                // Referable
                this.entityBuilder.AasTypes.Referable.CreateAddElements(o, CreateMode.Type);
                // Identifiable
                this.entityBuilder.AasTypes.Identification.CreateAddElements(o, CreateMode.Type,
                    modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
                this.entityBuilder.AasTypes.Administration.CreateAddElements(o, CreateMode.Type,
                    modellingRule: AasUaNodeHelper.ModellingRule.Optional);
                // IsCaseOf
                this.entityBuilder.AasTypes.Reference.CreateAddElements(o, CreateMode.Type, null, "IsCaseOf",
                    modellingRule: AasUaNodeHelper.ModellingRule.Optional);
                // HasDataSpecification
                this.entityBuilder.AasTypes.Reference.CreateAddElements(o, CreateMode.Type, null, "DataSpecification",
                    modellingRule: AasUaNodeHelper.ModellingRule.OptionalPlaceholder);

                // data specification is a child
                // TODO (jtikekar, 2023-09-04): Temporarily commented
                //this.entityBuilder.AasTypes.DataSpecificationIEC61360.CreateAddElements(o, CreateMode.Type,
                //    modellingRule: AasUaNodeHelper.ModellingRule.MandatoryPlaceholder);
            }
        }

        public NodeState GetTypeObjectFor(string identification)
        {
            var to = this.typeObject; // shall be NULL

            //commented as per new V3.0RC02
            //if (true == identification?.IsIRI())
            //    to = this.typeObjectUri;
            //else
            //if (true == identification?.IsIRDI())
            //    to = this.typeObjectIrdi;

            // TODO (MIHO, 2021-12-30): before V3.0, there was a compare to "custom" here.

            return to;
        }

        public NodeState CreateAddElements(NodeState parent, CreateMode mode, IConceptDescription cd = null,
            AasUaNodeHelper.ModellingRule modellingRule = AasUaNodeHelper.ModellingRule.None)
        {
            if (parent == null)
                return null;

            // split directly because of complexity
            if (mode == CreateMode.Type)
            {
                // not sure, if this will required, ever
                return null;
            }
            else
            {
                // access
                if (cd == null)
                    return null;

                // makeup name            
                var name = "ConceptDescription_" + Guid.NewGuid().ToString();

                if (false)
#pragma warning disable 162
                // ReSharper disable HeuristicUnreachableCode
                {
                    // Conventional approach: build up a speaking name
                    // but: shall be target of "HasDictionaryEntry", therefore the __PURE__ identifications 
                    // need to be the name!
                    // TODO (jtikekar, 2023-09-04): Temporarily commented
                    //if (cd.GetIEC61360() != null)
                    //{
                    //    var ds = cd.GetIEC61360();
                    //    if (ds.shortName != null)
                    //        name = ds.shortName.GetDefaultStr();
                    //    if (cd.Id != null)
                    //        name += "_" + cd.Id.ToString();
                    //}
                    name = AasUaUtils.ToOpcUaName(name);
                }
                // ReSharper enable HeuristicUnreachableCode
#pragma warning restore 162
                else
                {
                    // only identification (the type object will distinct between the id type)
                    if (cd.Id != null)
                        name = cd.Id;
                }

                // containing element
                var o = this.entityBuilder.CreateAddObject(parent, mode, name,
                    ReferenceTypeIds.HasComponent, this.GetTypeObjectFor(cd.Id)?.NodeId,
                    modellingRule: modellingRule);

                // Referable
                this.entityBuilder.AasTypes.Referable.CreateAddElements(
                    o, CreateMode.Instance, cd);
                // Identifiable
                this.entityBuilder.AasTypes.Identification.CreateAddElements(
                    o, CreateMode.Instance, cd.Id);
                this.entityBuilder.AasTypes.Administration.CreateAddElements(
                    o, CreateMode.Instance, cd.Administration);
                // IsCaseOf
                if (cd.IsCaseOf != null)
                    foreach (var ico in cd.IsCaseOf)
                        this.entityBuilder.AasTypes.Reference.CreateAddElements(
                            o, CreateMode.Instance, ico, "IsCaseOf");

                // HasDataSpecification solely under the viewpoint of IEC61360
                // TODO (jtikekar, 2023-09-04): Temporarily commented
                //var eds = cd.embeddedDataSpecification?.IEC61360;
                //if (eds != null)
                //    this.entityBuilder.AasTypes.Reference.CreateAddElements(
                //        o, CreateMode.Instance, eds.dataSpecification, "DataSpecification");

                // data specification is a child
                /// TODO (jtikekar, 2023-09-04): Temporarily commented
                //var ds61360 = cd.embeddedDataSpecification?.IEC61360Content;
                //if (ds61360 != null)
                //{
                //    var unused = this.entityBuilder.AasTypes.DataSpecificationIEC61360.CreateAddElements(
                //        o, CreateMode.Instance,
                //        ds61360);
                //}

                // remember CD as NodeRecord
                this.entityBuilder.AddNodeRecord(new AasEntityBuilder.NodeRecord(o, cd.Id));

                return o;
            }
        }
    }

    // 
    // Elements from the UA spc
    //

    public class AasUaNamespaceZeroEntity : AasUaBaseEntity
    {
        public AasUaNamespaceZeroEntity(AasEntityBuilder entityBuilder, uint presetNumId = 0)
            : base(entityBuilder)
        {
            // just set node id based on existing knowledge
            this.typeObjectId = new NodeId(presetNumId, 0);
        }
    }

    public class AasUaNamespaceZeroReference : AasUaBaseEntity
    {
        public AasUaNamespaceZeroReference(AasEntityBuilder entityBuilder, uint presetNumId = 0)
            : base(entityBuilder)
        {
            // just set node id based on existing knowledge
            this.typeObjectId = new NodeId(presetNumId, 0);
        }

        public void CreateAddInstanceReference(NodeState source, bool isInverse, ExpandedNodeId target)
        {
            if (source != null && target != null && this.GetTypeNodeId() != null)
                source.AddReference(this.GetTypeNodeId(), isInverse, target);
        }
    }

    //
    // References
    // 

    public class AasUaReferenceHasAasReference : AasUaBaseEntity
    {
        public AasUaReferenceHasAasReference(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddReferenceType("AASReference", "AASReferencedBy",
                preferredTypeNumId, useZeroNS: false);
        }

        public NodeState CreateAddInstanceReference(NodeState parent)
        {
            return null;
        }
    }


    //
    // Interfaces   
    //

    public class AasUaInterfaceAASIdentifiableType : AasUaBaseEntity
    {
        public AasUaInterfaceAASIdentifiableType(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddObjectType("IAASIdentifiableType",
                this.entityBuilder.AasTypes.IAASReferableType.GetTypeNodeId() /* ObjectTypeIds.BaseObjectType */,
                preferredTypeNumId, descriptionKey: "AAS:Identifiable");

            // add some elements
            this.entityBuilder.AasTypes.Identification.CreateAddElements(this.typeObject, CreateMode.Type,
                modellingRule: AasUaNodeHelper.ModellingRule.Mandatory);
            this.entityBuilder.AasTypes.Administration.CreateAddElements(this.typeObject, CreateMode.Type,
                modellingRule: AasUaNodeHelper.ModellingRule.Optional);
        }
    }

    public class AasUaInterfaceAASReferableType : AasUaBaseEntity
    {
        public AasUaInterfaceAASReferableType(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddObjectType("IAASReferableType",
                this.entityBuilder.AasTypes.BaseInterfaceType.GetTypeNodeId() /* ObjectTypeIds.BaseObjectType */,
                preferredTypeNumId, descriptionKey: "AAS:Referable");

            // some elements
            this.entityBuilder.AasTypes.Referable.CreateAddElements(this.typeObject, CreateMode.Type);
        }
    }
}
