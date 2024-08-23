using AasCore.Aas3_0;

namespace DataTransferObjects.CommonDTOs
{
    public record class QualifierDTO(
            string Type,
            DataTypeDefXsd ValueType,
            ReferenceDTO? SemanticId = null,
            List<ReferenceDTO>? SupplementalSemanticIds = null,
            QualifierKind? Kind = null,
            string? Value = null,
            ReferenceDTO? ValueId = null) : IDTO;
}
