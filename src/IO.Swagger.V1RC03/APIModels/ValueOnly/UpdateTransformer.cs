
using System;
using System.Linq;
using static AasCore.Aas3_0_RC02.Visitation;

namespace IO.Swagger.V1RC03.APIModels.ValueOnly
{
    internal class UpdateTransformer : ITransformerWithContext<UpdateContext, IClass>
    {
        public IClass Transform(IClass that, UpdateContext context)
        {
            return that.Transform(this, context);
        }

        public IClass Transform(Extension that, UpdateContext context)
        {
            throw new NotImplementedException();
        }

        public IClass Transform(AdministrativeInformation that, UpdateContext context)
        {
            throw new NotImplementedException();
        }

        public IClass Transform(Qualifier that, UpdateContext context)
        {
            throw new NotImplementedException();
        }

        public IClass Transform(AssetAdministrationShell that, UpdateContext context)
        {
            throw new NotImplementedException();
        }

        public IClass Transform(AssetInformation that, UpdateContext context)
        {
            throw new NotImplementedException();
        }

        public IClass Transform(Resource that, UpdateContext context)
        {
            throw new NotImplementedException();
        }

        public IClass Transform(SpecificAssetId that, UpdateContext context)
        {
            throw new NotImplementedException();
        }

        public IClass Transform(Submodel that, UpdateContext context)
        {
            var source = context.Source as Submodel;
            var outputModifierContext = context.OutputModifierContext;
            if (outputModifierContext.Content.Equals("value", StringComparison.OrdinalIgnoreCase))
            {
                if (source.SubmodelElements.Any())
                {
                    //First check if number of elements in source and that are equal
                    if (that.SubmodelElements == null || that.SubmodelElements.Count != source.SubmodelElements.Count)
                    {
                        throw new Exceptions.InvalidNumberOfChildElementsException("Submodel : " + that.IdShort, source.SubmodelElements.Count, that.SubmodelElements.Count);
                    }
                    for (int i = 0; i < source.SubmodelElements.Count; i++)
                    {
                        UpdateImplementation.Update(that.SubmodelElements[i], source.SubmodelElements[i], outputModifierContext);
                    }
                }
                return that;
            }

            that.Extensions = source.Extensions;
            that.Category = source.Category;
            that.IdShort = source.IdShort;
            that.DisplayName = source.DisplayName;
            that.Description = source.Description;
            that.Checksum = source.Checksum;
            that.Kind = source.Kind;
            that.SemanticId = source.SemanticId;
            that.SupplementalSemanticIds = source.SupplementalSemanticIds;
            that.Qualifiers = source.Qualifiers;
            that.EmbeddedDataSpecifications = source.EmbeddedDataSpecifications;

            if (outputModifierContext.IncludeChildren)
            {
                if (outputModifierContext.Level.Equals("core", StringComparison.OrdinalIgnoreCase))
                {
                    outputModifierContext.IncludeChildren = false;
                }
                if (source.SubmodelElements.Any())
                {
                    //First check if number of elements in source and that are equal
                    if (that.SubmodelElements == null || that.SubmodelElements.Count != source.SubmodelElements.Count)
                    {
                        throw new Exceptions.InvalidNumberOfChildElementsException("Submodel : " + that.IdShort, source.SubmodelElements.Count, that.SubmodelElements.Count);
                    }
                    for (int i = 0; i < source.SubmodelElements.Count; i++)
                    {
                        UpdateImplementation.Update(that.SubmodelElements[i], source.SubmodelElements[i], outputModifierContext);
                    }
                }
            }

            return that;
        }

