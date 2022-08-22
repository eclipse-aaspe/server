using AasCore.Aas3_0_RC02;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IO.Swagger.V1RC03.APIModels.Core
{
    internal class SubmodelElementCollectionCore : SubmodelElementCollection
    {
        public SubmodelElementCollectionCore(SubmodelElementCollection collection) : base(collection.Extensions, collection.Category, collection.IdShort, collection.DisplayName, collection.Description, collection.Checksum, collection.Kind, collection.SemanticId, collection.SupplementalSemanticIds, collection.Qualifiers, collection.DataSpecifications, collection.Value)
        {
            //Remove indirect children
            foreach (var submodelElement in this.Value)
            {
                if (submodelElement is SubmodelElementCollection childCollection)
                {
                    childCollection.Value.Clear();
                }
                else if (submodelElement is SubmodelElementList list)
                {
                    list.Value.Clear();
                }
                else if (submodelElement is Entity entity)
                {
                    entity.Statements.Clear();
                }
            }
        }
    }
}
