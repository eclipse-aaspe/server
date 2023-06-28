using DataTransferObjects.CommonDTOs;

namespace DataTransferObjects.ValueDTOs
{
    public record class AnnotatedRelationshipElementValue(string idShort, ReferenceDTO first, ReferenceDTO second, List<ISubmodelElementValue> annotations = null) : ISubmodelElementValue;
}