        public IClass Transform(RelationshipElement that, UpdateContext context)
        {
            var source = context.Source as RelationshipElement;
            var outputModifierContext = context.OutputModifierContext;
            if (outputModifierContext.Content.Equals("value", StringComparison.OrdinalIgnoreCase))
            {
                that.First = source.First;
                that.Second = source.Second;
                return that;
            }

            that.Extensions = source.Extensions;
            that.Category = source.Category;
            that.IdShort = source.IdShort;
            that.DisplayName = source.DisplayName;
            that.Description = source.Description;
            that.Checksum = source.Checksum;
            that.Kind = source.Kind;
            that.SemanticId = source.SemanticId;
            that.SupplementalSemanticIds = source.SupplementalSemanticIds;
            that.Qualifiers = source.Qualifiers;
            that.EmbeddedDataSpecifications = source.EmbeddedDataSpecifications;

            if (!outputModifierContext.Content.Equals("metadata", StringComparison.OrdinalIgnoreCase))
            {
                that.First = source.First;
                that.Second = source.Second;
            }

            return that;
        }

        public IClass Transform(SubmodelElementList that, UpdateContext context)
        {
            var source = context.Source as SubmodelElementList;
            var outputModifierContext = context.OutputModifierContext;
            if (outputModifierContext.Content.Equals("value", StringComparison.OrdinalIgnoreCase))
            {
                if (source.Value.Any())
                {
                    //First check if number of elements in source and that are equal
                    if (that.Value == null || that.Value.Count != source.Value.Count)
                    {
                        throw new Exceptions.InvalidNumberOfChildElementsException("SubmodelElementList : " + that.IdShort, source.Value.Count, that.Value.Count);
                    }
                    for (int i = 0; i < source.Value.Count; i++)
                    {
                        UpdateImplementation.Update(that.Value[i], source.Value[i], outputModifierContext);
                    }
                }
                return that;
            }

            that.Extensions = source.Extensions;
            that.Category = source.Category;
            that.IdShort = source.IdShort;
            that.DisplayName = source.DisplayName;
            that.Description = source.Description;
            that.Checksum = source.Checksum;
            that.Kind = source.Kind;
            that.SemanticId = source.SemanticId;
            that.SupplementalSemanticIds = source.SupplementalSemanticIds;
            that.Qualifiers = source.Qualifiers;
            that.EmbeddedDataSpecifications = source.EmbeddedDataSpecifications;

            if (outputModifierContext.IncludeChildren)
            {
                if (outputModifierContext.Level.Equals("core", StringComparison.OrdinalIgnoreCase))
                {
                    outputModifierContext.IncludeChildren = false;
                }
                if (source.Value.Any())
                {
                    //First check if number of elements in source and that are equal
                    if (that.Value == null || that.Value.Count != source.Value.Count)
                    {
                        throw new Exceptions.InvalidNumberOfChildElementsException("SubmodelElementList : " + that.IdShort, source.Value.Count, that.Value.Count);
                    }
                    for (int i = 0; i < source.Value.Count; i++)
                    {
                        UpdateImplementation.Update(that.Value[i], source.Value[i], outputModifierContext);
                    }
                }
            }

            return that;
        }

