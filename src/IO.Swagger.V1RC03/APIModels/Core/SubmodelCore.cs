using AasCore.Aas3_0_RC02;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IO.Swagger.V1RC03.APIModels.Core
{
    internal class SubmodelCore : Submodel
    {
        public SubmodelCore(Submodel submodel) : base(submodel.Id, submodel.Extensions, submodel.Category, submodel.IdShort, submodel.DisplayName, submodel.Description, submodel.Checksum, submodel.Administration, submodel.Kind, submodel.SemanticId, submodel.SupplementalSemanticIds, submodel.Qualifiers, submodel.DataSpecifications, submodel.SubmodelElements)
        {
            //Remove indirect children
            foreach (var submodelElement in this.SubmodelElements)
            {
                if (submodelElement is SubmodelElementCollection collection)
                {
                    collection.Value.Clear();
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
