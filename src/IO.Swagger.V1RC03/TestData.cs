using AasCore.Aas3_0_RC02;
using System.Collections.Generic;

namespace IO.Swagger.V1RC03
{
    public class TestData
    {
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

    }
}