        public IClass Transform(SubmodelElementCollection that, UpdateContext context)
        {
            var source = context.Source as SubmodelElementCollection;
            var outputModifierContext = context.OutputModifierContext;
            if (outputModifierContext.Content.Equals("value", StringComparison.OrdinalIgnoreCase))
            {
                if (source.Value.Any())
                {
                    //First check if number of elements in source and that are equal
                    if (that.Value == null || that.Value.Count != source.Value.Count)
                    {
                        throw new Exceptions.InvalidNumberOfChildElementsException("SubmodelElementCollection : " + that.IdShort, source.Value.Count, that.Value.Count);
                    }
                    for (int i = 0; i < source.Value.Count; i++)
                    {
                        UpdateImplementation.Update(that.Value[i], source.Value[i], outputModifierContext);
                    }
                }
                return that;
            }

            that.Extensions = source.Extensions;
            that.Category = source.Category;
            that.IdShort = source.IdShort;
            that.DisplayName = source.DisplayName;
            that.Description = source.Description;
            that.Checksum = source.Checksum;
            that.Kind = source.Kind;
            that.SemanticId = source.SemanticId;
            that.SupplementalSemanticIds = source.SupplementalSemanticIds;
            that.Qualifiers = source.Qualifiers;
            that.EmbeddedDataSpecifications = source.EmbeddedDataSpecifications;

            if (outputModifierContext.IncludeChildren)
            {
                if (outputModifierContext.Level.Equals("core", StringComparison.OrdinalIgnoreCase))
                {
                    outputModifierContext.IncludeChildren = false;
                }
                if (source.Value.Any())
                {
                    //First check if number of elements in source and that are equal
                    if (that.Value == null || that.Value.Count != source.Value.Count)
                    {
                        throw new Exceptions.InvalidNumberOfChildElementsException("SubmodelElementCollection : " + that.IdShort, source.Value.Count, that.Value.Count);
                    }
                    for (int i = 0; i < source.Value.Count; i++)
                    {
                        UpdateImplementation.Update(that.Value[i], source.Value[i], outputModifierContext);
                    }
                }
            }

            return that;
        }

        public IClass Transform(Property that, UpdateContext context)
        {
            var source = context.Source as Property;
            var outputModifierContext = context.OutputModifierContext;
            if (outputModifierContext.Content.Equals("value", StringComparison.OrdinalIgnoreCase))
            {
                that.Value = source.Value;
                return that;
            }

            that.Extensions = source.Extensions;
            that.Category = source.Category;
            that.IdShort = source.IdShort;
            that.DisplayName = source.DisplayName;
            that.Description = source.Description;
            that.Checksum = source.Checksum;
            that.Kind = source.Kind;
            that.SemanticId = source.SemanticId;
            that.SupplementalSemanticIds = source.SupplementalSemanticIds;
            that.Qualifiers = source.Qualifiers;
            that.EmbeddedDataSpecifications = source.EmbeddedDataSpecifications;
            that.ValueType = source.ValueType;
            that.ValueId = source.ValueId;

            if (!outputModifierContext.Content.Equals("metadata", StringComparison.OrdinalIgnoreCase))
            {
                that.Value = source.Value;
            }

            return that;
        }

        public IClass Transform(MultiLanguageProperty that, UpdateContext context)
        {
            var source = context.Source as MultiLanguageProperty;
            var outputModifierContext = context.OutputModifierContext;
            if (outputModifierContext.Content.Equals("value", StringComparison.OrdinalIgnoreCase))
            {
                that.Value = source.Value;
                return that;
            }

            that.Extensions = source.Extensions;
            that.Category = source.Category;
            that.IdShort = source.IdShort;
            that.DisplayName = source.DisplayName;
            that.Description = source.Description;
            that.Checksum = source.Checksum;
            that.Kind = source.Kind;
            that.SemanticId = source.SemanticId;
            that.SupplementalSemanticIds = source.SupplementalSemanticIds;
            that.Qualifiers = source.Qualifiers;
            that.EmbeddedDataSpecifications = source.EmbeddedDataSpecifications;
            that.ValueId = source.ValueId;

            if (!outputModifierContext.Content.Equals("metadata", StringComparison.OrdinalIgnoreCase))
            {
                that.Value = source.Value;
            }

            return that;
        }

        public IClass Transform(AasCore.Aas3_0_RC02.Range that, UpdateContext context)
        {
            var source = context.Source as AasCore.Aas3_0_RC02.Range;
            var outputModifierContext = context.OutputModifierContext;
            if (outputModifierContext.Content.Equals("value", StringComparison.OrdinalIgnoreCase))
            {
                that.Min = source.Min;
                that.Max = source.Max;
                return that;
            }

            that.Extensions = source.Extensions;
            that.Category = source.Category;
            that.IdShort = source.IdShort;
            that.DisplayName = source.DisplayName;
            that.Description = source.Description;
            that.Checksum = source.Checksum;
            that.Kind = source.Kind;
            that.SemanticId = source.SemanticId;
            that.SupplementalSemanticIds = source.SupplementalSemanticIds;
            that.Qualifiers = source.Qualifiers;
            that.EmbeddedDataSpecifications = source.EmbeddedDataSpecifications;
            that.ValueType = source.ValueType;

            if (!outputModifierContext.Content.Equals("metadata", StringComparison.OrdinalIgnoreCase))
            {
                that.Min = source.Min;
                that.Max = source.Max;
            }

            return that;
        }

