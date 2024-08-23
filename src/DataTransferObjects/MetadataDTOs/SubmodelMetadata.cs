using AasCore.Aas3_0;
using DataTransferObjects.CommonDTOs;

namespace DataTransferObjects.MetadataDTOs
{
    public record class SubmodelMetadata(
            string Id,
            List<ExtensionDTO>? Extensions = null,
            string? Category = null,
            string? IdShort = null,
            List<LangStringNameTypeDTO>? DisplayName = null,
            List<LangStringTextTypeDTO>? Description = null,
            AdministrativeInformationDTO? Administration = null,
            ModellingKind? Kind = null,
            ReferenceDTO? SemanticId = null,
            List<ReferenceDTO>? SupplementalSemanticIds = null,
            List<QualifierDTO>? Qualifiers = null,
            List<EmbeddedDataSpecificationDTO>? EmbeddedDataSpecifications = null,
            List<ISubmodelElementMetadata>? SubmodelElements = null,
            string ModelType = "Submodel") : IMetadataDTO;
}
