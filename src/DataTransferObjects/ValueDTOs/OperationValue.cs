namespace DataTransferObjects.ValueDTOs
{
    public record class OperationValue(string idShort, List<ISubmodelElementValue> inputVariables, List<ISubmodelElementValue> outputVariables, List<ISubmodelElementValue> inoutputvariables) : ISubmodelElementValue;
}