        public IClass Transform(ReferenceElement that, UpdateContext context)
        {
            var source = context.Source as ReferenceElement;
            var outputModifierContext = context.OutputModifierContext;
            if (outputModifierContext.Content.Equals("value", StringComparison.OrdinalIgnoreCase))
            {
                that.Value = source.Value;
                return that;
            }

            that.Extensions = source.Extensions;
            that.Category = source.Category;
            that.IdShort = source.IdShort;
            that.DisplayName = source.DisplayName;
            that.Description = source.Description;
            that.Checksum = source.Checksum;
            that.Kind = source.Kind;
            that.SemanticId = source.SemanticId;
            that.SupplementalSemanticIds = source.SupplementalSemanticIds;
            that.Qualifiers = source.Qualifiers;
            that.EmbeddedDataSpecifications = source.EmbeddedDataSpecifications;

            if (!outputModifierContext.Content.Equals("metadata", StringComparison.OrdinalIgnoreCase))
            {
                that.Value = source.Value;
            }

            return that;
        }

        public IClass Transform(Blob that, UpdateContext context)
        {
            var source = context.Source as Blob;
            var outputModifierContext = context.OutputModifierContext;
            if (outputModifierContext.Content.Equals("value", StringComparison.OrdinalIgnoreCase))
            {
                that.ContentType = source.ContentType;
                if (outputModifierContext.Extent.Equals("withBlobValue", StringComparison.OrdinalIgnoreCase))
                {
                    that.Value = source.Value;
                }
                return that;
            }

            that.Extensions = source.Extensions;
            that.Category = source.Category;
            that.IdShort = source.IdShort;
            that.DisplayName = source.DisplayName;
            that.Description = source.Description;
            that.Checksum = source.Checksum;
            that.Kind = source.Kind;
            that.SemanticId = source.SemanticId;
            that.SupplementalSemanticIds = source.SupplementalSemanticIds;
            that.Qualifiers = source.Qualifiers;
            that.EmbeddedDataSpecifications = source.EmbeddedDataSpecifications;

            if (!outputModifierContext.Content.Equals("metadata", StringComparison.OrdinalIgnoreCase))
            {
                that.ContentType = source.ContentType;
                that.Value = source.Value;
            }

            return that;
        }

        public IClass Transform(File that, UpdateContext context)
        {
            var source = context.Source as File;
            var outputModifierContext = context.OutputModifierContext;
            if (outputModifierContext.Content.Equals("value", StringComparison.OrdinalIgnoreCase))
            {
                that.ContentType = source.ContentType;
                that.Value = source.Value;
                return that;
            }

            that.Extensions = source.Extensions;
            that.Category = source.Category;
            that.IdShort = source.IdShort;
            that.DisplayName = source.DisplayName;
            that.Description = source.Description;
            that.Checksum = source.Checksum;
            that.Kind = source.Kind;
            that.SemanticId = source.SemanticId;
            that.SupplementalSemanticIds = source.SupplementalSemanticIds;
            that.Qualifiers = source.Qualifiers;
            that.EmbeddedDataSpecifications = source.EmbeddedDataSpecifications;

            if (!outputModifierContext.Content.Equals("metadata", StringComparison.OrdinalIgnoreCase))
            {
                that.ContentType = source.ContentType;
                that.Value = source.Value;
            }

            return that;
        }

