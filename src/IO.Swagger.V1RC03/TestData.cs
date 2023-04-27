
using IO.Swagger.V1RC03.ApiModel;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace IO.Swagger.V1RC03
{
    public class TestData
    {
        /// <summary>
        /// HandleId vs Operation Result of the corresponding Opration
        /// </summary>
        public static Dictionary<string, OperationResult> opResultAsyncDict = new Dictionary<string, OperationResult>();
        private static Timer m_simulationTimer;
        public static Submodel getTestSubmodel()
        {
            var prop1 = new Property(valueType: DataTypeDefXsd.String, idShort: "property1", value: "123");
            var prop2 = new Property(valueType: DataTypeDefXsd.String, idShort: "property2", value: "456");
            var list1 = new SubmodelElementList(AasSubmodelElements.Property, idShort: "submodelElementList1");
            list1.Value = new List<ISubmodelElement>();
            list1.Value.Add(prop1);
            list1.Value.Add(prop2);

            var list2 = new SubmodelElementList(AasSubmodelElements.Property, idShort: "submodelElementList2");
            list2.Value = new List<ISubmodelElement>();
            list2.Value.Add(prop1);
            list2.Value.Add(prop2);


            var sm = new Submodel(id: "http://submodel.org/submodel", idShort: "submodel", category: "category", administration: new AdministrativeInformation());
            sm.SubmodelElements = new List<ISubmodelElement>();
            sm.SubmodelElements.Add(list1);
            sm.SubmodelElements.Add(list2);
            return sm;
        }

        public static SubmodelElementList GetSubmodelElementList()
        {
            var list = new SubmodelElementList(AasSubmodelElements.Property, idShort: "TestSmeList_Level1");
            var prop1 = new Property(valueType: DataTypeDefXsd.String, idShort: "TestProp_Level2", value: "TestProp_Level2");
            var prop2 = new Property(valueType: DataTypeDefXsd.String, idShort: "TestProp2_Level2", value: "TestProp2_Level2");
            list.Value = new List<ISubmodelElement>();
            list.Value.Add(prop1);
            list.Value.Add(prop2);
            return list;
        }

        internal static void InvokeTestOperation(OperationHandle operationHandle)
        {
            //First invokation
            OperationResult opResult = new OperationResult();
            opResult.OutputArguments = new List<OperationVariable>
            {
                new OperationVariable(new Property(DataTypeDefXsd.String, idShort:"DemoOutputArgument"))
            };
            opResult.ExecutionState = ExecutionState.InitiatedEnum;
            Message message = new Message
            {
                Code = "xxx",
                MessageType = Message.MessageTypeEnum.InfoEnum,
                Text = "Initiated the operation",
                Timestamp = DateTime.UtcNow.ToString()
            };
            Result result = new Result
            {
                Messages = new List<Message>() { message }
            };
            opResult.ExecutionResult = result;
            opResult.RequestId = operationHandle.RequestId;

            opResultAsyncDict.Add(operationHandle.HandleId, opResult);

            m_simulationTimer = new Timer(DoSimulation, null, 5000, 5000);
        }

        private static void DoSimulation(object state)
        {
            var random = new Random();
            var values = Enum.GetValues(typeof(ExecutionState));

            foreach (var handleId in opResultAsyncDict.Keys)
            {
                var value = (ExecutionState)values.GetValue(random.Next(values.Length));
                opResultAsyncDict[handleId].ExecutionState = value;
            }
        }
    }
}
