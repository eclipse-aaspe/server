using AasCore.Aas3_0_RC02;
using IO.Swagger.Models;
using Opc.Ua;
using System.Text.Json.Nodes;
using static AasCore.Aas3_0_RC02.Visitation;

namespace IO.Swagger.Lib.V3.SerializationModifiers
{
    public class LevelExtentTransformer : AbstractTransformerWithContext<SerializationModifierContext, JsonObject>
    {
        public override JsonObject Transform(Extension that, SerializationModifierContext context)
        {
            return Jsonization.Serialize.ToJsonObject(that);
        }

        public override JsonObject Transform(AdministrativeInformation that, SerializationModifierContext context)
        {
            return Jsonization.Serialize.ToJsonObject(that);
        }

        public override JsonObject Transform(Qualifier that, SerializationModifierContext context)
        {
            return Jsonization.Serialize.ToJsonObject(that);
        }

        public override JsonObject Transform(AssetAdministrationShell that, SerializationModifierContext context)
        {
            return Jsonization.Serialize.ToJsonObject(that);
        }

        public override JsonObject Transform(AssetInformation that, SerializationModifierContext context)
        {
            return Jsonization.Serialize.ToJsonObject(that);
        }

        public override JsonObject Transform(Resource that, SerializationModifierContext context)
        {
            return Jsonization.Serialize.ToJsonObject(that);
        }

        public override JsonObject Transform(SpecificAssetId that, SerializationModifierContext context)
        {
            return Jsonization.Serialize.ToJsonObject(that);
        }

        public override JsonObject Transform(Submodel that, SerializationModifierContext context)
        {
            return Jsonization.Serialize.ToJsonObject(that);
        }

        public override JsonObject Transform(RelationshipElement that, SerializationModifierContext context)
        {
            return Jsonization.Serialize.ToJsonObject(that);
        }

        public override JsonObject Transform(SubmodelElementList that, SerializationModifierContext context)
        {
            return Jsonization.Serialize.ToJsonObject(that);
        }

        public override JsonObject Transform(SubmodelElementCollection that, SerializationModifierContext context)
        {
            var result = Jsonization.Serialize.ToJsonObject(that);

            //Transform SubmodelElements w.r.t. extent
            if (context.Extent == ExtentEnum.WithoutBlobValue)
            {
                JsonArray valueArray = (JsonArray)result["value"];
                foreach (JsonObject item in valueArray)
                {
                    var modelType = item["modelType"].GetValue<string>();
                    if(modelType.Equals("Blob"))
                    {
                        item.Remove("value");
                    }
                } 
            }

            return result;
        }

        public override JsonObject Transform(Property that, SerializationModifierContext context)
        {
            return Jsonization.Serialize.ToJsonObject(that);
        }

        public override JsonObject Transform(MultiLanguageProperty that, SerializationModifierContext context)
        {
            return Jsonization.Serialize.ToJsonObject(that);
        }

        public override JsonObject Transform(AasCore.Aas3_0_RC02.Range that, SerializationModifierContext context)
        {
            return Jsonization.Serialize.ToJsonObject(that);
        }

        public override JsonObject Transform(ReferenceElement that, SerializationModifierContext context)
        {
            return Jsonization.Serialize.ToJsonObject(that);
        }

        public override JsonObject Transform(Blob that, SerializationModifierContext context)
        {
            var result = Jsonization.Serialize.ToJsonObject(that);

            //Check extent and remove value if WithoutBlob
            if(result != null)
            {
                if(context.Extent == ExtentEnum.WithoutBlobValue)
                {
                    result.Remove("value");
                }
            }

            return result;
        }

        public override JsonObject Transform(File that, SerializationModifierContext context)
        {
            return Jsonization.Serialize.ToJsonObject(that);
        }

        public override JsonObject Transform(AnnotatedRelationshipElement that, SerializationModifierContext context)
        {
            return Jsonization.Serialize.ToJsonObject(that);
        }

        public override JsonObject Transform(Entity that, SerializationModifierContext context)
        {
            return Jsonization.Serialize.ToJsonObject(that);
        }

        public override JsonObject Transform(EventPayload that, SerializationModifierContext context)
        {
            return Jsonization.Serialize.ToJsonObject(that);
        }

        public override JsonObject Transform(BasicEventElement that, SerializationModifierContext context)
        {
            return Jsonization.Serialize.ToJsonObject(that);
        }

        public override JsonObject Transform(Operation that, SerializationModifierContext context)
        {
            return Jsonization.Serialize.ToJsonObject(that);
        }

        public override JsonObject Transform(OperationVariable that, SerializationModifierContext context)
        {
            return Jsonization.Serialize.ToJsonObject(that);
        }

        public override JsonObject Transform(Capability that, SerializationModifierContext context)
        {
            return Jsonization.Serialize.ToJsonObject(that);
        }

        public override JsonObject Transform(ConceptDescription that, SerializationModifierContext context)
        {
            return Jsonization.Serialize.ToJsonObject(that);
        }

        public override JsonObject Transform(Reference that, SerializationModifierContext context)
        {
            return Jsonization.Serialize.ToJsonObject(that);
        }

        public override JsonObject Transform(Key that, SerializationModifierContext context)
        {
            return Jsonization.Serialize.ToJsonObject(that);
        }

        public override JsonObject Transform(LangString that, SerializationModifierContext context)
        {
            return Jsonization.Serialize.ToJsonObject(that);
        }

        public override JsonObject Transform(Environment that, SerializationModifierContext context)
        {
            return Jsonization.Serialize.ToJsonObject(that);
        }

        public override JsonObject Transform(EmbeddedDataSpecification that, SerializationModifierContext context)
        {
            return Jsonization.Serialize.ToJsonObject(that);
        }

        public override JsonObject Transform(ValueReferencePair that, SerializationModifierContext context)
        {
            return Jsonization.Serialize.ToJsonObject(that);
        }

        public override JsonObject Transform(ValueList that, SerializationModifierContext context)
        {
            return Jsonization.Serialize.ToJsonObject(that);
        }

        public override JsonObject Transform(DataSpecificationIec61360 that, SerializationModifierContext context)
        {
            return Jsonization.Serialize.ToJsonObject(that);
        }

        public override JsonObject Transform(DataSpecificationPhysicalUnit that, SerializationModifierContext context)
        {
            return Jsonization.Serialize.ToJsonObject(that);
        }
    }
}
