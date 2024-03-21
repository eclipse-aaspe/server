namespace DataTransferObjects.ValueDTOs
{
    public record class OperationValue(string idShort, List<ISubmodelElementValue>? inputVariables = null, List<ISubmodelElementValue>? outputVariables = null, List<ISubmodelElementValue>? inoutputvariables = null) : ISubmodelElementValue;
}
