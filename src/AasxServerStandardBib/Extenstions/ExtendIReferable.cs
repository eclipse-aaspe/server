using AasCore.Aas3_0_RC02;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extenstions
{
    public static class ExtendIReferable
    {
        public static void SetTimeStamp(this IReferable referable, DateTime timeStamp)
        {
            IReferable newReferable = referable;
            do
            {
                newReferable.TimeStamp = timeStamp;
                if (newReferable != newReferable.Parent)
                {
                    newReferable = (IReferable)newReferable.Parent;
                }
                else
                    newReferable = null;
            }
            while (newReferable != null);
        }

        public static IEnumerable<ISubmodelElement> EnumerateChildren(this IReferable referable)
        {
            if (referable is Submodel submodel && submodel.SubmodelElements != null)
            {
                foreach (var submodelElement in submodel.SubmodelElements)
                {
                    yield return submodelElement;
                }
            }
            else if (referable is SubmodelElementCollection submodelElementCollection)
            {
                foreach (var submodelElement in submodelElementCollection.Value)
                {
                    yield return submodelElement;
                }
            }
            else if (referable is SubmodelElementList submodelElementList)
            {
                foreach (var submodelElement in submodelElementList.Value)
                {
                    yield return submodelElement;
                }
            }
            else if (referable is AnnotatedRelationshipElement annotatedRelationshipElement)
            {
                foreach (var submodelElement in annotatedRelationshipElement.Annotations)
                {
                    yield return submodelElement;
                }
            }
            else if (referable is Entity entity)
            {
                foreach (var submodelElement in entity.Statements)
                {
                    yield return submodelElement;
                }
            }
            else if (referable is Operation operation)
            {
                foreach (var inputVariable in operation.InputVariables)
                {
                    yield return inputVariable.Value;
                }

                foreach (var outputVariable in operation.OutputVariables)
                {
                    yield return outputVariable.Value;
                }

                foreach (var inOutVariable in operation.InoutputVariables)
                {
                    yield return inOutVariable.Value;
                }
            }
            else
            {
                yield break;
            }
        }


        public static void SetAllParentsAndTimestamps(this IReferable referable, IReferable parent, DateTime timeStamp, DateTime timeStampCreate)
        {
            if (parent == null)
                return;

            referable.Parent = parent;
            referable.TimeStamp = timeStamp;
            referable.TimeStampCreate = timeStampCreate;

            foreach (var submodelElement in referable.EnumerateChildren())
            {
                submodelElement.SetAllParentsAndTimestamps(referable, timeStamp, timeStampCreate);
            }
        }

        public static Submodel GetParentSubmodel(this IReferable referable)
        {
            IReferable parent = referable;
            while (parent is not Submodel && parent != null)
                parent = (IReferable)parent.Parent;
            return parent as Submodel;
        }

        public static string CollectIdShortByParent(this IReferable referable)
        {
            // recurse first
            var head = "";
            if (referable is not IIdentifiable && referable.Parent is IReferable parentReferable)
                // can go up
                head = parentReferable.CollectIdShortByParent() + "/";
            // add own
            var myid = "<no id-Short!>";
            if (string.IsNullOrEmpty(referable.IdShort))
                myid = referable.IdShort.Trim();
            // together
            return head + myid;
        }
    }
}
