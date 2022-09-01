using AasCore.Aas3_0_RC02;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extenstions
{
    public static class ExtendEntity
    {
        public static Entity ConvertFromV20(this Entity entity, AasxCompatibilityModels.AdminShellV20.Entity sourceEntity)
        {
            if(sourceEntity == null)
            {
                return null;
            }

            if(sourceEntity.statements != null)
            {
                entity.Statements ??= new List<ISubmodelElement>();
                foreach (var submodelElementWrapper in sourceEntity.statements)
                {
                    var sourceSubmodelElement = submodelElementWrapper.submodelElement;
                    ISubmodelElement outputSubmodelElement = null;
                    if (sourceSubmodelElement != null)
                    {
                        outputSubmodelElement = outputSubmodelElement.ConvertFromV20(sourceSubmodelElement);
                    }
                    entity.Statements.Add(outputSubmodelElement);
                }
            }

            if(sourceEntity.assetRef != null)
            {
                //TODO:jtikekar whether to convert to Global or specific asset id
                var assetRef = ExtensionsUtil.ConvertReferenceFromV20(sourceEntity.assetRef, ReferenceTypes.GlobalReference);
                entity.GlobalAssetId = assetRef;
            }

            return entity;
        }
    }
}
