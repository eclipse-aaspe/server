using AasCore.Aas3_0_RC02;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IO.Swagger.V1RC03.APIModels.Core
{
    internal class SubmodelElementListCore : SubmodelElementList
    {
        public SubmodelElementListCore(SubmodelElementList list) : base(list.TypeValueListElement, list.Extensions, list.Category, list.IdShort, list.DisplayName, list.Description, list.Checksum, list.Kind, list.SemanticId, list.SupplementalSemanticIds, list.Qualifiers, list.DataSpecifications, list.OrderRelevant, list.Value, list.SemanticIdListElement, list.ValueTypeListElement)
        {
            //Remove indirect children
            foreach (var submodelElement in this.Value)
            {
                if (submodelElement is SubmodelElementCollection childCollection)
                {
                    childCollection.Value.Clear();
                }
                else if (submodelElement is SubmodelElementList childList)
                {
                    childList.Value.Clear();
                }
                else if (submodelElement is Entity entity)
                {
                    entity.Statements.Clear();
                }
            }
        }
    }
}
