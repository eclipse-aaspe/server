using AasCore.Aas3_0_RC02;
using Extenstions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AasxServerStandardBib.Extenstions
{
    public static class ExtendAnnotedRelationshipElement
    {
        public static AnnotatedRelationshipElement ConvertAnnotationsFromV20(this AnnotatedRelationshipElement annotatedRelationshipElement, AasxCompatibilityModels.AdminShellV20.AnnotatedRelationshipElement sourceAnnotedRelElement)
        {
            if (sourceAnnotedRelElement == null)
            {
                return null;
            }

            if (sourceAnnotedRelElement.annotations != null)
            {
                annotatedRelationshipElement.Annotations ??= new List<IDataElement>();
                foreach (var submodelElementWrapper in sourceAnnotedRelElement.annotations)
                {
                    var sourceSubmodelElement = submodelElementWrapper.submodelElement;
                    ISubmodelElement outputSubmodelElement = null;
                    if (sourceSubmodelElement != null)
                    {
                        outputSubmodelElement = outputSubmodelElement.ConvertFromV20(sourceSubmodelElement);
                    }
                    annotatedRelationshipElement.Annotations.Add((IDataElement)outputSubmodelElement);
                }
            }

            return annotatedRelationshipElement;
        }
    }
}
