namespace DataTransferObjects.CommonDTOs
{
    public record class AdministrativeInformationDTO(
            List<EmbeddedDataSpecificationDTO>? EmbeddedDataSpecifications = null,
            string? Version = null,
            string? Revision = null,
            ReferenceDTO? Creator = null,
            string? TemplateId = null) : IDTO;
}
