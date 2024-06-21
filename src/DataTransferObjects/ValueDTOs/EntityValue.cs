using AasCore.Aas3_0;

namespace DataTransferObjects.ValueDTOs
{
    public record class EntityValue(string idShort, EntityType entityType, List<ISubmodelElementValue>? statements = null, string globalAssetId = null) : ISubmodelElementValue;
}
