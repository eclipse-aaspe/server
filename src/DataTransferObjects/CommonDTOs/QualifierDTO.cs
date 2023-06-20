using AasCore.Aas3_0;

namespace DataTransferObjects.CommonDTOs
{
    public record class QualifierDTO(
            string type,
            DataTypeDefXsd valueType,
            ReferenceDTO? semanticId = null,
            List<ReferenceDTO>? supplementalSemanticIds = null,
            QualifierKind? kind = null,
            string? value = null,
            ReferenceDTO? valueId = null) : IDTO;
}
