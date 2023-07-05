using IO.Swagger.Lib.V3.Exceptions;
using IO.Swagger.Models;
using System.Collections.Generic;
using static AasCore.Aas3_0.Visitation;

namespace IO.Swagger.Lib.V3.SerializationModifiers.LevelExtent
{
    public class LevelExtentTransformer : AbstractTransformerWithContext<LevelExtentModifierContext, IClass>
    {
        public override IClass TransformAdministrativeInformation(IAdministrativeInformation that, LevelExtentModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public override IClass TransformAnnotatedRelationshipElement(IAnnotatedRelationshipElement that, LevelExtentModifierContext context)
        {
            var output = Copying.Deep(that);

            if (output != null)
            {
                context.IsRoot = false;
                if (context.IncludeChildren)
                {
                    if (context.Level == LevelEnum.Core)
                    {
                        output.Annotations = new List<IDataElement>();
                        foreach (var child in that.Annotations)
                        {
                            context.IncludeChildren = false;
                            output.Annotations.Add((IDataElement)Transform(child, context));
                        }
                    }
                    else
                    {
                        output.Annotations = new List<IDataElement>();
                        foreach (var child in that.Annotations)
                        {
                            output.Annotations.Add((IDataElement)Transform(child, context));
                        }
                    }
                }
                else
                {
                    output.Annotations = null;
                }
            }

            return output;
        }

        public override IClass TransformAssetAdministrationShell(IAssetAdministrationShell that, LevelExtentModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public override IClass TransformAssetInformation(IAssetInformation that, LevelExtentModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public override IClass TransformBasicEventElement(IBasicEventElement that, LevelExtentModifierContext context)
        {
            if (context.IsRoot)
            {
                if (context.Extent == ExtentEnum.WithBlobValue)
                {
                    throw new InvalidSerializationModifierException("Extent", that.GetType().Name);
                }
                if (context.Level == LevelEnum.Core)
                {
                    throw new InvalidSerializationModifierException("Level", that.GetType().Name);
                }
            }
            return Copying.Deep(that);
        }

        public override IClass TransformBlob(IBlob that, LevelExtentModifierContext context)
        {
            if (context.IsRoot)
            {
                if (context.Level == LevelEnum.Core)
                {
                    throw new InvalidSerializationModifierException("Level", that.GetType().Name);
                }
            }
            var output = Copying.Deep(that);
            if (context.Extent == ExtentEnum.WithoutBlobValue)
            {
                output.Value = null;
            }

            return output;
        }

        public override IClass TransformCapability(ICapability that, LevelExtentModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public override IClass TransformConceptDescription(IConceptDescription that, LevelExtentModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public override IClass TransformDataSpecificationIec61360(IDataSpecificationIec61360 that, LevelExtentModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public override IClass TransformEmbeddedDataSpecification(IEmbeddedDataSpecification that, LevelExtentModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public override IClass TransformEntity(IEntity that, LevelExtentModifierContext context)
        {
            var output = Copying.Deep(that);

            if (output != null)
            {
                context.IsRoot = false;
                if (context.IncludeChildren)
                {
                    if (context.Level == LevelEnum.Core)
                    {
                        output.Statements = new List<ISubmodelElement>();
                        foreach (var child in that.Statements)
                        {
                            context.IncludeChildren = false;
                            output.Statements.Add((ISubmodelElement)Transform(child, context));
                        }
                    }
                    else
                    {
                        output.Statements = new List<ISubmodelElement>();
                        foreach (var child in that.Statements)
                        {
                            output.Statements.Add((ISubmodelElement)Transform(child, context));
                        }
                    }
                }
                else
                {
                    output.Statements = null;
                }
            }

            return output;
        }

        public override IClass TransformEnvironment(IEnvironment that, LevelExtentModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public override IClass TransformEventPayload(IEventPayload that, LevelExtentModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public override IClass TransformExtension(IExtension that, LevelExtentModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public override IClass TransformFile(IFile that, LevelExtentModifierContext context)
        {
            if (context.IsRoot)
            {
                if (context.Extent == ExtentEnum.WithBlobValue)
                {
                    throw new InvalidSerializationModifierException("Extent", that.GetType().Name);
                }
                if (context.Level == LevelEnum.Core)
                {
                    throw new InvalidSerializationModifierException("Level", that.GetType().Name);
                }
            }
            return Copying.Deep(that);
        }

        public override IClass TransformKey(IKey that, LevelExtentModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public override IClass TransformLangStringDefinitionTypeIec61360(ILangStringDefinitionTypeIec61360 that, LevelExtentModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public override IClass TransformLangStringNameType(ILangStringNameType that, LevelExtentModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public override IClass TransformLangStringPreferredNameTypeIec61360(ILangStringPreferredNameTypeIec61360 that, LevelExtentModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public override IClass TransformLangStringShortNameTypeIec61360(ILangStringShortNameTypeIec61360 that, LevelExtentModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public override IClass TransformLangStringTextType(ILangStringTextType that, LevelExtentModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public override IClass TransformLevelType(ILevelType that, LevelExtentModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public override IClass TransformMultiLanguageProperty(IMultiLanguageProperty that, LevelExtentModifierContext context)
        {
            if (context.IsRoot)
            {
                if (context.Extent == ExtentEnum.WithBlobValue)
                {
                    throw new InvalidSerializationModifierException("Extent", that.GetType().Name);
                }
                if (context.Level == LevelEnum.Core)
                {
                    throw new InvalidSerializationModifierException("Level", that.GetType().Name);
                }
            }
            return Copying.Deep(that);
        }

        public override IClass TransformOperation(IOperation that, LevelExtentModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public override IClass TransformOperationVariable(IOperationVariable that, LevelExtentModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public override IClass TransformProperty(IProperty that, LevelExtentModifierContext context)
        {
            if (context.IsRoot)
            {
                if (context.Extent == ExtentEnum.WithBlobValue)
                {
                    throw new InvalidSerializationModifierException("Extent", that.GetType().Name);
                }
                if (context.Level == LevelEnum.Core)
                {
                    throw new InvalidSerializationModifierException("Level", that.GetType().Name);
                }
            }
            return Copying.Deep(that);
        }

        public override IClass TransformQualifier(IQualifier that, LevelExtentModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public override IClass TransformRange(IRange that, LevelExtentModifierContext context)
        {
            if (context.IsRoot)
            {
                if (context.Extent == ExtentEnum.WithBlobValue)
                {
                    throw new InvalidSerializationModifierException("Extent", that.GetType().Name);
                }
                if (context.Level == LevelEnum.Core)
                {
                    throw new InvalidSerializationModifierException("Level", that.GetType().Name);
                }
            }
            return Copying.Deep(that);
        }

        public override IClass TransformReference(IReference that, LevelExtentModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public override IClass TransformReferenceElement(IReferenceElement that, LevelExtentModifierContext context)
        {
            if (context.IsRoot)
            {
                if (context.Extent == ExtentEnum.WithBlobValue)
                {
                    throw new InvalidSerializationModifierException("Extent", that.GetType().Name);
                }
                if (context.Level == LevelEnum.Core)
                {
                    throw new InvalidSerializationModifierException("Level", that.GetType().Name);
                }
            }
            return Copying.Deep(that);
        }

        public override IClass TransformRelationshipElement(IRelationshipElement that, LevelExtentModifierContext context)
        {
            if (context.IsRoot)
            {
                if (context.Extent == ExtentEnum.WithBlobValue)
                {
                    throw new InvalidSerializationModifierException("Extent", that.GetType().Name);
                }
                if (context.Level == LevelEnum.Core)
                {
                    throw new InvalidSerializationModifierException("Level", that.GetType().Name);
                }
            }
            return Copying.Deep(that);
        }

        public override IClass TransformResource(IResource that, LevelExtentModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public override IClass TransformSpecificAssetId(ISpecificAssetId that, LevelExtentModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public override IClass TransformSubmodel(ISubmodel that, LevelExtentModifierContext context)
        {
            var output = Copying.Deep(that);

            if (output != null)
            {
                context.IsRoot = false;
                if (context.Level == LevelEnum.Core)
                {
                    output.SubmodelElements = new List<ISubmodelElement>();
                    foreach (var child in that.SubmodelElements)
                    {
                        context.IncludeChildren = false;
                        output.SubmodelElements.Add((ISubmodelElement)Transform(child, context));
                    }
                }
                else
                {
                    output.SubmodelElements = new List<ISubmodelElement>();
                    foreach (var child in that.SubmodelElements)
                    {
                        output.SubmodelElements.Add((ISubmodelElement)Transform(child, context));
                    }
                }
            }

            return output;
        }

        public override IClass TransformSubmodelElementCollection(ISubmodelElementCollection that, LevelExtentModifierContext context)
        {
            var output = Copying.Deep(that);

            if (output != null)
            {
                context.IsRoot = false;
                if (context.IncludeChildren)
                {
                    if (context.Level == LevelEnum.Core)
                    {
                        output.Value = new List<ISubmodelElement>();
                        foreach (var child in that.Value)
                        {
                            context.IncludeChildren = false;
                            output.Value.Add((ISubmodelElement)Transform(child, context));
                        }
                    }
                    else
                    {
                        output.Value = new List<ISubmodelElement>();
                        foreach (var child in that.Value)
                        {
                            output.Value.Add((ISubmodelElement)Transform(child, context));
                        }
                    }
                }
                else
                {
                    output.Value = null;
                }
            }


            return output;
        }

        public override IClass TransformSubmodelElementList(ISubmodelElementList that, LevelExtentModifierContext context)
        {
            var output = Copying.Deep(that);
            if (output != null)
            {
                context.IsRoot = false;
                if (context.IncludeChildren)
                {
                    if (context.Level == LevelEnum.Core)
                    {
                        output.Value = new List<ISubmodelElement>();
                        foreach (var child in that.Value)
                        {
                            context.IncludeChildren = false;
                            output.Value.Add((ISubmodelElement)Transform(child, context));
                        }
                    }
                    else
                    {
                        output.Value = new List<ISubmodelElement>();
                        foreach (var child in that.Value)
                        {
                            output.Value.Add((ISubmodelElement)Transform(child, context));
                        }
                    }
                }
                else
                {
                    output.Value = null;
                }
            }

            return output;
        }

        public override IClass TransformValueList(IValueList that, LevelExtentModifierContext context)
        {
            throw new System.NotImplementedException();
        }

        public override IClass TransformValueReferencePair(IValueReferencePair that, LevelExtentModifierContext context)
        {
            throw new System.NotImplementedException();
        }
    }
}