        public IClass Transform(AnnotatedRelationshipElement that, UpdateContext context)
        {
            var source = context.Source as AnnotatedRelationshipElement;
            var outputModifierContext = context.OutputModifierContext;
            if (outputModifierContext.Content.Equals("value", StringComparison.OrdinalIgnoreCase))
            {
                that.First = source.First;
                that.Second = source.Second;
                if (source.Annotations.Any())
                {
                    //First check if number of elements in source and that are equal
                    if (that.Annotations == null || that.Annotations.Count != source.Annotations.Count)
                    {
                        throw new Exceptions.InvalidNumberOfChildElementsException("AnnotedRelationshipElement : " + that.IdShort, source.Annotations.Count, that.Annotations.Count);
                    }
                    for (int i = 0; i < source.Annotations.Count; i++)
                    {
                        UpdateImplementation.Update(that.Annotations[i], source.Annotations[i], outputModifierContext);
                    }
                }
                return that;
            }

            that.Extensions = source.Extensions;
            that.Category = source.Category;
            that.IdShort = source.IdShort;
            that.DisplayName = source.DisplayName;
            that.Description = source.Description;
            that.Checksum = source.Checksum;
            that.Kind = source.Kind;
            that.SemanticId = source.SemanticId;
            that.SupplementalSemanticIds = source.SupplementalSemanticIds;
            that.Qualifiers = source.Qualifiers;
            that.EmbeddedDataSpecifications = source.EmbeddedDataSpecifications;

            if (!outputModifierContext.Content.Equals("metadata", StringComparison.OrdinalIgnoreCase))
            {
                that.First = source.First;
                that.Second = source.Second;
            }

            if (outputModifierContext.IncludeChildren)
            {
                if (outputModifierContext.Level.Equals("core", StringComparison.OrdinalIgnoreCase))
                {
                    outputModifierContext.IncludeChildren = false;
                }
                if (source.Annotations.Any())
                {
                    //First check if number of elements in source and that are equal
                    if (that.Annotations == null || that.Annotations.Count != source.Annotations.Count)
                    {
                        throw new Exceptions.InvalidNumberOfChildElementsException("AnnotedRelationshipElement : " + that.IdShort, source.Annotations.Count, that.Annotations.Count);
                    }
                    for (int i = 0; i < source.Annotations.Count; i++)
                    {
                        UpdateImplementation.Update(that.Annotations[i], source.Annotations[i], outputModifierContext);
                    }
                }
            }

            return that;
        }

        public IClass Transform(Entity that, UpdateContext context)
        {
            var source = context.Source as Entity;
            var outputModifierContext = context.OutputModifierContext;
            if (outputModifierContext.Content.Equals("value", StringComparison.OrdinalIgnoreCase))
            {
                that.EntityType = source.EntityType;
                if (source.GlobalAssetId != null)
                {
                    that.GlobalAssetId = source.GlobalAssetId;
                }

                if (source.SpecificAssetId != null)
                {
                    that.SpecificAssetId = source.SpecificAssetId;
                }

                if (source.Statements.Any())
                {
                    if (that.Statements == null || that.Statements.Count != source.Statements.Count)
                    {
                        throw new Exceptions.InvalidNumberOfChildElementsException("Entity : " + that.IdShort, source.Statements.Count, that.Statements.Count);
                    }
                    for (int i = 0; i < source.Statements.Count; i++)
                    {
                        UpdateImplementation.Update(that.Statements[i], source.Statements[i], outputModifierContext);
                    }
                }
                return that;
            }

            that.Extensions = source.Extensions;
            that.Category = source.Category;
            that.IdShort = source.IdShort;
            that.DisplayName = source.DisplayName;
            that.Description = source.Description;
            that.Checksum = source.Checksum;
            that.Kind = source.Kind;
            that.SemanticId = source.SemanticId;
            that.SupplementalSemanticIds = source.SupplementalSemanticIds;
            that.Qualifiers = source.Qualifiers;
            that.EmbeddedDataSpecifications = source.EmbeddedDataSpecifications;

            that.EntityType = source.EntityType;
            if (!outputModifierContext.Content.Equals("metadata", StringComparison.OrdinalIgnoreCase))
            {
                if (source.GlobalAssetId != null)
                {
                    that.GlobalAssetId = source.GlobalAssetId;
                }

                if (source.SpecificAssetId != null)
                {
                    that.SpecificAssetId = source.SpecificAssetId;
                }
            }

            if (outputModifierContext.IncludeChildren)
            {
                if (outputModifierContext.Level.Equals("core", StringComparison.OrdinalIgnoreCase))
                {
                    outputModifierContext.IncludeChildren = false;
                }
                if (source.Statements.Any())
                {
                    if (that.Statements == null || that.Statements.Count != source.Statements.Count)
                    {
                        throw new Exceptions.InvalidNumberOfChildElementsException("Entity : " + that.IdShort, source.Statements.Count, that.Statements.Count);
                    }
                    for (int i = 0; i < source.Statements.Count; i++)
                    {
                        UpdateImplementation.Update(that.Statements[i], source.Statements[i], outputModifierContext);
                    }
                }
            }


            return that;
        }

