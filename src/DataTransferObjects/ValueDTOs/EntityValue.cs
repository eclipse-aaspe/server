using AasCore.Aas3_0;

namespace DataTransferObjects.ValueDTOs
{
    public record class EntityValue(
        string IdShort,
        EntityType EntityType,
        List<ISubmodelElementValue>? Statements = null,
        string? GlobalAssetId = null) : ISubmodelElementValue;
}
