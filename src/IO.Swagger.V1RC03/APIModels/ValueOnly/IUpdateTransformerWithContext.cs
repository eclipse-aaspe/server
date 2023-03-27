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
    internal interface IUpdateTransformerWithContext<in T> : ITransformerWithContext<OutputModifierContext, IClass>
    {
        public void Transform(IClass that, IClass source, OutputModifierContext context);
        public void Transform(Extension that, Extension source, OutputModifierContext context);
        public void Transform(AdministrativeInformation that, AdministrativeInformation source, OutputModifierContext context);
        public void Transform(Qualifier that, Qualifier source, OutputModifierContext context);
        public void Transform(AssetAdministrationShell that, AssetAdministrationShell source, OutputModifierContext context);
        public void Transform(AssetInformation that, AssetInformation source, OutputModifierContext context);
        public void Transform(Resource that, Resource source, OutputModifierContext context);
        public void Transform(SpecificAssetId that, SpecificAssetId source, OutputModifierContext context);
        public void Transform(Submodel that, Submodel source, OutputModifierContext context);
        public void Transform(RelationshipElement that, RelationshipElement source, OutputModifierContext context);
        public void Transform(SubmodelElementList that, SubmodelElementList source, OutputModifierContext context);
        public void Transform(SubmodelElementCollection that, SubmodelElementCollection source, OutputModifierContext context);
        public void Transform(Property that, Property source, OutputModifierContext context);
        public void Transform(MultiLanguageProperty that, MultiLanguageProperty source, OutputModifierContext context);
        public void Transform(AasCore.Aas3_0_RC02.Range that, OutputModifierContext context);
        public void Transform(ReferenceElement that, ReferenceElement source, OutputModifierContext context);
        public void Transform(Blob that, Blob source, OutputModifierContext context);
        public void Transform(File that, File source, OutputModifierContext context);
        public void Transform(AnnotatedRelationshipElement that, AnnotatedRelationshipElement source, OutputModifierContext context);
        public void Transform(Entity that, Entity source, OutputModifierContext context);
        public void Transform(EventPayload that, EventPayload source, OutputModifierContext context);
        public void Transform(BasicEventElement that, BasicEventElement source, OutputModifierContext context);
        public void Transform(Operation that, Operation source, OutputModifierContext context);
        public void Transform(OperationVariable that, OperationVariable source, OutputModifierContext context);
        public void Transform(Capability that, Capability source, OutputModifierContext context);
        public void Transform(ConceptDescription that, ConceptDescription source, OutputModifierContext context);
        public void Transform(Reference that, Reference source, OutputModifierContext context);
        public void Transform(Key that, Key source, OutputModifierContext context);
        public void Transform(LangString that, LangString source, OutputModifierContext context);
        public void Transform(LangStringSet that, LangStringSet source, OutputModifierContext context);
        public void Transform(DataSpecificationContent that, DataSpecificationContent source, OutputModifierContext context);
        public void Transform(DataSpecification that, DataSpecification source, OutputModifierContext context);
        public void Transform(AasCore.Aas3_0_RC02.Environment that, OutputModifierContext context);
    }
}
