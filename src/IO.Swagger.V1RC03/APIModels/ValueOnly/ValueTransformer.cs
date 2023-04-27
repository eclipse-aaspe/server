
using IO.Swagger.V1RC03.APIModels.Core;
using Org.BouncyCastle.Utilities.Encoders;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using static AasCore.Aas3_0_RC02.Jsonization;
using static AasCore.Aas3_0_RC02.Visitation;
using Nodes = System.Text.Json.Nodes;

namespace IO.Swagger.V1RC03.APIModels.ValueOnly
{
    internal class ValueTransformer : ITransformerWithContext<OutputModifierContext, JsonObject>
    {
        public JsonObject Transform(Extension that, OutputModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public JsonObject Transform(AdministrativeInformation that, OutputModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public JsonObject Transform(Qualifier that, OutputModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public JsonObject Transform(AssetAdministrationShell that, OutputModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public JsonObject Transform(AssetInformation that, OutputModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public JsonObject Transform(Resource that, OutputModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public JsonObject Transform(SpecificAssetId that, OutputModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public JsonObject Transform(Submodel that, OutputModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public JsonObject Transform(RelationshipElement that, OutputModifierContext context)
        {
            var value = new JsonObject
            {
                ["first"] = Transform(that.First, context),
                ["second"] = Transform(that.Second, context)
            };
            return value;
        }

        public JsonObject Transform(SubmodelElementList that, OutputModifierContext context)
        {
            throw new System.NotImplementedException();
        }


        public JsonObject Transform(SubmodelElementCollection that, OutputModifierContext context)
        {
            var value = new JsonObject();
            if (context.IncludeChildren)
            {
                if (context.Level.Equals("core", System.StringComparison.OrdinalIgnoreCase))
                {
                    context.IncludeChildren = false;
                }
                foreach (var item in that.Value)
                {
                    if (item is Property property)
                    {
                        value[item.IdShort] = JsonValue.Create(property.Value);
                    }
                    else if (item is MultiLanguageProperty multiLanguageProperty)
                    {
                        value[item.IdShort] = TransformValue(multiLanguageProperty.Value, context);
                    }
                    else if (item is SubmodelElementList submodelElementList)
                    {
                        value[item.IdShort] = TransformValue(submodelElementList, context);
                    }
                    else
                    {
                        value[item.IdShort] = Transform(item, context);
                    }
                }
            }
            return value;
        }

        public JsonObject Transform(Property that, OutputModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public JsonObject Transform(MultiLanguageProperty that, OutputModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public JsonObject Transform(Range that, OutputModifierContext context)
        {
            var value = new JsonObject();

            if (that.Min != null)
            {
                value["min"] = JsonValue.Create(that.Min);
            }

            if (that.Max != null)
            {
                value["max"] = JsonValue.Create(that.Max);
            }

            return value;
        }

        public JsonObject Transform(ReferenceElement that, OutputModifierContext context)
        {
            return Transform(that.Value, context);
        }

        public JsonObject Transform(Blob that, OutputModifierContext context)
        {
            var value = new JsonObject
            {
                ["contentType"] = JsonValue.Create(that.ContentType)
            };

            if (context.Extent.Equals("WithBLOBValue", System.StringComparison.OrdinalIgnoreCase))
            {
                value["value"] = Nodes.JsonValue.Create(
                        System.Convert.ToBase64String(
                            that.Value));
            }
            return value;
        }

        public JsonObject Transform(File that, OutputModifierContext context)
        {
            var value = new JsonObject
            {
                ["contentType"] = JsonValue.Create(that.ContentType),
                ["value"] = JsonValue.Create(that.Value)
            };

            return value;
        }

        public JsonObject Transform(AnnotatedRelationshipElement that, OutputModifierContext context)
        {
            var value = new JsonObject
            {
                ["first"] = Transform(that.First, context),
                ["second"] = Transform(that.Second, context)
            };

            if (that.Annotations != null)
            {
                var arrayAnnotations = new JsonArray();
                if (context.IncludeChildren)
                {
                    if (context.Level.Equals("core", System.StringComparison.OrdinalIgnoreCase))
                    {
                        context.IncludeChildren = false;
                    }
                    foreach (IDataElement item in that.Annotations)
                    {
                        var annotationValue = new JsonObject();
                        if (item is Property property)
                        {
                            annotationValue[item.IdShort] = JsonValue.Create(property.Value);
                        }
                        else if (item is MultiLanguageProperty multiLanguageProperty)
                        {
                            annotationValue[item.IdShort] = TransformValue(multiLanguageProperty.Value, context);
                        }
                        else
                        {
                            annotationValue[item.IdShort] = Transform(item, context);
                        }
                        arrayAnnotations.Add(annotationValue);
                    }
                }

                value["annotations"] = arrayAnnotations;
            }

            return value;
        }

        public JsonObject Transform(Entity that, OutputModifierContext context)
        {
            var value = new JsonObject();
            if (that.Statements != null)
            {
                var statementValue = new JsonObject();
                if (context.IncludeChildren)
                {
                    if (context.Level.Equals("core", System.StringComparison.OrdinalIgnoreCase))
                    {
                        context.IncludeChildren = false;
                    }
                    foreach (ISubmodelElement item in that.Statements)
                    {
                        //var statementValue = new JsonObject();
                        if (item is Property property)
                        {
                            statementValue[item.IdShort] = JsonValue.Create(property.Value);
                        }
                        else if (item is MultiLanguageProperty multiLanguageProperty)
                        {
                            statementValue[item.IdShort] = TransformValue(multiLanguageProperty.Value, context);
                        }
                        else if (item is SubmodelElementList submodelElementList)
                        {
                            value[item.IdShort] = TransformValue(submodelElementList, context);
                        }
                        else
                        {
                            statementValue[item.IdShort] = Transform(item, context);
                        }
                    }
                }
                value["statements"] = statementValue;
            }
            value["entityType"] = Serialize.EntityTypeToJsonValue(
                    that.EntityType);

            if (that.GlobalAssetId != null)
            {
                value["globalAssetId"] = Transform(that.GlobalAssetId, context);
            }

            return value;
        }

        public JsonObject Transform(EventPayload that, OutputModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public JsonObject Transform(BasicEventElement that, OutputModifierContext context)
        {
            var value = new JsonObject
            {
                ["observed"] = Transform(that.Observed, context)
            };
            return value;
        }

        public JsonObject Transform(Operation that, OutputModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public JsonObject Transform(OperationVariable that, OutputModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public JsonObject Transform(Capability that, OutputModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public JsonObject Transform(ConceptDescription that, OutputModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public JsonObject Transform(Reference that, OutputModifierContext context)
        {
            var result = new JsonObject
            {
                ["type"] = Serialize.ReferenceTypesToJsonValue(that.Type)
            };

            var arrayKeys = new Nodes.JsonArray();
            foreach (Key item in that.Keys)
            {
                arrayKeys.Add(
                    Transform(
                        item, context));
            }
            result["keys"] = arrayKeys;

            return result;
        }

        public JsonObject Transform(Key that, OutputModifierContext context)
        {
            var result = new Nodes.JsonObject();

            result["type"] = Serialize.KeyTypesToJsonValue(
                that.Type);

            result["value"] = Nodes.JsonValue.Create(
                that.Value);

            return result;
        }

        public JsonObject Transform(LangString that, OutputModifierContext context)
        {
            var result = new Nodes.JsonObject();

            result[that.Language] = Nodes.JsonValue.Create(
                that.Text);

            return result;
        }


        public JsonObject Transform(Environment that, OutputModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public JsonObject Transform(IClass that, OutputModifierContext context)
        {
            return that.Transform(this, context);
        }

        public JsonObject Transform(EmbeddedDataSpecification that, OutputModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public JsonObject Transform(ValueReferencePair that, OutputModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public JsonObject Transform(ValueList that, OutputModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public JsonObject Transform(DataSpecificationIec61360 that, OutputModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public JsonObject Transform(DataSpecificationPhysicalUnit that, OutputModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        internal JsonNode TransformValue(List<LangString> that, OutputModifierContext context)
        {
            var arrayLangStrings = new Nodes.JsonArray();
            foreach (LangString item in that)
            {
                arrayLangStrings.Add(
                    Transform(
                        item, context));
            }

            return arrayLangStrings;
        }

        internal JsonNode TransformValue(SubmodelElementList that, OutputModifierContext context)
        {
            var arrayValues = new Nodes.JsonArray();
            if (context.IncludeChildren)
            {
                if (context.Level.Equals("core", System.StringComparison.OrdinalIgnoreCase))
                {
                    context.IncludeChildren = false;
                }
                foreach (var item in that.Value)
                {
                    if (item is Property property)
                    {
                        arrayValues.Add(JsonValue.Create(property.Value));
                    }
                    else if (item is MultiLanguageProperty multiLanguageProperty)
                    {
                        arrayValues.Add(TransformValue(multiLanguageProperty.Value, context));
                    }
                    else if (item is SubmodelElementList submodelElementList)
                    {
                        arrayValues.Add(TransformValue(submodelElementList, context));
                    }
                    else
                    {
                        arrayValues.Add(Transform(item, context));
                    }
                }
            }

            return arrayValues;
        }
    }
}
