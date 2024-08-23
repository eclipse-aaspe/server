namespace DataTransferObjects.ValueDTOs
{
    public record class OperationValue(
        string IdShort,
        List<ISubmodelElementValue>? InputVariables = null,
        List<ISubmodelElementValue>? OutputVariables = null,
        List<ISubmodelElementValue>? Inoutputvariables = null)
        : ISubmodelElementValue;
}
