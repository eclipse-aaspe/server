namespace DataTransferObjects.ValueDTOs
{
    public record class SubmodelValue(
        List<ISubmodelElementValue>? SubmodelElements = null)
        : IValueDTO;
}
