namespace DataTransferObjects.CommonDTOs
{
    public record class AdministrativeInformationDTO(
            List<EmbeddedDataSpecificationDTO>? embeddedDataSpecifications = null,
            string? version = null,
            string? revision = null,
            ReferenceDTO? creator = null,
            string? templateId = null) : IDTO;
}
