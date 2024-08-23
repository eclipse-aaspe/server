using DataTransferObjects.CommonDTOs;

namespace DataTransferObjects.ValueDTOs
{
    public record class AnnotatedRelationshipElementValue(string IdShort, ReferenceDTO First, ReferenceDTO Second, List<ISubmodelElementValue>? Annotations = null) : ISubmodelElementValue;
}
