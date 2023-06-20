using AasCore.Aas3_0;

namespace DataTransferObjects.CommonDTOs
{
    public record class ExtensionDTO(
            string name,
            ReferenceDTO? semanticId = null,
            List<ReferenceDTO>? supplementalSemanticIds = null,
            DataTypeDefXsd? valueType = null,
            string? value = null,
            List<ReferenceDTO>? refersTo = null) : IDTO;
}
