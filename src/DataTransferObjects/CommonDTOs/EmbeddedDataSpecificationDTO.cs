namespace DataTransferObjects.CommonDTOs
{
    // TODO (jtikekar, 2023-09-04): support DataSpecificationContent
    public record class EmbeddedDataSpecificationDTO(ReferenceDTO dataSpecification) : IDTO;
}
