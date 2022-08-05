using AasCore.Aas3_0_RC02;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Extenstions
{
    public static class ExtendConceptDescription
    {
        public static ConceptDescription ConvertFromV10(this ConceptDescription conceptDescription, AasxCompatibilityModels.AdminShellV10.ConceptDescription sourceConceptDescription)
        {
            if (sourceConceptDescription == null)
            {
                return null;
            }

            if (string.IsNullOrEmpty(sourceConceptDescription.idShort))
            {
                conceptDescription.IdShort = "";
            }
            else
            {
                conceptDescription.IdShort = sourceConceptDescription.idShort;
            }

            if (sourceConceptDescription.description != null)
            {
                conceptDescription.Description = ExtensionsUtil.ConvertDescriptionFromV10(sourceConceptDescription.description);
            }

            if (sourceConceptDescription.administration != null)
            {
                conceptDescription.Administration = new AdministrativeInformation(version: sourceConceptDescription.administration.version, revision: sourceConceptDescription.administration.revision);
            }

            if (sourceConceptDescription.IsCaseOf != null && sourceConceptDescription.IsCaseOf.Count != 0)
            {
                if (conceptDescription.IsCaseOf == null)
                {
                    conceptDescription.IsCaseOf = new List<Reference>();
                }
                foreach (var caseOf in sourceConceptDescription.IsCaseOf)
                {
                    conceptDescription.IsCaseOf.Add(ExtensionsUtil.ConvertReferenceFromV10(caseOf, ReferenceTypes.ModelReference));
                }
            }

            return conceptDescription;
        }

        public static ConceptDescription ConvertFromV20(this ConceptDescription conceptDescription, AasxCompatibilityModels.AdminShellV20.ConceptDescription sourceConceptDescription)
        {
            if (sourceConceptDescription == null)
            {
                return null;
            }

            if (string.IsNullOrEmpty(sourceConceptDescription.idShort))
            {
                conceptDescription.IdShort = "";
            }
            else
            {
                conceptDescription.IdShort = sourceConceptDescription.idShort;
            }

            if (sourceConceptDescription.description != null)
            {
                conceptDescription.Description = ExtensionsUtil.ConvertDescriptionFromV20(sourceConceptDescription.description);
            }

            if (sourceConceptDescription.administration != null)
            {
                conceptDescription.Administration = new AdministrativeInformation(version: sourceConceptDescription.administration.version, revision: sourceConceptDescription.administration.revision);
            }

            if (sourceConceptDescription.IsCaseOf != null && sourceConceptDescription.IsCaseOf.Count != 0)
            {
                if (conceptDescription.IsCaseOf == null)
                {
                    conceptDescription.IsCaseOf = new List<Reference>();
                }
                foreach (var caseOf in sourceConceptDescription.IsCaseOf)
                {
                    conceptDescription.IsCaseOf.Add(ExtensionsUtil.ConvertReferenceFromV20(caseOf, ReferenceTypes.ModelReference));
                }
            }

            return conceptDescription;
        }

        public static Reference GetCdReference(this ConceptDescription conceptDescription)
        {
            var key = new Key(KeyTypes.ConceptDescription, conceptDescription.Id);
            return new Reference(ReferenceTypes.ModelReference, new List<Key> { key });
        }
    }
}
