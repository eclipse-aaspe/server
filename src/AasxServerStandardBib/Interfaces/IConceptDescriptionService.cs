using AasCore.Aas3_0_RC02;
using System.Collections.Generic;

namespace AasxServerStandardBib.Interfaces
{
    public interface IConceptDescriptionService
    {
        ConceptDescription CreateConceptDescription(ConceptDescription body);
        void DeleteConceptDescriptionById(string cdIdentifier);
        List<ConceptDescription> GetAllConceptDescriptions(string idShort, Reference isCaseOf, Reference dataSpecificationRef);
        ConceptDescription GetConceptDescriptionById(string cdIdentifier);
        void UpdateConceptDescriptionById(ConceptDescription body, string cdIdentifier);
    }
}
