using AasCore.Aas3_0_RC02;
using IO.Swagger.V1RC03.APIModels.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            that.DataSpecifications = source.DataSpecifications;

            //if (!outputModifierContext.Content.Equals("metadata", StringComparison.OrdinalIgnoreCase))
            //{
            if (source.SubmodelElements.Any())
            {
                for (int i = 0; i < source.SubmodelElements.Count; i++)
                {
                    UpdateImplementation.Update(that.SubmodelElements[i], source.SubmodelElements[i], outputModifierContext);
                }
            }
            //}

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
            that.DataSpecifications = source.DataSpecifications;

            if (!outputModifierContext.Content.Equals("metadata", StringComparison.OrdinalIgnoreCase))
            {
                that.First = source.First;
                that.Second = source.Second;
            }

            return that;
        }

        public IClass Transform(SubmodelElementList that, UpdateContext context)
        {
            throw new NotImplementedException();
        }

        public IClass Transform(SubmodelElementCollection that, UpdateContext context)
        {
            var source = context.Source as SubmodelElementCollection;
            var outputModifierContext = context.OutputModifierContext;
            if (outputModifierContext.Content.Equals("value", StringComparison.OrdinalIgnoreCase))
            {
                if (source.Value.Any())
                {
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
            that.DataSpecifications = source.DataSpecifications;

            if (!outputModifierContext.Content.Equals("metadata", StringComparison.OrdinalIgnoreCase))
            {
                if (source.Value.Any())
                {
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
            that.DataSpecifications = source.DataSpecifications;
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
            that.DataSpecifications = source.DataSpecifications;
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
            that.DataSpecifications = source.DataSpecifications;
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
            that.DataSpecifications = source.DataSpecifications;

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
            that.DataSpecifications = source.DataSpecifications;

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
            that.DataSpecifications = source.DataSpecifications;

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
                    var annotations = new List<IDataElement>();
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
            that.DataSpecifications = source.DataSpecifications;

            if (!outputModifierContext.Content.Equals("metadata", StringComparison.OrdinalIgnoreCase))
            {
                that.First = source.First;
                that.Second = source.Second;
                if (source.Annotations.Any())
                {
                    var annotations = new List<IDataElement>();
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
            throw new NotImplementedException();
        }

        public IClass Transform(EventPayload that, UpdateContext context)
        {
            throw new NotImplementedException();
        }

        public IClass Transform(BasicEventElement that, UpdateContext context)
        {
            throw new NotImplementedException();
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

        public IClass Transform(LangStringSet that, UpdateContext context)
        {
            throw new NotImplementedException();
        }

        public IClass Transform(DataSpecificationContent that, UpdateContext context)
        {
            throw new NotImplementedException();
        }

        public IClass Transform(DataSpecification that, UpdateContext context)
        {
            throw new NotImplementedException();
        }

        public IClass Transform(AasCore.Aas3_0_RC02.Environment that, UpdateContext context)
        {
            throw new NotImplementedException();
        }
    }
}
