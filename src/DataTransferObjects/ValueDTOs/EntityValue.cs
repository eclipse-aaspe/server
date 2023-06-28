using AasCore.Aas3_0;

namespace DataTransferObjects.ValueDTOs
{
    public record class EntityValue(string idShort, List<ISubmodelElementValue> statements, EntityType entityType, string globalAssetId) : ISubmodelElementValue;
}
