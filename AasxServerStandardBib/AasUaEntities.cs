using AdminShellNS;
using Opc.Ua;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AasOpcUaServer
{
    public class AasUaUtils
    {
        public static LocalizedText GetUaDescriptionFromAasDescription(AdminShell.Description desc)
        {
            var res = new LocalizedText("", "");
            if (desc != null && desc.langString != null)
            {
                var found = false;
                foreach (var ls in desc.langString)
                    if (!found || ls.lang.Trim().ToLower().StartsWith("en"))
                    {
                        found = true;
                        res = new LocalizedText(ls.lang, ls.str);
                    }
            }
            return res;
        }

        public static bool AasValueTypeToUaDataType (string valueType, out Type sharpType, out NodeId dataTypeId)
        {
            // defaults
            sharpType = "".GetType();
            dataTypeId = DataTypeIds.String;
            if (valueType == null)
                return false;

            // parse
            var vt = valueType.ToLower().Trim();
            if (vt == "boolean")
            {
                sharpType = typeof(bool);
                dataTypeId = DataTypeIds.Boolean;
                return true;
            }
            else if (vt == "datetime" || vt == "datetimestamp" || vt == "time")
            {
                sharpType = typeof(Int64);
                dataTypeId = DataTypeIds.DateTime;
                return true;
            }
            else if (vt == "decimal" || vt == "integer" || vt == "long" || vt == "nonpositiveinteger" || vt == "negativeinteger")
            {
                sharpType = typeof(Int64);
                dataTypeId = DataTypeIds.Int64;
                return true;
            }
            else if (vt == "int")
            {
                sharpType = typeof(Int32);
                dataTypeId = DataTypeIds.Int32;
                return true;
            }
            else if (vt == "short")
            {
                sharpType = typeof(Int16);
                dataTypeId = DataTypeIds.Int16;
                return true;
            }
            else if (vt == "byte")
            {
                sharpType = typeof(SByte);
                dataTypeId = DataTypeIds.Byte;
                return true;
            }
            else if (vt == "nonnegativeinteger" || vt == "positiveinteger" || vt == "unsignedlong")
            {
                sharpType = typeof(UInt64);
                dataTypeId = DataTypeIds.UInt64;
                return true;
            }
            else if (vt == "unsignedint")
            {
                sharpType = typeof(UInt32);
                dataTypeId = DataTypeIds.UInt32;
                return true;
            }
            else if (vt == "unsignedshort")
            {
                sharpType = typeof(UInt16);
                dataTypeId = DataTypeIds.UInt16;
                return true;
            }
            else if (vt == "unsignedbyte")
            {
                sharpType = typeof(Byte);
                dataTypeId = DataTypeIds.Byte;
                return true;
            }
            else if (vt == "double")
            {
                sharpType = typeof(double);
                dataTypeId = DataTypeIds.Double;
                return true;
            }
            else if (vt == "float")
            {
                sharpType = typeof(float);
                dataTypeId = DataTypeIds.Float;
                return true;
            }
            else if (vt == "string")
            {
                sharpType = typeof(string);
                dataTypeId = DataTypeIds.String;
                return true;
            }

            return false;
        }
    }

    public class AasUaBaseEntity
    {
        protected AasEntityBuilder entityBuilder = null;

        public AasUaBaseEntity(AasEntityBuilder entityBuilder)
        {
            this.entityBuilder = entityBuilder;
        }

        protected NodeState typeObject = null;

        public NodeState GetTypeObject()
        {
            return typeObject;
        }
    }

    public class AasUaEntityIdentification : AasUaBaseEntity
    {
        public AasUaEntityIdentification(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddObjectType("IdentificationType", ObjectTypeIds.BaseObjectType, preferredTypeNumId);
        }

        public NodeState CreateAddInstanceObject(NodeState parent, AdminShell.Identification identification)
        {
            var o = this.entityBuilder.CreateAddObject(parent, "Identification", ReferenceTypeIds.HasComponent, GetTypeObject().NodeId);

            // this.entityBuilder.CreateAddVariable("idType", DataTypeIds.String, ReferenceTypeIds.HasProperty, "" + identification.idType);
            // this.entityBuilder.CreateAddVariable("id", DataTypeIds.String, ReferenceTypeIds.HasProperty, "" + identification.id);

            this.entityBuilder.CreateAddPropertyState<string>(o, "idType", DataTypeIds.String, "" + "" + identification.idType, ReferenceTypeIds.HasProperty);
            this.entityBuilder.CreateAddPropertyState<string>(o, "id", DataTypeIds.String, "" + "" + identification.id, ReferenceTypeIds.HasProperty);

            return o;
        }
    }

    public class AasUaEntityAdministration : AasUaBaseEntity
    {
        public AasUaEntityAdministration(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddObjectType("AdministrationType", ObjectTypeIds.BaseObjectType, preferredTypeNumId);
        }

        public NodeState CreateAddInstanceObject(NodeState parent, AdminShell.Administration administration)
        {
            if (administration == null)
                return null;

            var o = this.entityBuilder.CreateAddObject(parent, "Administration", ReferenceTypeIds.HasComponent, GetTypeObject().NodeId);

            // this.entityBuilder.CreateAddVariable("version", DataTypeIds.String, ReferenceTypeIds.HasProperty, "" + administration.version);
            // this.entityBuilder.CreateAddVariable("revision", DataTypeIds.String, ReferenceTypeIds.HasProperty, "" + administration.revision);

            this.entityBuilder.CreateAddPropertyState<string>(o, "version", DataTypeIds.String, "" + "" + administration.version, ReferenceTypeIds.HasProperty);
            this.entityBuilder.CreateAddPropertyState<string>(o, "revision", DataTypeIds.String, "" + "" + administration.revision, ReferenceTypeIds.HasProperty);

            return o;
        }
    }

    public class AasUaEntityQualifier : AasUaBaseEntity
    {
        public AasUaEntityQualifier(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddObjectType("QualifierType", ObjectTypeIds.BaseObjectType, preferredTypeNumId);
        }

        public NodeState CreateAddInstanceObject(NodeState parent, AdminShell.Qualifier qualifier)
        {
            if (qualifier == null)
                return null;

            var o = this.entityBuilder.CreateAddObject(parent, "Qualifier", ReferenceTypeIds.HasComponent, GetTypeObject().NodeId);

            this.entityBuilder.AasTypes.SemanticId.CreateAddInstanceObject(o, qualifier.semanticId, "hasSemantics");
            this.entityBuilder.CreateAddPropertyState<string>(o, "qualifierType", DataTypeIds.String, "" + qualifier.qualifierType, ReferenceTypeIds.HasProperty);
            this.entityBuilder.CreateAddPropertyState<string>(o, "qualifierValue", DataTypeIds.String, "" + qualifier.qualifierValue, ReferenceTypeIds.HasProperty);
            this.entityBuilder.AasTypes.Reference.CreateAddInstanceObject(o, qualifier.qualifierValueId, "qualifierValueId");

            return o;
        }
    }

    public class AasUaEntityKind : AasUaBaseEntity
    {
        public AasUaEntityKind(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddVariableType("KindType", VariableTypeIds.PropertyType, preferredTypeNumId);
        }

        public NodeState CreateAddInstanceObject(NodeState parent, AdminShell.Kind kind)
        {
            if (kind == null)
                return null;

            // var o = this.entityBuilder.CreateAddVariable("Kind", ReferenceTypeIds.HasProperty, parent.NodeId, "" + kind.kind);
            var o = this.entityBuilder.CreateAddPropertyState<string>(parent, "Kind", DataTypeIds.String, "" + "" + kind.kind, ReferenceTypeIds.HasProperty);

            return o;
        }
    }

    public class AasUaEntityReferable : AasUaBaseEntity
    {
        public AasUaEntityReferable(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // NO type object required
        }

        /// <summary>
        /// This adds all Referable attributes to the parent and re-defines the descriptons 
        /// </summary>
        public NodeState CreateAddFurtherVariables(NodeState parent, AdminShell.Referable refdata)
        {
            if (refdata == null)
                return null;

            // this.entityBuilder.CreateAddVariable("idShort", DataTypeIds.String, ReferenceTypeIds.HasProperty, parent.NodeId, "" + refdata.idShort);
            // this.entityBuilder.CreateAddVariable("category", DataTypeIds.String, ReferenceTypeIds.HasProperty, parent.NodeId, "" + refdata.category);
            this.entityBuilder.CreateAddPropertyState<string>(parent, "idShort", DataTypeIds.String, "" + refdata.idShort, ReferenceTypeIds.HasProperty);
            this.entityBuilder.CreateAddPropertyState<string>(parent, "category", DataTypeIds.String, "" + refdata.category, ReferenceTypeIds.HasProperty);

            // now, re-set the description on the parent
            // ISSUE: only ONE language supported!
            parent.Description = AasUaUtils.GetUaDescriptionFromAasDescription(refdata.description);

            return null;
        }
    }

    public class AasUaEntityReference : AasUaBaseEntity
    {
        public AasUaEntityReference(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASReferenceType", ObjectTypeIds.BaseObjectType, preferredTypeNumId);
        }

        public NodeState CreateAddInstanceObject(NodeState parent, AdminShell.Reference reference, string browseDisplayName = null)
        {
            if (reference == null)
                return null;

            var o = this.entityBuilder.CreateAddObject(parent, browseDisplayName ?? "Reference", ReferenceTypeIds.HasComponent, GetTypeObject().NodeId);

            var keys = this.entityBuilder.CreateAddPropertyState<string[]>(o, "Keys", DataTypeIds.Structure, null, ReferenceTypeIds.HasProperty);

            var l = new List<string>();
            if (reference.Keys != null)
                foreach (var k in reference.Keys)
                    l.Add("" + k.type + "; " + (k.local ? "True" : "False") + "; " + k.idType + "; " + k.value);
            keys.Value = l.ToArray();

            return o;
        }
    }

    public class AasUaEntitySemanticId : AasUaBaseEntity
    {
        public AasUaEntitySemanticId(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASReferenceType", ObjectTypeIds.BaseObjectType, preferredTypeNumId);
        }

        public NodeState CreateAddInstanceObject(NodeState parent, AdminShell.SemanticId semid, string browseDisplayName = null)
        {
            if (semid == null)
                return null;

            var o = this.entityBuilder.CreateAddObject(parent, browseDisplayName ?? "SemanticId", ReferenceTypeIds.HasComponent, GetTypeObject().NodeId);

            var keys = this.entityBuilder.CreateAddPropertyState<string[]>(o, "Keys", DataTypeIds.Structure, null, ReferenceTypeIds.HasProperty);

            var l = new List<string>();
            if (semid.Keys != null)
                foreach (var k in semid.Keys)
                    l.Add("" + k.type + "; " + (k.local ? "True" : "False") + "; " + k.idType + "; " + k.value);
            keys.Value = l.ToArray();

            return o;
        }
    }

    public class AasUaEntityAsset : AasUaBaseEntity
    {
        public AasUaEntityAsset(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddObjectType("AssetType", ObjectTypeIds.BaseObjectType, preferredTypeNumId);
        }

        public NodeState CreateAddInstanceObject(NodeState parent, AdminShell.Asset asset)
        {
            // access
            if (asset == null)
                return null;

            // containing element
            var o = this.entityBuilder.CreateAddObject(parent, "Asset", ReferenceTypeIds.HasComponent, GetTypeObject().NodeId);

            // Referable
            this.entityBuilder.AasTypes.Referable.CreateAddFurtherVariables(o, asset);
            // Identifiable
            this.entityBuilder.AasTypes.Identification.CreateAddInstanceObject(o, asset.identification);
            this.entityBuilder.AasTypes.Administration.CreateAddInstanceObject(o, asset.administration);
            // HasKind
            this.entityBuilder.AasTypes.Kind.CreateAddInstanceObject(o, asset.kind);
            // HasDataSpecification
            if (asset.hasDataSpecification != null && asset.hasDataSpecification.reference != null)
                foreach (var ds in asset.hasDataSpecification.reference)
                    this.entityBuilder.AasTypes.Reference.CreateAddInstanceObject(o, ds, "hasDataSpecification");
            // own attributes
            this.entityBuilder.AasTypes.Reference.CreateAddInstanceObject(o, asset.assetIdentificationModelRef, "assetIdentificationModel");

            return o;
        }
    }

    public class AasUaEntityAAS : AasUaBaseEntity
    {
        public AasUaEntityAAS(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASType", ObjectTypeIds.BaseObjectType, preferredTypeNumId);
        }

        public NodeState CreateAddInstanceObject(NodeState parent, AdminShell.AdministrationShellEnv env, AdminShell.AdministrationShell aas)
        {
            // access
            if (env == null || aas == null)
                return null;

            // containing element
            var o = this.entityBuilder.CreateAddObject(parent, aas.idShort, ReferenceTypeIds.HasComponent, GetTypeObject().NodeId);

            // Referable
            this.entityBuilder.AasTypes.Referable.CreateAddFurtherVariables(o, aas);
            // Identifiable
            this.entityBuilder.AasTypes.Identification.CreateAddInstanceObject(o, aas.identification);
            this.entityBuilder.AasTypes.Administration.CreateAddInstanceObject(o, aas.administration);
            // HasDataSpecification
            if (aas.hasDataSpecification != null && aas.hasDataSpecification.reference != null)
                foreach (var ds in aas.hasDataSpecification.reference)
                    this.entityBuilder.AasTypes.Reference.CreateAddInstanceObject(o, ds, "hasDataSpecification");
            // own attributes
            this.entityBuilder.AasTypes.Reference.CreateAddInstanceObject(o, aas.derivedFrom, "derivedFrom");

            // associated asset
            if (aas.assetRef != null)
            {
                var asset = env.FindAsset(aas.assetRef);
                if (asset != null)
                    this.entityBuilder.AasTypes.Asset.CreateAddInstanceObject(o, asset);
            }

            // associated submodels
            if (aas.submodelRefs != null)
                foreach (var smr in aas.submodelRefs)
                {
                    var sm = env.FindSubmodel(smr);
                    if (sm != null)
                        this.entityBuilder.AasTypes.Submodel.CreateAddInstanceObject(o, sm);
                }

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
            // TODO: in "I4.0AAS.v0.1.vsdx" this is call SubModelType, which is not coherent to AASiD Part 1
            this.typeObject = this.entityBuilder.CreateAddObjectType("SubmodelType", ObjectTypeIds.BaseObjectType, preferredTypeNumId);
        }

        public NodeState CreateAddInstanceObject(NodeState parent, AdminShell.Submodel sm)
        {
            // access
            if (sm == null)
                return null;

            // containing element
            var o = this.entityBuilder.CreateAddObject(parent, "" + sm.idShort, ReferenceTypeIds.HasComponent, GetTypeObject().NodeId);

            // Referable
            this.entityBuilder.AasTypes.Referable.CreateAddFurtherVariables(o, sm);
            // Identifiable
            this.entityBuilder.AasTypes.Identification.CreateAddInstanceObject(o, sm.identification);
            this.entityBuilder.AasTypes.Administration.CreateAddInstanceObject(o, sm.administration);
            // HasSemantics
            this.entityBuilder.AasTypes.SemanticId.CreateAddInstanceObject(o, sm.semanticId, "hasSemantics");
            // HasKind
            this.entityBuilder.AasTypes.Kind.CreateAddInstanceObject(o, sm.kind);
            // HasDataSpecification
            if (sm.hasDataSpecification != null && sm.hasDataSpecification.reference != null)
                foreach (var ds in sm.hasDataSpecification.reference)
                    this.entityBuilder.AasTypes.Reference.CreateAddInstanceObject(o, ds, "hasDataSpecification");
            // Qualifiable
            if (sm.qualifiers != null)
                foreach (var q in sm.qualifiers)
                    this.entityBuilder.AasTypes.Qualifier.CreateAddInstanceObject(o, q);

            // SubmodelElements
            if (sm.submodelElements != null)
                foreach (var smw in sm.submodelElements)
                    if (smw.submodelElement != null)
                        this.entityBuilder.AasTypes.SubmodelWrapper.CreateAddInstanceObject(o, smw);

            // result
            return o;
        }
    }

    public class AasUaEntitySubmodelElementBase : AasUaBaseEntity
    {
        public AasUaEntitySubmodelElementBase(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // do NOT create type object, as this is done by sub-class
        }

        public NodeState PopulateInstanceObject(NodeState o, AdminShell.SubmodelElement sme)
        {
            // access
            if (o == null || sme == null)
                return null;

            // Referable
            this.entityBuilder.AasTypes.Referable.CreateAddFurtherVariables(o, sme);
            // HasSemantics
            this.entityBuilder.AasTypes.SemanticId.CreateAddInstanceObject(o, sme.semanticId, "hasSemantics");
            // HasKind
            this.entityBuilder.AasTypes.Kind.CreateAddInstanceObject(o, sme.kind);
            // HasDataSpecification
            if (sme.hasDataSpecification != null && sme.hasDataSpecification.reference != null)
                foreach (var ds in sme.hasDataSpecification.reference)
                    this.entityBuilder.AasTypes.Reference.CreateAddInstanceObject(o, ds, "hasDataSpecification");
            // Qualifiable
            if (sme.qualifiers != null)
                foreach (var q in sme.qualifiers)
                    this.entityBuilder.AasTypes.Qualifier.CreateAddInstanceObject(o, q);

            // result
            return o;
        }
    }

    public class AasUaEntitySubmodelWrapper : AasUaBaseEntity
    {
        public AasUaEntitySubmodelWrapper(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // do NOT create type object, as this is done by sub-class
        }

        public NodeState CreateAddInstanceObject(NodeState o, AdminShell.SubmodelElementWrapper smw)
        {
            // access
            if (o == null || smw == null || smw.submodelElement == null)
                return null;

            if (smw.submodelElement is AdminShell.SubmodelElementCollection)
            {
                var coll = smw.submodelElement as AdminShell.SubmodelElementCollection;
                if (coll.ordered)
                    return this.entityBuilder.AasTypes.OrderedCollection.CreateAddInstanceObject(o, coll);
                else
                    return this.entityBuilder.AasTypes.Collection.CreateAddInstanceObject(o, coll);
            }
            else if (smw.submodelElement is AdminShell.Property)
                return this.entityBuilder.AasTypes.Property.CreateAddInstanceObject(o, smw.submodelElement as AdminShell.Property);
            else if (smw.submodelElement is AdminShell.File)
                return this.entityBuilder.AasTypes.File.CreateAddInstanceObject(o, smw.submodelElement as AdminShell.File);
            else if (smw.submodelElement is AdminShell.Blob)
                return this.entityBuilder.AasTypes.Blob.CreateAddInstanceObject(o, smw.submodelElement as AdminShell.Blob);
            else if (smw.submodelElement is AdminShell.ReferenceElement)
                return this.entityBuilder.AasTypes.ReferenceElement.CreateAddInstanceObject(o, smw.submodelElement as AdminShell.ReferenceElement);
            else if (smw.submodelElement is AdminShell.RelationshipElement)
                return this.entityBuilder.AasTypes.RelationshipElement.CreateAddInstanceObject(o, smw.submodelElement as AdminShell.RelationshipElement);
            else if (smw.submodelElement is AdminShell.Operation)
                return this.entityBuilder.AasTypes.Operation.CreateAddInstanceObject(o, smw.submodelElement as AdminShell.Operation);

            // nope
            return null;
        }
    }

    public class AasUaEntityProperty : AasUaEntitySubmodelElementBase
    {
        public AasUaEntityProperty(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASPropertyType", ObjectTypeIds.BaseObjectType, preferredTypeNumId);
        }

        public NodeState CreateAddInstanceObject(NodeState parent, AdminShell.Property prop)
        {
            // access
            if (prop == null)
                return null;

            // containing element
            var o = this.entityBuilder.CreateAddObject(parent, "" + prop.idShort, ReferenceTypeIds.HasComponent, GetTypeObject().NodeId);

            // populate common attributes
            base.PopulateInstanceObject(o, prop);

            // TODO: not sure if to add these
            this.entityBuilder.CreateAddPropertyState<string>(o, "valueType", DataTypeIds.String, "" + prop.valueType, ReferenceTypeIds.HasProperty);
            this.entityBuilder.AasTypes.Reference.CreateAddInstanceObject(o, prop.valueId, "valueId");

            // TODO: aim is to support many types natively
            var vt = (prop.valueType ?? "").ToLower().Trim();
            if (vt == "boolean")
            {
                var x = (prop.value ?? "").ToLower().Trim();
                this.entityBuilder.CreateAddPropertyState<bool>(o, "Value", DataTypeIds.Boolean, x == "true", ReferenceTypeIds.HasProperty);
            }
            else if (vt == "datetime" || vt == "datetimestamp" || vt == "time")
            {
                DateTime dt;
                if (DateTime.TryParse(prop.value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out dt))
                    this.entityBuilder.CreateAddPropertyState<Int64>(o, "Value", DataTypeIds.DateTime, dt.ToFileTimeUtc(), ReferenceTypeIds.HasProperty);
            }
            else if (vt == "decimal" || vt == "integer" || vt == "long" || vt == "nonpositiveinteger" || vt == "negativeinteger")
            {
                Int64 v;
                if (Int64.TryParse(prop.value, out v))
                    this.entityBuilder.CreateAddPropertyState<Int64>(o, "Value", DataTypeIds.Int64, v, ReferenceTypeIds.HasProperty);
            }
            else if (vt == "int")
            {
                Int32 v;
                if (Int32.TryParse(prop.value, out v))
                    this.entityBuilder.CreateAddPropertyState<Int32>(o, "Value", DataTypeIds.Int32, v, ReferenceTypeIds.HasProperty);
            }
            else if (vt == "short")
            {
                Int16 v;
                if (Int16.TryParse(prop.value, out v))
                    this.entityBuilder.CreateAddPropertyState<Int16>(o, "Value", DataTypeIds.Int16, v, ReferenceTypeIds.HasProperty);
            }
            else if (vt == "byte")
            {
                SByte v;
                if (SByte.TryParse(prop.value, out v))
                    this.entityBuilder.CreateAddPropertyState<SByte>(o, "Value", DataTypeIds.Byte, v, ReferenceTypeIds.HasProperty);
            }
            else if (vt == "nonnegativeinteger" || vt == "positiveinteger" || vt == "unsignedlong")
            {
                UInt64 v;
                if (UInt64.TryParse(prop.value, out v))
                    this.entityBuilder.CreateAddPropertyState<UInt64>(o, "Value", DataTypeIds.UInt64, v, ReferenceTypeIds.HasProperty);
            }
            else if (vt == "unsignedint")
            {
                UInt32 v;
                if (UInt32.TryParse(prop.value, out v))
                    this.entityBuilder.CreateAddPropertyState<UInt32>(o, "Value", DataTypeIds.UInt32, v, ReferenceTypeIds.HasProperty);
            }
            else if (vt == "unsignedshort")
            {
                UInt16 v;
                if (UInt16.TryParse(prop.value, out v))
                    this.entityBuilder.CreateAddPropertyState<UInt16>(o, "Value", DataTypeIds.UInt16, v, ReferenceTypeIds.HasProperty);
            }
            else if (vt == "unsignedbyte")
            {
                Byte v;
                if (Byte.TryParse(prop.value, out v))
                    this.entityBuilder.CreateAddPropertyState<Byte>(o, "Value", DataTypeIds.Byte, v, ReferenceTypeIds.HasProperty);
            }
            else if (vt == "double")
            {
                double v;
                if (double.TryParse(prop.value, NumberStyles.Any, CultureInfo.InvariantCulture, out v))
                    this.entityBuilder.CreateAddPropertyState<double>(o, "Value", DataTypeIds.Double, v, ReferenceTypeIds.HasProperty);
            }
            else if (vt == "float")
            {
                float v;
                if (float.TryParse(prop.value, NumberStyles.Any, CultureInfo.InvariantCulture, out v))
                    this.entityBuilder.CreateAddPropertyState<float>(o, "Value", DataTypeIds.Float, v, ReferenceTypeIds.HasProperty);
            }
            else
            {
                // leave in string
                this.entityBuilder.CreateAddPropertyState<string>(o, "Value", DataTypeIds.String, prop.value, ReferenceTypeIds.HasProperty);
            }

            // result
            return o;
        }
    }

    public class AasUaEntityCollection : AasUaEntitySubmodelElementBase
    {
        public AasUaEntityCollection(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddObjectType("CollectionType", ObjectTypeIds.BaseObjectType, preferredTypeNumId);
        }

        public NodeState CreateAddInstanceObject(NodeState parent, AdminShell.SubmodelElementCollection coll)
        {
            // access
            if (coll == null)
                return null;

            // containing element
            var o = this.entityBuilder.CreateAddObject(parent, "" + coll.idShort, ReferenceTypeIds.HasComponent, GetTypeObject().NodeId);

            // populate common attributes
            base.PopulateInstanceObject(o, coll);

            // own attributes
            this.entityBuilder.CreateAddPropertyState<bool>(o, "ordered", DataTypeIds.Boolean, coll.ordered, ReferenceTypeIds.HasProperty);
            this.entityBuilder.CreateAddPropertyState<bool>(o, "allowDuplicates", DataTypeIds.Boolean, coll.allowDuplicates, ReferenceTypeIds.HasProperty);

            // values
            if (coll.value != null)
                foreach (var smw in coll.value)
                    if (smw.submodelElement != null)
                        this.entityBuilder.AasTypes.SubmodelWrapper.CreateAddInstanceObject(o, smw);

            // result
            return o;
        }
    }

    public class AasUaEntityOrderedCollection : AasUaEntityCollection
    {
        public AasUaEntityOrderedCollection(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddObjectType("OrderedCollectionType", ObjectTypeIds.BaseObjectType, preferredTypeNumId);
        }

        public new NodeState CreateAddInstanceObject(NodeState parent, AdminShell.SubmodelElementCollection coll)
        {
            // pass on to base class
            return base.CreateAddInstanceObject(parent, coll);
        }
    }

    public class AasUaEntityFile : AasUaEntitySubmodelElementBase
    {
        public AasUaEntityFile(AasEntityBuilder entityBuilder, uint preferredTypeNumId = 0)
            : base(entityBuilder)
        {
            // create type object
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASFileType", ObjectTypeIds.BaseObjectType, preferredTypeNumId);
        }

        public NodeState CreateAddInstanceObject(NodeState parent, AdminShell.File file)
        {
            // access
            if (file == null)
                return null;

            // containing element
            var o = this.entityBuilder.CreateAddObject(parent, "" + file.idShort, ReferenceTypeIds.HasComponent, GetTypeObject().NodeId);

            // populate common attributes
            base.PopulateInstanceObject(o, file);

            // own attributes
            this.entityBuilder.CreateAddPropertyState<string>(o, "mimeType", DataTypeIds.String, file.mimeType, ReferenceTypeIds.HasProperty);
            this.entityBuilder.CreateAddPropertyState<string>(o, "FileReference", DataTypeIds.String, file.value, ReferenceTypeIds.HasProperty);

            // ISSUE: dont know to handle FileType (the stack is missing these features?)
            this.entityBuilder.AasTypes.FileType.CreateAddInstanceObject(o, file);

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
            this.typeObject = this.entityBuilder.CreateAddObjectType("AASBlobType", ObjectTypeIds.BaseObjectType, preferredTypeNumId);
        }

        public NodeState CreateAddInstanceObject(NodeState parent, AdminShell.Blob blob)
        {
            // access
            if (blob == null)
                return null;

            // containing element
            var o = this.entityBuilder.CreateAddObject(parent, "" + blob.idShort, ReferenceTypeIds.HasComponent, GetTypeObject().NodeId);

            // populate common attributes
            base.PopulateInstanceObject(o, blob);

            // own attributes
            this.entityBuilder.CreateAddPropertyState<string>(o, "mimeType", DataTypeIds.String, blob.mimeType, ReferenceTypeIds.HasProperty);
            this.entityBuilder.CreateAddPropertyState<string>(o, "value", DataTypeIds.String, blob.value, ReferenceTypeIds.HasProperty);

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
            this.typeObject = this.entityBuilder.CreateAddObjectType("ReferenceElementType", ObjectTypeIds.BaseObjectType, preferredTypeNumId);
        }

        public NodeState CreateAddInstanceObject(NodeState parent, AdminShell.ReferenceElement refElem)
        {
            // access
            if (refElem == null)
                return null;

            // containing element
            var o = this.entityBuilder.CreateAddObject(parent, "" + refElem.idShort, ReferenceTypeIds.HasComponent, GetTypeObject().NodeId);

            // populate common attributes
            base.PopulateInstanceObject(o, refElem);

            // own attributes
            this.entityBuilder.AasTypes.Reference.CreateAddInstanceObject(o, refElem.value, "value");

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
            this.typeObject = this.entityBuilder.CreateAddObjectType("RelationshipElementType", ObjectTypeIds.BaseObjectType, preferredTypeNumId);
        }

        public NodeState CreateAddInstanceObject(NodeState parent, AdminShell.RelationshipElement relElem)
        {
            // access
            if (relElem == null)
                return null;

            // containing element
            var o = this.entityBuilder.CreateAddObject(parent, "" + relElem.idShort, ReferenceTypeIds.HasComponent, GetTypeObject().NodeId);

            // populate common attributes
            base.PopulateInstanceObject(o, relElem);

            // own attributes
            this.entityBuilder.AasTypes.Reference.CreateAddInstanceObject(o, relElem.first, "first");
            this.entityBuilder.AasTypes.Reference.CreateAddInstanceObject(o, relElem.second, "second");

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
            this.typeObject = this.entityBuilder.CreateAddObjectType("OperationVariableType", ObjectTypeIds.BaseObjectType, preferredTypeNumId);
        }

        public NodeState CreateAddInstanceObject(NodeState parent, AdminShell.OperationVariable opvar)
        {
            // access
            if (opvar == null)
                return null;

            // containing element
            var o = this.entityBuilder.CreateAddObject(parent, "" + opvar.idShort, ReferenceTypeIds.HasComponent, GetTypeObject().NodeId);

            // populate common attributes
            base.PopulateInstanceObject(o, opvar);

            // own attributes
            if (opvar.value != null)
                if (opvar.value.submodelElement != null)
                    this.entityBuilder.AasTypes.SubmodelWrapper.CreateAddInstanceObject(o, opvar.value);

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
            this.typeObject = this.entityBuilder.CreateAddObjectType("OperationType", ObjectTypeIds.BaseObjectType, preferredTypeNumId);
        }

        public NodeState CreateAddInstanceObject(NodeState parent, AdminShell.Operation op)
        {
            // access
            if (op == null)
                return null;

            // containing element
            var o = this.entityBuilder.CreateAddObject(parent, "" + op.idShort, ReferenceTypeIds.HasComponent, GetTypeObject().NodeId);

            // populate common attributes
            base.PopulateInstanceObject(o, op);

            // own AAS attributes (in/out op vars)
            for (int i=0; i<2; i++)
            {
                var opvarList = op[i];
                if (opvarList != null && opvarList.Count > 0)
                {
                    var o2 = this.entityBuilder.CreateAddObject(o, (i == 0) ? "in" : "out", ReferenceTypeIds.HasComponent, GetTypeObject().NodeId);
                    foreach (var opvar in opvarList)
                        this.entityBuilder.AasTypes.OperationVariable.CreateAddInstanceObject(o2, opvar);
                }
            }

            // create a method?
            if (true)
            {
                var args = new List<Argument>[] { new List<Argument>(), new List<Argument>() };
                for (int i = 0; i < 2; i++)
                    if (op[i] != null)
                        foreach (var opvar in op[i])
                        {
                            // TODO: decide to from where the name comes
                            var name = "noname";
                            if (opvar.idShort != null && opvar.idShort.Trim() != "")
                                name = "" + opvar.idShort;

                            // TODO: description: get "en" version is appropriate?
                            var desc = AasUaUtils.GetUaDescriptionFromAasDescription(opvar.description);

                            // TODO: parse UA data type out .. OK?
                            NodeId dataType = null; 
                            if (opvar.value != null && opvar.value.submodelElement != null)
                            {
                                // currenty, only accept properties as in/out arguments. Only these have an XSD value type!!
                                var prop = opvar.value.submodelElement as AdminShell.Property;
                                if (prop != null && prop.valueType != null)
                                {
                                    // TODO: this any better?
                                    if (prop.idShort != null && prop.idShort.Trim() != "")
                                        name = "" + prop.idShort;
                                    // try convert type
                                    Type sharpType;
                                    if (!AasUaUtils.AasValueTypeToUaDataType(prop.valueType, out sharpType, out dataType))
                                        dataType = null;
                                }
                            }
                            if (dataType == null)
                                continue;

                            var a = new Argument(name, dataType, -1, desc.Text ?? "");
                            args[i].Add(a);
                        }

                var opmeth = this.entityBuilder.CreateAddMethodState(o, "Operation",
                    inputArgs: args[0].ToArray(),
                    outputArgs: args[1].ToArray(),
                    referenceTypeFromParentId: ReferenceTypeIds.HasComponent);
            }

            // result
            return o;
        }
    }


}
