using AasxServerStandardBib.Exceptions;
using System.Linq;
using static AasCore.Aas3_0.Visitation;

namespace AasxServerStandardBib.Transformers
{
    //internal class UpdateTransformer : AbstractTransformerWithContext<IClass, IClass>
    internal class UpdateTransformer : AbstractVisitorWithContext<IClass>
    {
        public override void VisitAdministrativeInformation(IAdministrativeInformation that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitAnnotatedRelationshipElement(IAnnotatedRelationshipElement that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitAssetAdministrationShell(IAssetAdministrationShell that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitAssetInformation(IAssetInformation that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitBasicEventElement(IBasicEventElement that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitBlob(IBlob that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitCapability(ICapability that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitConceptDescription(IConceptDescription that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitDataSpecificationIec61360(IDataSpecificationIec61360 that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitEmbeddedDataSpecification(IEmbeddedDataSpecification that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitEntity(IEntity that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitEnvironment(IEnvironment that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitEventPayload(IEventPayload that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitExtension(IExtension that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitFile(IFile that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitKey(IKey that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitLangStringDefinitionTypeIec61360(ILangStringDefinitionTypeIec61360 that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitLangStringNameType(ILangStringNameType that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitLangStringPreferredNameTypeIec61360(ILangStringPreferredNameTypeIec61360 that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitLangStringShortNameTypeIec61360(ILangStringShortNameTypeIec61360 that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitLangStringTextType(ILangStringTextType that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitLevelType(ILevelType that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitMultiLanguageProperty(IMultiLanguageProperty that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitOperation(IOperation that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitOperationVariable(IOperationVariable that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitProperty(IProperty that, IClass context)
        {
            if (context is Property target)
            {
                that.Extensions = target.Extensions;
                that.Category = target.Category;
                that.IdShort = target.IdShort;
                that.DisplayName = target.DisplayName;
                that.Description = target.Description;
                that.SemanticId = target.SemanticId;
                that.SupplementalSemanticIds = target.SupplementalSemanticIds;
                that.Qualifiers = target.Qualifiers;
                that.EmbeddedDataSpecifications = target.EmbeddedDataSpecifications;
                that.ValueType = target.ValueType;
                that.Value = target.Value;
                that.ValueId = target.ValueId;
            }
            else
            {
                throw new InvalidUpdateResourceException(typeof(Property).Name, context.GetType().Name);
            }
        }

        public override void VisitQualifier(IQualifier that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitRange(IRange that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitReference(IReference that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitReferenceElement(IReferenceElement that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitRelationshipElement(IRelationshipElement that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitResource(IResource that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitSpecificAssetId(ISpecificAssetId that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitSubmodel(ISubmodel that, IClass context)
        {
            if (context is Submodel target)
            {
                //As per the specifications, check for smes first. If any of the resource not available, do not change the server
                if (target.SubmodelElements.Any())
                {
                    //First check if number of elements in the server's resource are greater than or equal to the request's resource
                    if (that.SubmodelElements.Count < target.SubmodelElements.Count)
                    {
                        throw new InvalidNumberOfChildElementsException("Submodel : " + that.IdShort, that.SubmodelElements.Count, target.SubmodelElements.Count);
                    }
                    for (int i = 0; i < target.SubmodelElements.Count; i++)
                    {
                        Update.ToUpdateObject(that.SubmodelElements[i], target.SubmodelElements[i]);
                    }
                }

                //TODO:jtikekar check if nested transform necessary
                that.Extensions = target.Extensions;
                that.Category = target.Category;
                that.IdShort = target.IdShort;
                that.DisplayName = target.DisplayName;
                that.Description = target.Description;
                that.Administration = target.Administration;
                that.Id = target.Id;
                that.Kind = target.Kind;
                that.SemanticId = target.SemanticId;
                that.SupplementalSemanticIds = target.SupplementalSemanticIds;
                that.Qualifiers = target.Qualifiers;
                that.EmbeddedDataSpecifications = target.EmbeddedDataSpecifications;
            }
        }

        public override void VisitSubmodelElementCollection(ISubmodelElementCollection that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitSubmodelElementList(ISubmodelElementList that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitValueList(IValueList that, IClass context)
        {
            throw new System.NotImplementedException();
        }

        public override void VisitValueReferencePair(IValueReferencePair that, IClass context)
        {
            throw new System.NotImplementedException();
        }
    }
}
