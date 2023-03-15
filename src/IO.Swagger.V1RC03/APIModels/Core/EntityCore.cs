using AasCore.Aas3_0_RC02;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IO.Swagger.V1RC03.APIModels.Core
{
    //TODO: jtikekar remove this file
    internal class EntityCore : Entity
    {
        public EntityCore(Entity entity) : base(entity.EntityType, entity.Extensions, entity.Category, entity.IdShort, entity.DisplayName, entity.Description, entity.Checksum, entity.Kind, entity.SemanticId, entity.SupplementalSemanticIds, entity.Qualifiers, null, entity.Statements, entity.GlobalAssetId, entity.SpecificAssetId)
        {
            //Remove indirect children
            foreach (var submodelElement in this.Statements)
            {
                if (submodelElement is SubmodelElementCollection childCollection)
                {
                    childCollection.Value.Clear();
                }
                else if (submodelElement is SubmodelElementList childList)
                {
                    childList.Value.Clear();
                }
                else if (submodelElement is Entity childEntity)
                {
                    childEntity.Statements.Clear();
                }
            }
        }
    }
}