        public IClass Transform(EventPayload that, UpdateContext context)
        {
            throw new NotImplementedException();
        }

        public IClass Transform(BasicEventElement that, UpdateContext context)
        {
            var source = context.Source as BasicEventElement;
            var outputModifierContext = context.OutputModifierContext;
            if (outputModifierContext.Content.Equals("value", StringComparison.OrdinalIgnoreCase))
            {
                that.Observed = source.Observed;
                return that;
            }

            that.Extensions = source.Extensions;
            that.Category = source.Category;
            that.IdShort = source.IdShort;
            that.DisplayName = source.DisplayName;
            that.Description = source.Description;
            that.Checksum = source.Checksum;
            that.Kind = source.Kind;
            that.SemanticId = source.SemanticId;
            that.SupplementalSemanticIds = source.SupplementalSemanticIds;
            that.Qualifiers = source.Qualifiers;
            that.EmbeddedDataSpecifications = source.EmbeddedDataSpecifications;

            if (!outputModifierContext.Content.Equals("metadata", StringComparison.OrdinalIgnoreCase))
            {
                that.Observed = source.Observed;
            }
            return that;
        }

        public IClass Transform(Operation that, UpdateContext context)
        {
            throw new NotImplementedException();
        }

        public IClass Transform(OperationVariable that, UpdateContext context)
        {
            throw new NotImplementedException();
        }

        public IClass Transform(Capability that, UpdateContext context)
        {
            throw new NotImplementedException();
        }

        public IClass Transform(ConceptDescription that, UpdateContext context)
        {
            throw new NotImplementedException();
        }

        public IClass Transform(Reference that, UpdateContext context)
        {
            throw new NotImplementedException();
        }

        public IClass Transform(Key that, UpdateContext context)
        {
            throw new NotImplementedException();
        }

        public IClass Transform(LangString that, UpdateContext context)
        {
            throw new NotImplementedException();
        }

        public IClass Transform(AasCore.Aas3_0_RC02.Environment that, UpdateContext context)
        {
            throw new NotImplementedException();
        }

        public IClass Transform(EmbeddedDataSpecification that, UpdateContext context)
        {
            throw new NotImplementedException();
        }

        public IClass Transform(ValueReferencePair that, UpdateContext context)
        {
            throw new NotImplementedException();
        }

        public IClass Transform(ValueList that, UpdateContext context)
        {
            throw new NotImplementedException();
        }

        public IClass Transform(DataSpecificationIec61360 that, UpdateContext context)
        {
            throw new NotImplementedException();
        }

        public IClass Transform(DataSpecificationPhysicalUnit that, UpdateContext context)
        {
            throw new NotImplementedException();
        }
    }
}
